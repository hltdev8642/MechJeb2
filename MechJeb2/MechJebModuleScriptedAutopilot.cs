extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    /// <summary>
    /// A sequenced autopilot that executes a series of configurable steps:
    /// launch, circularize, transfer, rendezvous, dock, etc.
    /// Each step can be configured with parameters and conditions.
    /// </summary>
    public class MechJebModuleScriptedAutopilot : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleScriptedAutopilot(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool LoopSequence = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool AutoExecute = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int CurrentStepIndex = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public string SequenceName = "New Sequence";

        public enum StepType
        {
            WAIT,
            SET_ORBIT,
            EXECUTE_NODE,
            LAUNCH_ASCENT,
            CIRCULARIZE,
            TRANSFER,
            RENDEZVOUS,
            DOCK,
            LAND,
            WARP_TO_UT,
            SET_THROTTLE,
            RUN_MODULE,
            LOG_MESSAGE
        }

        [Serializable]
        public class SequenceStep
        {
            public StepType type = StepType.WAIT;
            public string name = "Step";
            public string description = "";
            public double param1;     // e.g. duration, altitude, throttle
            public double param2;     // e.g. inclination, speed limit
            public string targetBody = "";
            public string moduleToRun = "";
            public bool waitForCompletion = true;
            public double timeout = 300;
            public bool isComplete;
            public string status = "Pending";
        }

        public List<SequenceStep> Steps { get; private set; } = new List<SequenceStep>();
        public string StatusText { get; private set; } = "";
        public bool IsRunning { get; private set; } = false;

        private double _stepStartTime;

        public void StartSequence()
        {
            if (Steps.Count == 0)
            {
                StatusText = "No steps in sequence";
                return;
            }

            CurrentStepIndex = 0;
            IsRunning = true;
            foreach (var step in Steps)
            {
                step.isComplete = false;
                step.status = "Pending";
            }

            StatusText = $"Started sequence '{SequenceName}'";
            _stepStartTime = Planetarium.GetUniversalTime();
            EnableForCurrentStep();
        }

        public void StopSequence()
        {
            IsRunning = false;
            StatusText = "Sequence stopped";
            DisableActiveModules();
        }

        public void AddStep(StepType type, string name)
        {
            var step = new SequenceStep
            {
                type = type,
                name = name,
                status = "Pending"
            };
            Steps.Add(step);
            StatusText = $"Added step: {name}";
        }

        public void RemoveStep(int index)
        {
            if (index >= 0 && index < Steps.Count)
            {
                Steps.RemoveAt(index);
                if (CurrentStepIndex >= Steps.Count)
                    CurrentStepIndex = Steps.Count - 1;
                StatusText = $"Removed step {index + 1}";
            }
        }

        public void MoveStepUp(int index)
        {
            if (index > 0 && index < Steps.Count)
            {
                var temp = Steps[index];
                Steps[index] = Steps[index - 1];
                Steps[index - 1] = temp;
            }
        }

        public void MoveStepDown(int index)
        {
            if (index >= 0 && index < Steps.Count - 1)
            {
                var temp = Steps[index];
                Steps[index] = Steps[index + 1];
                Steps[index + 1] = temp;
            }
        }

        public void AdvanceToNextStep()
        {
            if (CurrentStepIndex < Steps.Count)
            {
                Steps[CurrentStepIndex].isComplete = true;
                Steps[CurrentStepIndex].status = "Complete";
            }

            CurrentStepIndex++;

            if (CurrentStepIndex >= Steps.Count)
            {
                if (LoopSequence)
                {
                    CurrentStepIndex = 0;
                    foreach (var step in Steps)
                    {
                        step.isComplete = false;
                        step.status = "Pending";
                    }
                    StatusText = "Looping sequence";
                }
                else
                {
                    IsRunning = false;
                    StatusText = "Sequence complete!";
                    return;
                }
            }

            _stepStartTime = Planetarium.GetUniversalTime();
            EnableForCurrentStep();
        }

        private void EnableForCurrentStep()
        {
            if (CurrentStepIndex < 0 || CurrentStepIndex >= Steps.Count) return;

            var step = Steps[CurrentStepIndex];
            step.status = "Running";
            step.isComplete = false;

            switch (step.type)
            {
                case StepType.LAUNCH_ASCENT:
                    Core.AscentSettings.DesiredOrbitAltitude.Val = step.param1;
                    Core.AscentSettings.DesiredInclination.Val = step.param2;
                    var ascent = Core.GetComputerModule("MechJebModuleAscentAutopilot");
                    if (ascent != null)
                    {
                        ascent.Enabled = true;
                        ascent.Users.Add(this);
                    }
                    break;

                case StepType.CIRCULARIZE:
                    Core.Node.Enabled = true;
                    Core.Node.Users.Add(this);
                    break;

                case StepType.RENDEZVOUS:
                    var rendezvous = Core.GetComputerModule("MechJebModuleRendezvousAutopilot");
                    if (rendezvous != null)
                    {
                        rendezvous.Enabled = true;
                        rendezvous.Users.Add(this);
                    }
                    break;

                case StepType.DOCK:
                    var docking = Core.GetComputerModule("MechJebModuleDockingAutopilot");
                    if (docking != null)
                    {
                        docking.Enabled = true;
                        docking.Users.Add(this);
                    }
                    break;

                case StepType.LAND:
                    Core.Landing.Enabled = true;
                    Core.Landing.Users.Add(this);
                    break;

                case StepType.RUN_MODULE:
                    if (!string.IsNullOrEmpty(step.moduleToRun))
                    {
                        var module = Core.GetComputerModule(step.moduleToRun);
                        if (module != null)
                        {
                            module.Enabled = true;
                            module.Users.Add(this);
                        }
                    }
                    break;
            }

            StatusText = $"Step {CurrentStepIndex + 1}/{Steps.Count}: {step.name} ({step.type})";
        }

        private void DisableActiveModules()
        {
            foreach (var step in Steps)
            {
                if (!step.isComplete)
                    step.status = "Stopped";
            }

            // Disable modules we enabled
            Core.GetComputerModules<ComputerModule>()
                .Where(m => m.Users.Contains(this))
                .ToList()
                .ForEach(m => m.Users.Remove(this));
        }

        public override void OnUpdate()
        {
            if (!Enabled || !IsRunning) return;

            var step = GetCurrentStep();
            if (step == null)
            {
                AdvanceToNextStep();
                return;
            }

            // Check timeout
            double elapsed = Planetarium.GetUniversalTime() - _stepStartTime;
            if (elapsed > step.timeout && step.timeout > 0)
            {
                step.status = $"Timeout ({elapsed:F0}s)";
                AdvanceToNextStep();
                return;
            }

            // Step-specific completion checks
            bool completed = CheckStepCompletion(step);
            if (completed)
            {
                AdvanceToNextStep();
            }

            // Update status
            if (step != null && CurrentStepIndex < Steps.Count)
            {
                StatusText = $"Step {CurrentStepIndex + 1}/{Steps.Count}: {step.name} [{step.status}] ({elapsed:F0}s)";
            }
        }

        private bool CheckStepCompletion(SequenceStep step)
        {
            switch (step.type)
            {
                case StepType.WAIT:
                    double elapsed = Planetarium.GetUniversalTime() - _stepStartTime;
                    return elapsed >= step.param1;

                case StepType.LAUNCH_ASCENT:
                    // Check if we've reached target apoapsis
                    return Vessel.orbit != null && Vessel.orbit.ApA >= step.param1 * 0.95;

                case StepType.CIRCULARIZE:
                    return Vessel.orbit != null &&
                           Math.Abs(Vessel.orbit.eccentricity) < 0.01 &&
                           Core.Node.Enabled == false;

                case StepType.EXECUTE_NODE:
                    return Vessel.patchedConicSolver == null ||
                           Vessel.patchedConicSolver.maneuverNodes.Count == 0;

                case StepType.WARP_TO_UT:
                    return Planetarium.GetUniversalTime() >= step.param1;

                case StepType.LAND:
                    return Vessel.LandedOrSplashed;

                case StepType.DOCK:
                    var docking = Core.GetComputerModule("MechJebModuleDockingAutopilot");
                    return docking == null || !docking.Enabled;

                case StepType.SET_ORBIT:
                    return Vessel.orbit != null &&
                           Vessel.orbit.PeA >= step.param1 * 0.95 &&
                           Vessel.orbit.ApA <= step.param2 * 1.05;

                default:
                    return step.waitForCompletion && !step.isComplete;
            }
        }

        public SequenceStep GetCurrentStep()
        {
            if (CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count)
                return Steps[CurrentStepIndex];
            return null;
        }

        public void ClearSteps()
        {
            Steps.Clear();
            CurrentStepIndex = 0;
            IsRunning = false;
            StatusText = "Sequence cleared";
        }
    }
}
