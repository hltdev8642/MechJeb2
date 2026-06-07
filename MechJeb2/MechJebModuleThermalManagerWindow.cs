extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleThermalManagerWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleThermalManagerWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Thermal Manager";

        public MechJebModuleThermalManager Thermal
        {
            get => Core.ThermalManager;
            set => Core.ThermalManager = value;
        }

        private Vector2 _scrollPosition;

        protected override void WindowGUI(int windowID)
        {
            if (Thermal == null) return;

            GUILayout.BeginVertical();

            GUILayout.Label(Thermal.StatusText, Thermal.CriticalPartCount > 0 ? GuiUtils.RedLabel : GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

            Thermal.AutoRadiatorControl = GUILayout.Toggle(Thermal.AutoRadiatorControl, "Auto radiator control");
            GuiUtils.SimpleTextBox("Warning threshold:", Thermal.OverheatWarningThreshold, "%");
            GuiUtils.SimpleTextBox("Critical threshold:", Thermal.CriticalTempThreshold, "%");

            GUILayout.Space(5);

            if (GUILayout.Button("Refresh"))
            {
                Thermal.UpdateThermalData();
            }

            if (Thermal.PartThermalData.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Part Thermal Status:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(250));

                // Header
                GUILayout.BeginHorizontal();
                GUILayout.Label("Part", GUILayout.Width(110));
                GUILayout.Label("Temp", GUILayout.Width(60));
                GUILayout.Label("Skin", GUILayout.Width(60));
                GUILayout.Label("Flux", GUILayout.Width(45));
                GUILayout.Label("Status", GUILayout.Width(50));
                GUILayout.EndHorizontal();

                foreach (var part in Thermal.PartThermalData)
                {
                    string tempStr = part.currentTemp.ToString("F0") + "/" + part.maxTemp.ToString("F0");
                    string skinStr = part.skinTemp.ToString("F0") + "/" + part.maxTemp.ToString("F0");

                    GUIStyle statusStyle;
                    if (part.isCritical)
                        statusStyle = GuiUtils.RedLabel;
                    else if (part.isOverheating)
                        statusStyle = GuiUtils.YellowLabel;
                    else
                        statusStyle = GuiUtils.GreenLabel;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(part.partName.Length > 18
                        ? part.partName.Substring(0, 18)
                        : part.partName, GUILayout.Width(110));
                    GUILayout.Label(tempStr, GUILayout.Width(60));
                    GUILayout.Label(skinStr, GUILayout.Width(60));
                    GUILayout.Label(part.flux.ToString("F0"), GUILayout.Width(45));

                    string status = part.isCritical ? "CRITICAL" :
                        part.isOverheating ? "WARNING" :
                        part.hasRadiator ? (part.radiatorActive ? "COOLING" : "RAD OFF") : "OK";

                    GUILayout.Label(status, statusStyle, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
