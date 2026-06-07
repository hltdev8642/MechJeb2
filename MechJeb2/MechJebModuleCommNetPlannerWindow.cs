extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleCommNetPlannerWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleCommNetPlannerWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "CommNet Planner";

        public MechJebModuleCommNetPlanner Planner
        {
            get => Core.CommNetPlanner;
            set => Core.CommNetPlanner = value;
        }

        protected override void WindowGUI(int windowID)
        {
            if (Planner == null) return;

            GUILayout.BeginVertical();

            // Vessel antenna info
            GUILayout.Label("Vessel Antenna:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label($"  {Planner.VesselAntennaName}");
            GUILayout.Label($"  Power: {Planner.VesselAntennaPower:F0} m");

            GUILayout.Space(5);

            Planner.ShowAllVessels = GUILayout.Toggle(Planner.ShowAllVessels, "Show all vessels");
            GuiUtils.SimpleTextBox("Min elevation:", Planner.MinElevation, "°");

            if (!Planner.IsComputing)
            {
                if (GUILayout.Button("Scan CommNet"))
                {
                    Planner.ComputeLinks();
                }
            }
            else
            {
                GUILayout.Label("Scanning...");
            }

            GUILayout.Label(Planner.StatusText, GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

            if (Planner.Links.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Links:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

                // Header
                GUILayout.BeginHorizontal();
                GUILayout.Label("Vessel", GUILayout.Width(100));
                GUILayout.Label("Signal", GUILayout.Width(60));
                GUILayout.Label("Dist (km)", GUILayout.Width(70));
                GUILayout.Label("Elev", GUILayout.Width(40));
                GUILayout.Label("Status", GUILayout.Width(50));
                GUILayout.EndHorizontal();

                foreach (var link in Planner.Links)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(link.vesselName.Length > 15
                        ? link.vesselName.Substring(0, 15)
                        : link.vesselName, GUILayout.Width(100));
                    GUILayout.Label(link.signalStrength.ToString("P0"), GUILayout.Width(60));
                    GUILayout.Label((link.distance / 1000).ToString("F0"), GUILayout.Width(70));
                    GUILayout.Label(link.elevation.ToString("F0"), GUILayout.Width(40));
                    GUILayout.Label(link.hasOcclusion ? "BLOCKED" : "OK", GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
