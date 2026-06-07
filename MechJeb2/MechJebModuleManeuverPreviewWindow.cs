extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleManeuverPreviewWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleManeuverPreviewWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Maneuver Preview";

        public MechJebModuleManeuverPreview Preview
        {
            get => Core.ManeuverPreview;
            set => Core.ManeuverPreview = value;
        }

        protected override void WindowGUI(int windowID)
        {
            if (Preview == null) return;

            GUILayout.BeginVertical();

            Preview.ShowPostBurnOrbit = GUILayout.Toggle(Preview.ShowPostBurnOrbit, "Show post-burn orbit");
            Preview.ShowInterceptInfo = GUILayout.Toggle(Preview.ShowInterceptInfo, "Show intercept info");
            Preview.ShowDeltaVBreakdown = GUILayout.Toggle(Preview.ShowDeltaVBreakdown, "Show Δv breakdown");

            GUILayout.Space(5);

            if (GUILayout.Button("Refresh Analysis"))
            {
                Preview.AnalyzeCurrentNode();
            }

            if (Preview.CurrentAnalysis != null)
            {
                var a = Preview.CurrentAnalysis;
                GUILayout.Label(Preview.StatusText, GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

                GUILayout.Space(10);
                GUILayout.Label("Δv Breakdown:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label($"  Total Δv:       {a.deltaV:F1} m/s");
                GUILayout.Label($"  Prograde:       {a.progradeComponent:F1} m/s");
                GUILayout.Label($"  Normal:         {a.normalComponent:F1} m/s");
                GUILayout.Label($"  Radial:         {a.radialComponent:F1} m/s");

                GUILayout.Space(5);
                GUILayout.Label("Post-Burn Orbit:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label($"  Periapsis:      {a.postBurnPeriapsis / 1000:F1} km");
                GUILayout.Label($"  Apoapsis:       {a.postBurnApoapsis / 1000:F1} km");
                GUILayout.Label($"  Inclination:    {a.postBurnInclination:F2}°");

                if (a.isEscapeTrajectory)
                {
                    GUILayout.Label("  ** ESCAPE TRAJECTORY **", GuiUtils.RedLabel, GUILayout.ExpandWidth(true));
                    GUILayout.Label($"  Ejection angle: {a.ejectionAngle:F1}°");
                }

                if (a.hasIntercept)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Intercept:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
                    GUILayout.Label($"  Target:         {a.targetBodyName}");
                    GUILayout.Label($"  Pe altitude:    {a.interceptDistance / 1000:F1} km");
                    GUILayout.Label($"  Time to go:     {TimeSpan.FromSeconds(a.timeToNextEncounter):g}");
                }

                GUILayout.Space(10);

                // Trajectory sample info
                if (a.trajectoryPoints.Count > 0)
                {
                    GUILayout.Label($"Trajectory samples: {a.trajectoryPoints.Count}");
                }

                if (GUILayout.Button("Copy to Clipboard"))
                {
                    string report = $"Maneuver Analysis @ T+{a.burnUt:F0}s\n" +
                                    $"Δv: {a.deltaV:F1} m/s\n" +
                                    $"Post Pe: {a.postBurnPeriapsis / 1000:F1} km\n" +
                                    $"Post Ap: {a.postBurnApoapsis / 1000:F1} km\n" +
                                    $"Inc: {a.postBurnInclination:F2}°\n" +
                                    $"Escape: {a.isEscapeTrajectory}";
                    GUIUtility.systemCopyBuffer = report;
                }
            }
            else
            {
                if (Preview.GetActiveNode() == null)
                {
                    GUILayout.Label("No maneuver node exists.\nCreate a node to see analysis.", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
                }
                else
                {
                    GUILayout.Label(Preview.StatusText, GUILayout.ExpandWidth(true));
                }
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
