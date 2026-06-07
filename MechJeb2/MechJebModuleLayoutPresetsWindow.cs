extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleLayoutPresetsWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleLayoutPresetsWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Layout Presets";

        public MechJebModuleLayoutPresets Presets
        {
            get => Core.LayoutPresets;
            set => Core.LayoutPresets = value;
        }

        private Vector2 _scrollPos;

        protected override void WindowGUI(int windowID)
        {
            if (Presets == null) return;

            GUILayout.BeginVertical();

            GUILayout.Label("Save/Load Window Layouts", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

            GUILayout.Space(5);

            // Save section
            GUILayout.BeginHorizontal();
            if (Presets.NewPresetName == null) Presets.NewPresetName = "";
            Presets.NewPresetName = GUILayout.TextField(Presets.NewPresetName, GUILayout.Width(150));
            if (GUILayout.Button("Save Current Layout"))
            {
                Presets.SaveCurrentLayout(Presets.NewPresetName);
                Presets.NewPresetName = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label(Presets.StatusText, GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

            // Presets list
            if (Presets.Presets.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Saved Layouts:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));

                foreach (var preset in Presets.Presets.OrderByDescending(p => p.timestamp))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(preset.name, GUILayout.Width(120));
                    GUILayout.Label($"{preset.enabledModules.Count} windows", GUILayout.Width(80));
                    GUILayout.Label(preset.timestamp, GUILayout.Width(100));

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Load", GUILayout.Width(50)))
                    {
                        Presets.LoadLayout(preset.name);
                    }

                    if (GUILayout.Button("Del", GUILayout.Width(40)))
                    {
                        Presets.DeletePreset(preset.name);
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("No saved layouts yet.", GUILayout.ExpandWidth(true));
            }

            // Quick presets
            GUILayout.Space(10);
            GUILayout.Label("Quick Actions:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save as 'Default'"))
            {
                Presets.SaveCurrentLayout("Default");
            }
            if (GUILayout.Button("Save as 'Docking'"))
            {
                Presets.SaveCurrentLayout("Docking");
            }
            if (GUILayout.Button("Save as 'Ascent'"))
            {
                Presets.SaveCurrentLayout("Ascent");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
