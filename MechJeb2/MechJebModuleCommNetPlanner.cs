extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleCommNetPlanner : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleCommNetPlanner(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowAllVessels = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble MinElevation = new EditableDouble(5);

        public class LinkInfo
        {
            public string vesselName;
            public double distance;
            public double signalStrength;
            public double combinedDsnPower;
            public double vesselAntennaPower;
            public bool hasConnection;
            public bool hasOcclusion;
            public double elevation;
        }

        public List<LinkInfo> Links { get; private set; } = new List<LinkInfo>();
        public bool IsComputing { get; private set; } = false;
        public string StatusText { get; private set; } = "";
        public double VesselAntennaPower { get; private set; }
        public string VesselAntennaName { get; private set; } = "None";

        public void ComputeLinks()
        {
            IsComputing = true;
            StatusText = "Scanning network...";

            try
            {
                Links.Clear();
                ComputeVesselAntennaInfo();

                var vessels = FlightGlobals.Vessels;
                foreach (Vessel v in vessels)
                {
                    if (v == Vessel) continue;
                    if (v.vesselType == VesselType.Debris || v.vesselType == VesselType.SpaceObject || v.vesselType == VesselType.Unknown)
                        continue;
                    if (!ShowAllVessels && !v.IsControllable) continue;

                    LinkInfo link = ComputeLinkToVessel(v);
                    if (link != null)
                        Links.Add(link);
                }

                Links = Links.OrderByDescending(l => l.signalStrength).ToList();

                StatusText = Links.Count > 0
                    ? $"Found {Links.Count} links. Best: {Links[0].signalStrength:P1} to {Links[0].vesselName}"
                    : "No communication links found";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }

            IsComputing = false;
        }

        private void ComputeVesselAntennaInfo()
        {
            double maxPower = 0;
            string bestName = "None";

            foreach (Part part in Vessel.parts)
            {
                foreach (ModuleDataTransmitter xmitter in part.FindModulesImplementing<ModuleDataTransmitter>())
                {
                    double power = xmitter.antennaPower;
                    if (power > maxPower)
                    {
                        maxPower = power;
                        bestName = xmitter.part.partInfo.title;
                    }
                }

                // Also check CommNet antenna components via ModuleDataTransmitter
                // Most deployable antennas also have a ModuleDataTransmitter with the power rating
                // ModuleDeployableAntenna handles the animation/deployment logic
            }

            VesselAntennaPower = maxPower;
            VesselAntennaName = bestName;
        }

        private LinkInfo ComputeLinkToVessel(Vessel target)
        {
            try
            {
                double dist = Vector3d.Distance(Vessel.CoMD, target.CoMD);

                // Check line of sight (occlusion by celestial bodies)
                bool occluded = CheckOcclusion(Vessel.CoMD, target.CoMD);

                // Compute combined DSN power for the target
                double targetPower = 0;
                foreach (Part part in target.parts)
                {
                    foreach (ModuleDataTransmitter xmitter in part.FindModulesImplementing<ModuleDataTransmitter>())
                    {
                        targetPower = Math.Max(targetPower, xmitter.antennaPower);
                    }
                }

                double combinedPower = Math.Sqrt(VesselAntennaPower * targetPower);

                // Compute elevation angle
                double elevation = ComputeElevation(Vessel.CoMD, target.CoMD);

                // Compute signal strength using KSP's model
                double signalStrength = occluded ? 0 : ComputeSignalStrength(dist, combinedPower);

                return new LinkInfo
                {
                    vesselName = target.vesselName,
                    distance = dist,
                    signalStrength = signalStrength,
                    combinedDsnPower = combinedPower,
                    vesselAntennaPower = targetPower,
                    hasConnection = signalStrength > 0.01,
                    hasOcclusion = occluded,
                    elevation = elevation
                };
            }
            catch
            {
                return null;
            }
        }

        private bool CheckOcclusion(Vector3d a, Vector3d b)
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body == Vessel.mainBody) continue;
                if (!body.atmosphere && body.Radius < 1000) continue;

                Vector3d bodyPos = body.position;
                double radius = body.Radius + (body.atmosphere ? body.atmosphereDepth : 0);

                // Check if the line segment a-b intersects the body's sphere
                Vector3d ab = b - a;
                Vector3d ac = bodyPos - a;

                double t = Vector3d.Dot(ac, ab) / Vector3d.Dot(ab, ab);
                t = Math.Min(Math.Max(t, 0), 1);

                Vector3d closest = a + t * ab;
                double distToBody = Vector3d.Distance(closest, bodyPos);

                if (distToBody < radius)
                    return true;
            }

            return false;
        }

        private double ComputeElevation(Vector3d a, Vector3d b)
        {
            Vector3d aUp = (a - Vessel.mainBody.position).normalized;
            Vector3d aToB = (b - a).normalized;
            return 90 - Vector3d.Angle(aUp, aToB);
        }

        private double ComputeSignalStrength(double distance, double combinedPower)
        {
            if (combinedPower <= 0 || distance <= 0) return 0;

            // KSP CommNet signal strength formula
            double ratio = distance / combinedPower;
            double strength = 1.0 - Math.Pow(ratio, 0.5);
            return Math.Max(0, Math.Min(1, strength));
        }
    }
}
