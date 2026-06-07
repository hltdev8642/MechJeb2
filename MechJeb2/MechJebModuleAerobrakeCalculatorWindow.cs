extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleAerobrakeCalculatorWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleAerobrakeCalculatorWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Aerobrake Calculator";

        public MechJebModuleAerobrakeCalculator Aerobrake
        {
            get => Core.Aerobrake;
            set => Core.Aerobrake = value;
        }

        protected override void WindowGUI(int windowID)
        {
            if (Aerobrake == null) return;

            GUILayout.BeginVertical();

            GuiUtils.SimpleTextBox("Target Ap:", Aerobrake.TargetPeakApoapsis, "m");
            GuiUtils.SimpleTextBox("PE step:", Aerobrake.PeriapsisStep, "m");

            if (!Aerobrake.IsComputing)
            {
                if (GUILayout.Button("Compute Aerobrake Profiles"))
                {
                    Aerobrake.ComputeAerobrakePredictions();
                }
            }
            else
            {
                GUILayout.Label("Computing...");
            }

            GUILayout.Label(Aerobrake.StatusText, GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

            if (Aerobrake.Passes.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Aerobrake Profiles:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

                GUILayout.BeginHorizontal();
                GUILayout.Label("Pe (m)", GUILayout.Width(80));
                GUILayout.Label("Ap (m)", GUILayout.Width(100));
                GUILayout.Label("q (kPa)", GUILayout.Width(70));
                GUILayout.Label("Heat", GUILayout.Width(60));
                GUILayout.Label("Status", GUILayout.Width(60));
                GUILayout.EndHorizontal();

                foreach (var pass in Aerobrake.Passes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pass.periapsisAltitude.ToString("F0"), GUILayout.Width(80));
                    GUILayout.Label(pass.resultingApoapsis.ToString("F0"), GUILayout.Width(100));
                    GUILayout.Label(pass.peakDynamicPressure.ToString("F1"), GUILayout.Width(70));
                    GUILayout.Label(pass.peakHeating.ToString("F1"), GUILayout.Width(60));
                    GUILayout.Label(pass.isCapture ? "Capture" : "Flyby", GUILayout.Width(60));
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(10);

                var optimal = Aerobrake.FindOptimalPass();
                if (optimal != null)
                {
                    GUILayout.Label("Optimal for target Ap:", GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));
                    GUILayout.Label($"  Pe: {optimal.periapsisAltitude:F0}m -> Ap: {optimal.resultingApoapsis:F0}m");
                    GUILayout.Label($"  Peak heating: {optimal.peakHeating:F2}");
                    GUILayout.Label($"  Max Q: {optimal.peakDynamicPressure:F1} kPa");
                }
            }

            Aerobrake.ShowThermalData = GUILayout.Toggle(Aerobrake.ShowThermalData, "Show thermal data");

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
