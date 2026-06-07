extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleLaunchWindowSyncWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleLaunchWindowSyncWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Launch Window Sync";

        public MechJebModuleLaunchWindowSync Sync
        {
            get => Core.LaunchWindowSync;
            set => Core.LaunchWindowSync = value;
        }

        protected override void WindowGUI(int windowID)
        {
            if (Sync == null) return;

            GUILayout.BeginVertical();

            Sync.EnableSync = GUILayout.Toggle(Sync.EnableSync, "Enable Phase Sync");

            GuiUtils.SimpleTextBox("Target Phase Angle:", Sync.TargetPhaseAngle, "°");
            GuiUtils.SimpleTextBox("Tolerance:", Sync.PhaseAngleTolerance, "°");

            GUILayout.Space(10);
            GUILayout.Label(Sync.StatusText, GUILayout.ExpandWidth(true));

            if (Sync.EnableSync && Sync.TimeToWindow > 0)
            {
                if (GUILayout.Button("Warp to Window"))
                {
                    Sync.WarpToWindow();
                }
            }

            // Quick buttons for common phase angles
            GUILayout.Space(10);
            GUILayout.Label("Quick Phase Angles:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-180°")) Sync.TargetPhaseAngle.Val = -180;
            if (GUILayout.Button("-90°"))  Sync.TargetPhaseAngle.Val = -90;
            if (GUILayout.Button("0°"))    Sync.TargetPhaseAngle.Val = 0;
            if (GUILayout.Button("90°"))   Sync.TargetPhaseAngle.Val = 90;
            if (GUILayout.Button("180°"))  Sync.TargetPhaseAngle.Val = 180;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
