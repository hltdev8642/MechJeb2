extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    /// <summary>
    /// Provides a detailed preview of maneuver node effects, including post-burn trajectory,
    /// ejection angles, and intercept analysis.
    /// </summary>
    public class MechJebModuleManeuverPreview : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleManeuverPreview(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowPostBurnOrbit = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowInterceptInfo = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowDeltaVBreakdown = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int TrajectorySampleCount = 120;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble PreviewDuration = new EditableDouble(86400); // 1 day

        public class ManeuverAnalysis
        {
            public double burnUt;
            public Vector3d burnVector;
            public double deltaV;
            public double progradeComponent;
            public double normalComponent;
            public double radialComponent;
            public double postBurnPeriapsis;
            public double postBurnApoapsis;
            public double postBurnInclination;
            public double ejectionAngle;
            public bool isEscapeTrajectory;
            public double timeToNextEncounter;
            public string targetBodyName;
            public double interceptDistance;
            public bool hasIntercept;
            public List<Vector3d> trajectoryPoints;
        }

        public ManeuverAnalysis CurrentAnalysis { get; private set; }
        public string StatusText { get; private set; } = "";

        public void AnalyzeCurrentNode()
        {
            try
            {
                if (Vessel.patchedConicSolver == null || Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                {
                    StatusText = "No maneuver node";
                    CurrentAnalysis = null;
                    return;
                }

                ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes[0];
                Orbit patch = node.patch;
                Orbit nextPatch = node.nextPatch;

                Vector3d burnVector = node.GetBurnVector(patch);
                double dv = burnVector.magnitude;
                double ut = node.UT;

                // Decompose delta-V
                Vector3d progradeDir = patch.GetOrbitNormal().normalized;
                Vector3d radialDir = Vector3d.Cross(progradeDir, patch.GetOrbitNormal()).normalized;
                Vector3d normalDir = patch.GetOrbitNormal().normalized;

                double progradeComponent = Vector3d.Dot(burnVector, patch.GetOrbitNormal());
                double normalComponent = Vector3d.Dot(burnVector, normalDir);
                double radialComponent = Vector3d.Dot(burnVector, radialDir);

                // Compute post-burn orbit
                Orbit postBurnOrbit = null;
                double postBurnPeriapsis = 0;
                double postBurnApoapsis = 0;
                double postBurnInclination = 0;

                try
                {
                    Vector3d posAtNode = patch.getRelativePositionAtUT(ut);
                    Vector3d velAtNode = patch.getOrbitalVelocityAtUT(ut).xzy;
                    Vector3d newVel = velAtNode + burnVector;

                    postBurnOrbit = new Orbit();
                    postBurnOrbit.UpdateFromStateVectors(posAtNode, newVel, patch.referenceBody, ut);
                    postBurnPeriapsis = postBurnOrbit.PeA;
                    postBurnApoapsis = postBurnOrbit.ApA;
                    postBurnInclination = postBurnOrbit.inclination;
                }
                catch { }

                // Compute ejection angle
                double ejectionAngle = 0;
                bool isEscape = false;
                if (patch.referenceBody != null && patch.referenceBody.referenceBody != null)
                {
                    Vector3d pos = patch.getRelativePositionAtUT(ut);
                    Vector3d prograde = patch.getOrbitalVelocityAtUT(ut).xzy.normalized;
                    Vector3d planetPos = patch.referenceBody.position;
                    Vector3d fromPlanet = (pos + patch.referenceBody.getPositionAtUT(ut) - planetPos).normalized;
                    ejectionAngle = Vector3d.Angle(prograde, fromPlanet);
                    isEscape = postBurnApoapsis > patch.referenceBody.sphereOfInfluence * 0.9;
                }

                // Generate trajectory points
                List<Vector3d> trajectoryPoints = new List<Vector3d>();
                double timeStep = PreviewDuration / Math.Max(1, TrajectorySampleCount);
                for (int i = 0; i < TrajectorySampleCount; i++)
                {
                    double sampleUt = ut + i * timeStep;
                    try
                    {
                        Vector3d pos = postBurnOrbit.getRelativePositionAtUT(sampleUt);
                        trajectoryPoints.Add(pos);
                    }
                    catch
                    {
                        break;
                    }
                }

                // Intercept info
                double interceptDist = 0;
                bool hasIntercept = false;
                double timeToEncounter = 0;
                string targetBody = "";

                if (nextPatch != null && nextPatch.referenceBody != patch.referenceBody)
                {
                    hasIntercept = true;
                    targetBody = nextPatch.referenceBody.bodyName;
                    interceptDist = nextPatch.PeA;
                    timeToEncounter = nextPatch.StartUT - ut;
                }

                CurrentAnalysis = new ManeuverAnalysis
                {
                    burnUt = ut,
                    burnVector = burnVector,
                    deltaV = dv,
                    progradeComponent = progradeComponent,
                    normalComponent = normalComponent,
                    radialComponent = radialComponent,
                    postBurnPeriapsis = postBurnPeriapsis,
                    postBurnApoapsis = postBurnApoapsis,
                    postBurnInclination = postBurnInclination,
                    ejectionAngle = ejectionAngle,
                    isEscapeTrajectory = isEscape,
                    timeToNextEncounter = timeToEncounter,
                    targetBodyName = targetBody,
                    interceptDistance = interceptDist,
                    hasIntercept = hasIntercept,
                    trajectoryPoints = trajectoryPoints
                };

                StatusText = $"Δv: {dv:F1} m/s | Pe: {postBurnPeriapsis / 1000:F0} km | Ap: {postBurnApoapsis / 1000:F0} km";
            }
            catch (Exception ex)
            {
                StatusText = $"Analysis error: {ex.Message}";
                CurrentAnalysis = null;
            }
        }

        public override void OnUpdate()
        {
            if (Enabled && Vessel.patchedConicSolver != null && Vessel.patchedConicSolver.maneuverNodes.Count > 0)
            {
                AnalyzeCurrentNode();
            }
        }

        public ManeuverNode GetActiveNode()
        {
            if (Vessel.patchedConicSolver != null && Vessel.patchedConicSolver.maneuverNodes.Count > 0)
                return Vessel.patchedConicSolver.maneuverNodes[0];
            return null;
        }
    }
}
