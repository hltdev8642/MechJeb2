extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModule3DTrajectoryWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModule3DTrajectoryWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "3D Trajectory";

        public MechJebModule3DTrajectory Trajectory
        {
            get => Core.Traj3D;
            set => Core.Traj3D = value;
        }

        protected override void WindowGUI(int windowID)
        {
            if (Trajectory == null) return;

            GUILayout.BeginVertical();

            GUILayout.Label("Trajectory Overlay Controls", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

            Trajectory.ShowTrajectory = GUILayout.Toggle(Trajectory.ShowTrajectory, "Show current orbit");
            Trajectory.ShowNodePreview = GUILayout.Toggle(Trajectory.ShowNodePreview, "Show node preview");
            Trajectory.ShowInterceptLine = GUILayout.Toggle(Trajectory.ShowInterceptLine, "Show intercept line");

            GUILayout.Space(5);

            GuiUtils.SimpleTextBox("Duration (days):", Trajectory.TrajectoryDuration, "s");
            GuiUtils.SimpleTextBox("Line thickness:", Trajectory.LineThickness, "");

            if (GUILayout.Button("Refresh"))
            {
                Trajectory.IsDirty = true;
            }

            if (Trajectory.Segments.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Active Trajectories:", GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

                foreach (var seg in Trajectory.Segments)
                {
                    GUILayout.BeginHorizontal();
                    GUI.color = seg.color;
                    GUILayout.Label("•", GUILayout.Width(15));
                    GUI.color = Color.white;
                    GUILayout.Label($"{seg.label} ({seg.points.Count} pts)");
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No trajectories active.\nOpen Map View to display.", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
