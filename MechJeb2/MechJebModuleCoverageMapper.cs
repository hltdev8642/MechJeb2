extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleCoverageMapper : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleCoverageMapper(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowCoverageOverlay = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble MinElevationAngle = new EditableDouble(10);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble ScanDuration = new EditableDouble(86400 * 3); // 3 days

        [Persistent(pass = (int)Pass.GLOBAL)]
        public string TargetBody = "Kerbin";

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int LatitudeBins = 18;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int LongitudeBins = 36;

        public class CoverageCell
        {
            public double latitude;
            public double longitude;
            public double coveragePercent;
            public double maxRevisitTime;
            public double averageRevisitTime;
            public int passesPerDay;
            public List<string> coveringVessels = new List<string>();
        }

        public class CoverageResult
        {
            public string bodyName;
            public double bodyRadius;
            public double totalCoveragePercent;
            public double averageRevisitTime;
            public double maxRevisitTime;
            public List<CoverageCell> cells = new List<CoverageCell>();
            public List<string> contributingVessels = new List<string>();
        }

        public CoverageResult CurrentCoverage { get; private set; }
        public bool IsComputing { get; private set; } = false;
        public string StatusText { get; private set; } = "";

        public void ComputeCoverage()
        {
            IsComputing = true;
            StatusText = "Computing coverage...";

            try
            {
                CelestialBody body = FlightGlobals.Bodies.FirstOrDefault(b =>
                    b.bodyName.Equals(TargetBody, StringComparison.OrdinalIgnoreCase));

                if (body == null)
                {
                    StatusText = $"Body '{TargetBody}' not found";
                    IsComputing = false;
                    return;
                }

                var result = new CoverageResult
                {
                    bodyName = body.bodyName,
                    bodyRadius = body.Radius,
                    cells = new List<CoverageCell>(),
                    contributingVessels = new List<string>()
                };

                double bodyRadius = body.Radius;
                double maxAtmoAlt = body.atmosphereDepth;

                // Collect all vessels that can reach this body
                var relevantVessels = FlightGlobals.Vessels
                    .Where(v => v != Vessel && v.orbit != null &&
                                v.orbit.referenceBody == body &&
                                v.vesselType != VesselType.Debris)
                    .ToList();

                if (!relevantVessels.Contains(Vessel) && Vessel.orbit != null &&
                    Vessel.orbit.referenceBody == body)
                {
                    relevantVessels.Add(Vessel);
                }

                if (relevantVessels.Count == 0)
                {
                    StatusText = "No vessels found orbiting " + body.bodyName;
                    IsComputing = false;
                    return;
                }

                result.contributingVessels = relevantVessels.Select(v => v.vesselName).ToList();

                double ut = Planetarium.GetUniversalTime();
                double timeStep = ScanDuration / Math.Max(100, LatitudeBins * LongitudeBins);
                int sampleCount = (int)(ScanDuration / timeStep);

                // Initialize coverage cells
                for (int lat = 0; lat < LatitudeBins; lat++)
                {
                    for (int lon = 0; lon < LongitudeBins; lon++)
                    {
                        var cell = new CoverageCell
                        {
                            latitude = -90 + (lat + 0.5) * (180.0 / LatitudeBins),
                            longitude = (lon + 0.5) * (360.0 / LongitudeBins) - 180,
                            coveragePercent = 0,
                            maxRevisitTime = 0,
                            averageRevisitTime = 0,
                            passesPerDay = 0,
                            coveringVessels = new List<string>()
                        };
                        result.cells.Add(cell);
                    }
                }

                // Simulate coverage over time
                double[] lastCoveredTime = new double[result.cells.Count];
                double[] totalCoveredDuration = new double[result.cells.Count];
                int[] coverageCounts = new int[result.cells.Count];
                double[] lastRevisitTime = new double[result.cells.Count];
                double[] totalRevisitTime = new double[result.cells.Count];
                int[] revisitCounts = new int[result.cells.Count];

                for (int i = 0; i < lastCoveredTime.Length; i++)
                    lastCoveredTime[i] = -1e9;

                for (int s = 0; s < sampleCount; s++)
                {
                    double sampleUt = ut + s * timeStep;

                    // For each cell, check if any vessel has line-of-sight
                    for (int c = 0; c < result.cells.Count; c++)
                    {
                        var cell = result.cells[c];
                        double latRad = cell.latitude * Mathf.Deg2Rad;
                        double lonRad = cell.longitude * Mathf.Deg2Rad;

                        // Surface point on body
                        Vector3d surfacePos = body.GetWorldSurfacePosition(cell.latitude, cell.longitude, 0);
                        bool isCovered = false;
                        string coveringVessel = "";

                        foreach (var v in relevantVessels)
                        {
                            if (v.orbit == null) continue;

                            Vector3d vesselPos = v.orbit.getRelativePositionAtUT(sampleUt) + body.position;
                            Vector3d toVessel = vesselPos - surfacePos;

                            // Check elevation angle
                            Vector3d surfaceUp = (surfacePos - body.position).normalized;
                            double elevation = 90 - Vector3d.Angle(surfaceUp, toVessel);

                            if (elevation >= MinElevationAngle)
                            {
                                // Check occlusion (vessel below horizon)
                                double distToVessel = toVessel.magnitude;
                                double horizonAngle = Math.Acos(bodyRadius / (bodyRadius + v.orbit.PeA));
                                if (elevation > horizonAngle * Mathf.Rad2Deg)
                                {
                                    isCovered = true;
                                    coveringVessel = v.vesselName;
                                    break;
                                }
                            }
                        }

                        if (isCovered)
                        {
                            totalCoveredDuration[c] += timeStep;
                            coverageCounts[c]++;

                            if (!cell.coveringVessels.Contains(coveringVessel) && !string.IsNullOrEmpty(coveringVessel))
                                cell.coveringVessels.Add(coveringVessel);

                            if (lastCoveredTime[c] > -1e8)
                            {
                                double gap = sampleUt - lastCoveredTime[c] - timeStep;
                                if (gap > timeStep * 1.5)
                                {
                                    totalRevisitTime[c] += gap;
                                    revisitCounts[c]++;
                                    if (gap > lastRevisitTime[c])
                                        lastRevisitTime[c] = gap;
                                }
                            }

                            lastCoveredTime[c] = sampleUt;
                        }
                    }
                }

                // Compute statistics
                double totalCovered = 0;
                double totalRevisit = 0;
                double maxRevisit = 0;
                int coveredCells = 0;

                for (int c = 0; c < result.cells.Count; c++)
                {
                    var cell = result.cells[c];
                    cell.coveragePercent = (totalCoveredDuration[c] / ScanDuration) * 100;
                    cell.maxRevisitTime = lastRevisitTime[c];
                    cell.averageRevisitTime = revisitCounts[c] > 0 ? totalRevisitTime[c] / revisitCounts[c] : 0;
                    cell.passesPerDay = (int)(coverageCounts[c] / (ScanDuration / 86400));

                    if (cell.coveragePercent > 0)
                    {
                        totalCovered += cell.coveragePercent;
                        coveredCells++;
                        if (cell.maxRevisitTime > maxRevisit)
                            maxRevisit = cell.maxRevisitTime;
                        totalRevisit += cell.averageRevisitTime;
                    }
                }

                result.totalCoveragePercent = coveredCells > 0 ? totalCovered / coveredCells : 0;
                result.averageRevisitTime = coveredCells > 0 ? totalRevisit / coveredCells : 0;
                result.maxRevisitTime = maxRevisit;

                CurrentCoverage = result;
                StatusText = $"Coverage: {result.totalCoveragePercent:F1}% avg | " +
                             $"Revisit: {result.averageRevisitTime / 60:F1}min avg | " +
                             $"Vessels: {result.contributingVessels.Count}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }

            IsComputing = false;
        }
    }
}
