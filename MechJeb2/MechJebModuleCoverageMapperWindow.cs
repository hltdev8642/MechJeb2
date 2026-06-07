extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleCoverageMapperWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleCoverageMapperWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Coverage Mapper";

        public MechJebModuleCoverageMapper Mapper
        {
            get => Core.CoverageMapper;
            set => Core.CoverageMapper = value;
        }

        private Vector2 _scrollPos;

        protected override void WindowGUI(int windowID)
        {
            if (Mapper == null) return;

            GUILayout.BeginVertical();

            GUILayout.Label("Satellite Coverage Analysis", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Body:", GUILayout.Width(50));
            Mapper.TargetBody = GUILayout.TextField(Mapper.TargetBody, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GuiUtils.SimpleTextBox("Min elevation:", Mapper.MinElevationAngle, "°");
            GuiUtils.SimpleTextBox("Scan duration:", Mapper.ScanDuration, "s");

            Mapper.ShowCoverageOverlay = GUILayout.Toggle(Mapper.ShowCoverageOverlay, "Show overlay");

            if (!Mapper.IsComputing)
            {
                if (GUILayout.Button("Compute Coverage"))
                {
                    Mapper.ComputeCoverage();
                }
            }
            else
            {
                GUILayout.Label("Computing... (this may take a moment)");
            }

            GUILayout.Label(Mapper.StatusText, GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

            if (Mapper.CurrentCoverage != null)
            {
                var cov = Mapper.CurrentCoverage;
                GUILayout.Space(10);

                GUILayout.Label("Coverage Summary:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label($"  Body: {cov.bodyName}");
                GUILayout.Label($"  Average coverage: {cov.totalCoveragePercent:F1}%");
                GUILayout.Label($"  Avg revisit time: {cov.averageRevisitTime / 60:F1} min");
                GUILayout.Label($"  Max revisit time: {cov.maxRevisitTime / 60:F1} min");
                GUILayout.Label($"  Contributing vessels: {cov.contributingVessels.Count}");

                GUILayout.Space(5);
                if (cov.contributingVessels.Count > 0)
                {
                    GUILayout.Label("Vessels:", GUILayout.ExpandWidth(true));
                    foreach (string v in cov.contributingVessels)
                    {
                        GUILayout.Label($"  • {v}", GUILayout.ExpandWidth(true));
                    }
                }

                // Coverage cell table (simplified - show by latitude band)
                if (cov.cells.Count > 0)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Coverage by Latitude:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Lat", GUILayout.Width(40));
                    GUILayout.Label("Coverage", GUILayout.Width(70));
                    GUILayout.Label("Revisit", GUILayout.Width(70));
                    GUILayout.Label("Passes/day", GUILayout.Width(70));
                    GUILayout.EndHorizontal();

                    // Group by latitude bands
                    var latGroups = cov.cells
                        .GroupBy(c => Math.Round(c.latitude / 10) * 10)
                        .OrderBy(g => g.Key);

                    foreach (var group in latGroups)
                    {
                        double avgCov = group.Average(c => c.coveragePercent);
                        double avgRev = group.Average(c => c.averageRevisitTime);
                        double avgPasses = group.Average(c => c.passesPerDay);

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{group.Key:F0}°", GUILayout.Width(40));
                        GUILayout.Label($"{avgCov:F1}%", GUILayout.Width(70));
                        GUILayout.Label($"{avgRev / 60:F1}min", GUILayout.Width(70));
                        GUILayout.Label($"{avgPasses:F1}", GUILayout.Width(70));
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndScrollView();
                }
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
