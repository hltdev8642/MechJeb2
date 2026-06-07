extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModule3DTrajectory : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModule3DTrajectory(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowTrajectory = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowInterceptLine = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowNodePreview = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TrajectoryDuration = new EditableDouble(86400 * 7); // 7 days

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble LineThickness = new EditableDouble(1.5);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int SampleCount = 360;

        public class TrajectorySegment
        {
            public List<Vector3d> points = new List<Vector3d>();
            public Color color = Color.green;
            public string label = "";
        }

        public List<TrajectorySegment> Segments { get; private set; } = new List<TrajectorySegment>();
        public bool IsDirty { get; set; } = true;

        public override void OnUpdate()
        {
            if (!Enabled || !ShowTrajectory) return;

            if (IsDirty || MapView.MapIsEnabled)
            {
                ComputeTrajectory();
                IsDirty = false;
            }
        }

        public void ComputeTrajectory()
        {
            Segments.Clear();

            if (!MapView.MapIsEnabled) return;

            try
            {
                double ut = Planetarium.GetUniversalTime();

                // Current orbit segment
                if (Vessel.orbit != null)
                {
                    var orbitSeg = new TrajectorySegment
                    {
                        color = Color.green,
                        label = "Current orbit"
                    };

                    double step = TrajectoryDuration / SampleCount;
                    for (int i = 0; i < SampleCount; i++)
                    {
                        double sampleUt = ut + i * step;
                        Vector3d pos = Vessel.orbit.getRelativePositionAtUT(sampleUt);
                        orbitSeg.points.Add(pos);
                    }
                    Segments.Add(orbitSeg);
                }

                // Maneuver node preview segments
                if (ShowNodePreview && Vessel.patchedConicSolver != null)
                {
                    foreach (ManeuverNode node in Vessel.patchedConicSolver.maneuverNodes)
                    {
                        var nodeSeg = new TrajectorySegment
                        {
                            color = Color.cyan,
                            label = $"Node @ T+{(node.UT - ut) / 60:F0}min"
                        };

                        double step = TrajectoryDuration / SampleCount;
                        for (int i = 0; i < SampleCount; i++)
                        {
                            double sampleUt = node.UT + i * step;
                            try
                            {
                                Vector3d pos = node.patch.getRelativePositionAtUT(sampleUt);
                                nodeSeg.points.Add(pos);
                            }
                            catch { break; }
                        }
                        Segments.Add(nodeSeg);
                    }
                }

                // Target intercept line
                if (ShowInterceptLine && Core.Target.NormalTargetExists)
                {
                    var interceptSeg = new TrajectorySegment
                    {
                        color = Color.magenta,
                        label = $"To {Core.Target.Target.GetVessel()?.vesselName ?? "target"}"
                    };

                    // Line from our vessel to target
                    Vector3d ourPos = Vessel.CoMD;
                    Vector3d targetPos = Core.Target.RelativePosition + Vessel.CoMD;

                    int steps = 20;
                    for (int i = 0; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        Vector3d point = Vector3d.Lerp(ourPos, targetPos, t);
                        interceptSeg.points.Add(point);
                    }
                    Segments.Add(interceptSeg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MechJeb 3DTrajectory error: {ex.Message}");
            }
        }

        // Note: 3D trajectory rendering requires KSP's map view drawing API.
        // This module provides data for the preview window; actual 3D rendering
        // would use MapView.OnPreRender or a stock-orbit-drawer approach.
    }
}
