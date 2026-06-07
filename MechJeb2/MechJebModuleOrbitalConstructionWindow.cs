extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleOrbitalConstructionWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleOrbitalConstructionWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Orbital Construction";

        public MechJebModuleOrbitalConstruction Construction
        {
            get => Core.OrbitalConstruction;
            set => Core.OrbitalConstruction = value;
        }

        private Vector2 _scrollPos;

        protected override void WindowGUI(int windowID)
        {
            if (Construction == null) return;

            GUILayout.BeginVertical();

            GUILayout.Label("Orbital Construction Planner", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

            // Target orbit config
            GUILayout.BeginHorizontal();
            GUILayout.Label("Body:", GUILayout.Width(50));
            Construction.TargetCelestialBody = GUILayout.TextField(Construction.TargetCelestialBody, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GuiUtils.SimpleTextBox("Orbit altitude:", Construction.TargetOrbitAltitude, "m");
            GuiUtils.SimpleTextBox("Inclination:", Construction.TargetInclination, "°");

            GUILayout.Space(5);

            // Create plan
            GUILayout.BeginHorizontal();
            Construction.NewStationNameLabel = GUILayout.TextField(Construction.NewStationNameLabel, GUILayout.Width(150));
            if (GUILayout.Button("Create Plan"))
            {
                Construction.CreatePlan(Construction.NewStationNameLabel);
            }
            GUILayout.EndHorizontal();

            Construction.AutoSequenceConstruction = GUILayout.Toggle(Construction.AutoSequenceConstruction, "Auto-sequence");

            GUILayout.Space(5);
            GUILayout.Label(Construction.StatusText, GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

            if (Construction.ActivePlan != null)
            {
                GUILayout.Space(10);
                GUILayout.Label($"Plan: {Construction.ActivePlan.stationName}", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label($"  Orbit: {Construction.ActivePlan.targetBody} @ {Construction.ActivePlan.targetAltitude / 1000:F0} km");

                // Add step buttons
                GUILayout.BeginHorizontal();
                Construction.NewPayloadInput = GUILayout.TextField(Construction.NewPayloadInput, GUILayout.Width(120));
                if (GUILayout.Button("Add Launch", GUILayout.ExpandWidth(false)))
                {
                    Construction.AddLaunchStep(Construction.NewPayloadInput);
                    Construction.NewPayloadInput = "";
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Construction.NewVesselInput = GUILayout.TextField(Construction.NewVesselInput, GUILayout.Width(120));
                if (GUILayout.Button("Add Rendezvous", GUILayout.ExpandWidth(false)))
                {
                    Construction.AddRendezvousStep(Construction.NewVesselInput);
                    Construction.NewVesselInput = "";
                }
                GUILayout.EndHorizontal();

                // Steps list
                if (Construction.ActivePlan.steps.Count > 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Steps:", GUILayout.ExpandWidth(true));

                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(150));

                    for (int i = 0; i < Construction.ActivePlan.steps.Count; i++)
                    {
                        var step = Construction.ActivePlan.steps[i];
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"#{i + 1}", GUILayout.Width(25));
                        GUILayout.Label(step.moduleName, GUILayout.Width(150));
                        GUILayout.Label(step.isComplete ? "✓" : "○", GUILayout.Width(20));
                        if (!step.isComplete && GUILayout.Button("Complete", GUILayout.Width(70)))
                        {
                            Construction.CompleteStep(i);
                        }
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndScrollView();

                    // Execute next button
                    if (Construction.PendingStepsCount > 0)
                    {
                        if (GUILayout.Button("Execute Next Step"))
                        {
                            Construction.ExecuteNextStep();
                        }
                    }
                }
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
