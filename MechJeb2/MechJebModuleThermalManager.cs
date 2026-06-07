extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleThermalManager : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleThermalManager(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool AutoRadiatorControl = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble OverheatWarningThreshold = new EditableDouble(0.85);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble CriticalTempThreshold = new EditableDouble(0.95);

        public class PartThermalInfo
        {
            public string partName;
            public double currentTemp;
            public double maxTemp;
            public double skinTemp;
            public double maxSkinTemp;
            public double tempFraction;
            public double skinTempFraction;
            public bool isOverheating;
            public bool isCritical;
            public bool hasRadiator;
            public bool radiatorActive;
            public double flux;
        }

        public List<PartThermalInfo> PartThermalData { get; private set; } = new List<PartThermalInfo>();
        public string StatusText { get; private set; } = "";
        public int OverheatingPartCount { get; private set; }
        public int CriticalPartCount { get; private set; }

        public override void OnUpdate()
        {
            if (!Enabled) return;

            UpdateThermalData();
            if (AutoRadiatorControl)
                ManageRadiators();
        }

        public void UpdateThermalData()
        {
            PartThermalData.Clear();
            OverheatingPartCount = 0;
            CriticalPartCount = 0;

            foreach (Part part in Vessel.parts)
            {
                if (part.State == PartStates.DEAD) continue;

                var info = new PartThermalInfo
                {
                    partName = part.partInfo.title,
                    currentTemp = part.temperature,
                    maxTemp = part.maxTemp,
                    skinTemp = part.skinTemperature,
                    maxSkinTemp = part.maxTemp,
                    tempFraction = part.maxTemp > 0 ? part.temperature / part.maxTemp : 0,
                    skinTempFraction = part.maxTemp > 0 ? part.skinTemperature / part.maxTemp : 0,
                    isOverheating = false,
                    isCritical = false,
                    hasRadiator = false,
                    radiatorActive = false,
                    flux = 0
                };

                // Check for ModuleActiveRadiator
                foreach (ModuleActiveRadiator rad in part.FindModulesImplementing<ModuleActiveRadiator>())
                {
                    info.hasRadiator = true;
                    info.radiatorActive = rad.IsCooling;
                }

                double warningFrac = OverheatWarningThreshold;
                double criticalFrac = CriticalTempThreshold;

                if (info.tempFraction >= warningFrac || info.skinTempFraction >= warningFrac)
                {
                    info.isOverheating = true;
                    OverheatingPartCount++;
                }

                if (info.tempFraction >= criticalFrac || info.skinTempFraction >= criticalFrac)
                {
                    info.isCritical = true;
                    CriticalPartCount++;
                }

                PartThermalData.Add(info);
            }

            // Sort by temperature fraction descending
            PartThermalData = PartThermalData.OrderByDescending(p => p.tempFraction).ToList();

            StatusText = $"Parts: {PartThermalData.Count} | Overheating: {OverheatingPartCount}" +
                         (CriticalPartCount > 0 ? $" | CRITICAL: {CriticalPartCount}" : "");
        }

        private void ManageRadiators()
        {
            bool hasOverheating = OverheatingPartCount > 0;

            foreach (Part part in Vessel.parts)
            {
                foreach (ModuleActiveRadiator rad in part.FindModulesImplementing<ModuleActiveRadiator>())
                {
                    if (hasOverheating)
                    {
                        if (!rad.IsCooling) rad.Activate();
                    }
                    else
                    {
                        if (rad.IsCooling) rad.Shutdown();
                    }
                }
            }
        }

        public List<PartThermalInfo> GetOverheatingParts()
        {
            return PartThermalData.Where(p => p.isOverheating).ToList();
        }

        public List<PartThermalInfo> GetCriticalParts()
        {
            return PartThermalData.Where(p => p.isCritical).ToList();
        }
    }
}
