extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    /// <summary>
    /// Planning tool for orbital construction — station assembly, module placement,
    /// and orbital rendezvous sequencing.
    /// </summary>
    public class MechJebModuleOrbitalConstruction : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleOrbitalConstruction(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool AutoSequenceConstruction = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble StationKeepRadius = new EditableDouble(200);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetOrbitAltitude = new EditableDouble(400000);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetInclination = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public string TargetCelestialBody = "Kerbin";

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool CircularizeAfterDock = true;

        public class ConstructionStep
        {
            public string moduleName;
            public double targetUt;
            public string status;
            public bool isComplete;
        }

        public class OrbitalConstructionPlan
        {
            public string stationName;
            public double targetAltitude;
            public double targetInclination;
            public string targetBody;
            public List<ConstructionStep> steps = new List<ConstructionStep>();
        }

        public OrbitalConstructionPlan ActivePlan { get; private set; }
        public string StatusText { get; private set; } = "";
        public string NewStationNameLabel { get; set; } = "New Station";
        public string NewPayloadInput { get; set; } = "";
        public string NewVesselInput { get; set; } = "";
        public int PendingStepsCount => _pendingSteps.Count;

        private readonly List<ConstructionStep> _pendingSteps = new List<ConstructionStep>();

        public void CreatePlan(string stationName)
        {
            ActivePlan = new OrbitalConstructionPlan
            {
                stationName = stationName,
                targetAltitude = TargetOrbitAltitude,
                targetInclination = TargetInclination,
                targetBody = TargetCelestialBody
            };

            _pendingSteps.Clear();
            StatusText = $"Created plan for '{stationName}' at {TargetOrbitAltitude / 1000:F0} km";
        }

        public void AddLaunchStep(string payloadName)
        {
            if (ActivePlan == null) return;

            var step = new ConstructionStep
            {
                moduleName = $"Launch: {payloadName}",
                targetUt = 0,
                status = "Pending",
                isComplete = false
            };

            ActivePlan.steps.Add(step);
            _pendingSteps.Add(step);
            StatusText = $"Added launch step: {payloadName}";
        }

        public void AddRendezvousStep(string targetVessel)
        {
            if (ActivePlan == null) return;

            var step = new ConstructionStep
            {
                moduleName = $"Rendezvous: {targetVessel}",
                targetUt = 0,
                status = "Pending",
                isComplete = false
            };

            ActivePlan.steps.Add(step);
            _pendingSteps.Add(step);
            StatusText = $"Added rendezvous step: {targetVessel}";
        }

        public void CompleteStep(int index)
        {
            if (ActivePlan == null || index < 0 || index >= ActivePlan.steps.Count) return;

            ActivePlan.steps[index].isComplete = true;
            ActivePlan.steps[index].status = "Complete";
            _pendingSteps.Remove(ActivePlan.steps[index]);

            StatusText = $"Step {index + 1} completed";
        }

        public void ExecuteNextStep()
        {
            var nextStep = _pendingSteps.FirstOrDefault();
            if (nextStep == null)
            {
                StatusText = "All steps complete";
                return;
            }

            // Auto-execute based on step type
            if (nextStep.moduleName.StartsWith("Launch:"))
            {
                // Set up ascent autopilot for the target orbit
                Core.AscentSettings.DesiredOrbitAltitude.Val = TargetOrbitAltitude;
                Core.AscentSettings.DesiredInclination.Val = TargetInclination;
                StatusText = $"Configured ascent for: {nextStep.moduleName}";
            }
            else if (nextStep.moduleName.StartsWith("Rendezvous:"))
            {
                // Enable rendezvous autopilot
                var rendezvous = Core.GetComputerModule("MechJebModuleRendezvousAutopilot");
                if (rendezvous != null)
                {
                    rendezvous.Enabled = true;
                    rendezvous.Users.Add(this);
                }
                StatusText = $"Starting rendezvous: {nextStep.moduleName}";
            }
        }

        public override void OnUpdate()
        {
            if (!Enabled || ActivePlan == null) return;

            // Auto-sequence construction steps
            if (AutoSequenceConstruction && _pendingSteps.Count > 0)
            {
                ExecuteNextStep();
            }
        }
    }
}
