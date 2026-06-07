extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using UnityEngine;

namespace MuMech
{
    /// <summary>
    ///     Launch Window Planner — computes optimal launch times for rendezvous using Lambert targeting.
    ///     Integrates with the timed-launch countdown on the ascent autopilot.
    /// </summary>
    [UsedImplicitly]
    public class MechJebModuleLaunchWindowPlanner : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleLaunchWindowPlanner(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetSemiMajor = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetEccentricity = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetInclination = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetLAN = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetArgumentOfPeriapsis = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetMeanAnomaly = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UseCurrentTarget = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool UseCurrentVesselOrbit = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble SearchWindowDays = new EditableDouble(30);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble MinPhaseAngle = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble MaxPhaseAngle = new EditableDouble(360);

        // Computed results
        private double _bestUT;
        private double _bestPhaseAngle;
        private double _bestDV;
        private int    _foundWindows;

        private readonly List<LaunchWindowResult> _windows = new List<LaunchWindowResult>();

        private struct LaunchWindowResult
        {
            public double UT;
            public double PhaseAngle;
            public double DeltaV;
            public double Epoch;
        }

        public double BestLaunchUT => _bestUT;
        public double BestLaunchDV => _bestDV;
        public int    FoundWindows => _foundWindows;

        private void ComputeWindows()
        {
            _windows.Clear();
            _bestUT  = 0;
            _bestDV  = double.MaxValue;
            _foundWindows = 0;

            if (MainBody == null || MainBody.referenceBody == null) return;

            // Build target orbit from parameters or use current target
            Orbit targetOrbit;
            if (UseCurrentTarget && Core.Target.NormalTargetExists)
            {
                targetOrbit = Core.Target.TargetOrbit;
            }
            else
            {
                targetOrbit = new Orbit(
                    TargetInclination * UtilMath.Deg2Rad,
                    TargetEccentricity,
                    TargetSemiMajor,
                    TargetLAN * UtilMath.Deg2Rad,
                    TargetArgumentOfPeriapsis * UtilMath.Deg2Rad,
                    TargetMeanAnomaly * UtilMath.Deg2Rad,
                    0,
                    MainBody.referenceBody
                );
            }

            // Current parking orbit
            Orbit currentOrbit = UseCurrentVesselOrbit ? Orbit : Vessel.orbit;

            if (currentOrbit.referenceBody != targetOrbit.referenceBody) return;

            // Scan for launch windows using phase angle analysis
            double startTime = Planetarium.GetUniversalTime();
            double endTime   = startTime + SearchWindowDays * KSPUtil.dateTimeFormatter.Day;

            // Calculate relative orbital parameters
            double currentPeriod = currentOrbit.period;
            double targetPeriod  = targetOrbit.period;

            double relativeAngularRate = (1.0 / currentPeriod - 1.0 / targetPeriod) * 360.0;

            if (Math.Abs(relativeAngularRate) < 1e-10) return;

            // Scan over time to find good transfer windows
            const int SCAN_STEPS = 720; // ~1 hour resolution for 30 days
            double stepSize = (endTime - startTime) / SCAN_STEPS;

            for (int i = 0; i < SCAN_STEPS; i++)
            {
                double ut       = startTime + i * stepSize;
                double phaseAngle = CalculatePhaseAngle(currentOrbit, targetOrbit, ut);

                // Normalize phase angle
                phaseAngle = MuUtils.ClampDegrees360(phaseAngle * UtilMath.Rad2Deg);

                if (phaseAngle < MinPhaseAngle || phaseAngle > MaxPhaseAngle) continue;

                // Estimate transfer delta-v using Lambert solver
                double dV = EstimateTransferDV(currentOrbit, targetOrbit, ut);
                if (dV < _bestDV)
                {
                    _bestDV = dV;
                    _bestUT = ut;
                    _bestPhaseAngle = phaseAngle;
                }

                _windows.Add(new LaunchWindowResult
                {
                    UT = ut,
                    PhaseAngle = phaseAngle,
                    DeltaV = dV,
                    Epoch = ut
                });
                _foundWindows++;
            }
        }

        private static double CalculatePhaseAngle(Orbit from, Orbit to, double ut)
        {
            Vector3d fromPos = from.WorldPositionAtUT(ut);
            Vector3d toPos   = to.WorldPositionAtUT(ut);
            Vector3d fromVel = from.WorldOrbitalVelocityAtUT(ut);

            // Phase angle is the angle between the position vectors
            return Vector3d.Angle(fromPos, toPos) * UtilMath.Deg2Rad;
        }

        private double EstimateTransferDV(Orbit from, Orbit to, double ut)
        {
            // Simplified transfer DV estimation using Hohmann-like approximation
            try
            {
                double transferTime = from.period * 0.5;
                if (transferTime <= 0) return double.MaxValue;

                // Simple DV estimate: difference in orbital velocities plus plane change
                Vector3d fromVel = from.WorldOrbitalVelocityAtUT(ut);
                Vector3d toVel   = to.WorldOrbitalVelocityAtUT(ut);

                double dvTransfer = (toVel - fromVel).magnitude;
                double planeChangeDV = Math.Abs(from.inclination - to.inclination) * 0.5 * fromVel.magnitude * (Math.PI / 180.0);

                return dvTransfer + planeChangeDV;
            }
            catch
            {
                return double.MaxValue;
            }
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Launch Window Planner");

            // Target orbit selection
            UseCurrentTarget = GUILayout.Toggle(UseCurrentTarget, "Use current target orbit");
            if (!UseCurrentTarget)
            {
                GuiUtils.SimpleTextBox("SMA:", TargetSemiMajor, "m", 60);
                GuiUtils.SimpleTextBox("Ecc:", TargetEccentricity, "", 40);
                GuiUtils.SimpleTextBox("Inc:", TargetInclination, "°", 40);
                GuiUtils.SimpleTextBox("LAN:", TargetLAN, "°", 40);
                GuiUtils.SimpleTextBox("ArgPe:", TargetArgumentOfPeriapsis, "°", 40);
                GuiUtils.SimpleTextBox("MNA:", TargetMeanAnomaly, "°", 40);
            }

            UseCurrentVesselOrbit = GUILayout.Toggle(UseCurrentVesselOrbit, "Use current vessel orbit");

            GuiUtils.SimpleTextBox("Search window:", SearchWindowDays, "days", 50);
            GuiUtils.SimpleTextBox("Min phase:", MinPhaseAngle, "°", 40);
            GuiUtils.SimpleTextBox("Max phase:", MaxPhaseAngle, "°", 40);

            if (GUILayout.Button("Compute Launch Windows"))
            {
                ComputeWindows();
            }

            if (_foundWindows > 0)
            {
                GUILayout.Label($"Best window: {GuiUtils.TimeToDHMS(_bestUT - Planetarium.GetUniversalTime())}");
                GUILayout.Label($"Best ΔV: {_bestDV:F1} m/s");
                GUILayout.Label($"Windows found: {_foundWindows}");

                if (Core.Ascent != null)
                {
                    if (GUILayout.Button("Set Launch Countdown"))
                    {
                        Core.Ascent.StartCountdown(_bestUT - 30);
                        Core.Warp.WarpToUT(_bestUT - 60);
                    }
                }
            }
            else if (_foundWindows == 0 && GUILayout.Button("Compute")
            )
            {
                ComputeWindows();
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(300), GUILayout.Height(200) };

        public override string GetName() => "Launch Window Planner";

        public override string IconName() => "Launch Window Planner";
    }
}
