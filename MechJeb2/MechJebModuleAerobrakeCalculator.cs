extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleAerobrakeCalculator : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleAerobrakeCalculator(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowThermalData = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetPeakApoapsis = new EditableDouble(120000);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble PeriapsisStep = new EditableDouble(1000);

        public class AerobrakePass
        {
            public double periapsisAltitude;
            public double resultingApoapsis;
            public double peakDynamicPressure;
            public double peakHeating;
            public double peakDeceleration;
            public double timeToImpact;
            public bool isCapture;
        }

        public List<AerobrakePass> Passes { get; private set; } = new List<AerobrakePass>();
        public string StatusText { get; private set; } = "";
        public bool IsComputing { get; private set; } = false;

        public void ComputeAerobrakePredictions()
        {
            IsComputing = true;
            StatusText = "Computing...";

            try
            {
                Passes.Clear();
                double currentPe = Vessel.orbit.PeA;
                double currentAp = Vessel.orbit.ApA;
                double currentSMA = Vessel.orbit.semiMajorAxis;

                // Compute the range of periapsis altitudes to test
                double bodyRadius = Vessel.mainBody.Radius;
                double atmosLimit = Vessel.mainBody.atmosphereDepth;

                double startPe = Math.Max(bodyRadius + 5000, currentPe - 20000);
                double endPe = Math.Min(bodyRadius + atmosLimit - 5000, currentPe + 20000);
                double step = PeriapsisStep;

                // Simulate passes at each periapsis altitude
                for (double peAlt = startPe; peAlt <= endPe; peAlt += step)
                {
                    AerobrakePass pass = SimulateAerobrakePass(peAlt - bodyRadius);
                    if (pass != null)
                    {
                        Passes.Add(pass);
                    }
                }

                // Sort by resulting apoapsis
                Passes = Passes.OrderBy(p => p.resultingApoapsis).ToList();

                StatusText = Passes.Count > 0
                    ? $"Computed {Passes.Count} profiles. Best: Pe={Passes[0].periapsisAltitude:F0}m, Ap={Passes[0].resultingApoapsis:F0}m"
                    : "No viable aerobrake solutions found";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }

            IsComputing = false;
        }

        private AerobrakePass SimulateAerobrakePass(double periapsisAltitude)
        {
            // Simplified aerobrake simulation using orbital mechanics
            double bodyRadius = Vessel.mainBody.Radius;
            double mu = Vessel.mainBody.gravParameter;
            double atmosDepth = Vessel.mainBody.atmosphereDepth;

            double rPe = bodyRadius + periapsisAltitude;
            double rAp = Vessel.orbit.ApR;

            if (rPe >= rAp) return null;

            // Compute velocity at periapsis using vis-viva
            double a = (rPe + rAp) / 2.0;
            double vPe = Math.Sqrt(mu * (2.0 / rPe - 1.0 / a));

            // Compute atmospheric density at periapsis using KSP's model
            double altitudeASL = periapsisAltitude;
            double pressure = altitudeASL < atmosDepth
                ? FlightGlobals.getStaticPressure(altitudeASL, Vessel.mainBody)
                : 0;
            double temperature = FlightGlobals.getExternalTemperature(altitudeASL, Vessel.mainBody);
            double density = FlightGlobals.getAtmDensity(pressure, temperature);

            // Compute dynamic pressure
            double q = 0.5 * density * vPe * vPe;

            // Simplified heating rate (approximate convective heating)
            double heatingRate = 1.0e-8 * Math.Pow(vPe, 3) * Math.Sqrt(Math.Max(density, 0));

            // Deceleration
            double decel = 0.5 * density * vPe * vPe;

            // Simplified delta-v from drag (impulsive approximation)
            double dragMultiplier = 0.001;
            double dvDrag = decel * dragMultiplier * vPe;

            // Compute resulting orbit after drag
            double newVPe = vPe - dvDrag;

            // New apoapsis after drag at periapsis
            double newA = rPe / (2.0 - (newVPe * newVPe * rPe / mu));
            double newApR = 2.0 * newA - rPe;
            double newApAlt = newApR - bodyRadius;

            bool isCapture = newApR < bodyRadius + atmosDepth && newApR > rPe;

            return new AerobrakePass
            {
                periapsisAltitude = periapsisAltitude,
                resultingApoapsis = Math.Max(newApAlt, 0),
                peakDynamicPressure = q / 1000.0, // kPa
                peakHeating = heatingRate,
                peakDeceleration = decel / 1000.0,
                timeToImpact = 0,
                isCapture = isCapture
            };
        }

        public AerobrakePass FindOptimalPass()
        {
            if (Passes.Count == 0) return null;

            // Find the pass that gets us closest to the target apoapsis
            return Passes
                .OrderBy(p => Math.Abs(p.resultingApoapsis - TargetPeakApoapsis))
                .FirstOrDefault();
        }
    }
}
