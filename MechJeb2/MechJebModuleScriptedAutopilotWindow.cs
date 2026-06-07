extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleScriptedAutopilotWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleScriptedAutopilotWindow(MechJebCore core) : base(core) { }

        public override string GetName() => "Scripted Autopilot";

        public MechJebModuleScriptedAutopilot Sequencer
        {
            get => Core.ScriptedAutopilot;
            set => Core.ScriptedAutopilot = value;
        }

        private Vector2 _scrollPos;
        private string _newStepName = "";
        private MechJebModuleScriptedAutopilot.StepType _newStepType = MechJebModuleScriptedAutopilot.StepType.WAIT;

        protected override void WindowGUI(int windowID)
        {
            if (Sequencer == null) return;

            GUILayout.BeginVertical();

            GUILayout.Label("Scripted Autopilot Sequencer", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

            // Sequence name
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(45));
            Sequencer.SequenceName = GUILayout.TextField(Sequencer.SequenceName, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            // Control buttons
            GUILayout.BeginHorizontal();
            GUI.color = Sequencer.IsRunning ? Color.red : Color.green;

            if (GUILayout.Button(Sequencer.IsRunning ? "Stop" : "Start"))
            {
                if (Sequencer.IsRunning)
                    Sequencer.StopSequence();
                else
                    Sequencer.StartSequence();
            }

            GUI.color = Color.white;

            if (!Sequencer.IsRunning && GUILayout.Button("Clear All"))
            {
                Sequencer.ClearSteps();
            }

            GUILayout.EndHorizontal();

            Sequencer.LoopSequence = GUILayout.Toggle(Sequencer.LoopSequence, "Loop");
            Sequencer.AutoExecute = GUILayout.Toggle(Sequencer.AutoExecute, "Auto-execute");

            GUILayout.Space(5);
            GUILayout.Label(Sequencer.StatusText, GuiUtils.GreenLabel, GUILayout.ExpandWidth(true));

            // Add step
            GUILayout.Space(5);
            GUILayout.Label("Add Step:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            _newStepName = GUILayout.TextField(_newStepName, GUILayout.Width(120));
            _newStepType = (MechJebModuleScriptedAutopilot.StepType)
                GUILayout.SelectionGrid((int)_newStepType,
                    Enum.GetNames(typeof(MechJebModuleScriptedAutopilot.StepType)), 4,
                    GUILayout.Height(60));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Add Step"))
            {
                if (string.IsNullOrEmpty(_newStepName))
                    _newStepName = _newStepType.ToString();
                Sequencer.AddStep(_newStepType, _newStepName);
                _newStepName = "";
            }

            // Step list
            if (Sequencer.Steps.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Steps:", GuiUtils.YellowLabel, GUILayout.ExpandWidth(true));

                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(250));

                for (int i = 0; i < Sequencer.Steps.Count; i++)
                {
                    var step = Sequencer.Steps[i];
                    bool isActive = i == Sequencer.CurrentStepIndex && Sequencer.IsRunning;

                    GUI.color = isActive ? Color.cyan :
                        step.isComplete ? Color.green : Color.white;

                    GUILayout.BeginVertical(GUI.skin.box);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"#{i + 1}", GUILayout.Width(25));
                    GUILayout.Label(step.name, GUILayout.Width(100));
                    GUILayout.Label(step.type.ToString(), GUILayout.Width(80));
                    GUILayout.Label(step.status, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    // Step parameters (show relevant ones)
                    GUILayout.BeginHorizontal();
                    if (step.type == MechJebModuleScriptedAutopilot.StepType.WAIT)
                    {
                        GUILayout.Label("Duration:", GUILayout.Width(60));
                        step.param1 = double.Parse(GUILayout.TextField(step.param1.ToString("F0"), GUILayout.Width(60)));
                        GUILayout.Label("s");
                    }
                    else if (step.type == MechJebModuleScriptedAutopilot.StepType.LAUNCH_ASCENT)
                    {
                        GUILayout.Label("Alt:", GUILayout.Width(35));
                        step.param1 = double.Parse(GUILayout.TextField(step.param1.ToString("F0"), GUILayout.Width(60)));
                        GUILayout.Label("Inc:", GUILayout.Width(30));
                        step.param2 = double.Parse(GUILayout.TextField(step.param2.ToString("F1"), GUILayout.Width(50)));
                    }
                    else if (step.type == MechJebModuleScriptedAutopilot.StepType.SET_ORBIT)
                    {
                        GUILayout.Label("Pe:", GUILayout.Width(25));
                        step.param1 = double.Parse(GUILayout.TextField(step.param1.ToString("F0"), GUILayout.Width(60)));
                        GUILayout.Label("Ap:", GUILayout.Width(25));
                        step.param2 = double.Parse(GUILayout.TextField(step.param2.ToString("F0"), GUILayout.Width(60)));
                    }
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                        Sequencer.MoveStepUp(i);
                    if (GUILayout.Button("↓", GUILayout.Width(25)))
                        Sequencer.MoveStepDown(i);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                        Sequencer.RemoveStep(i);

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    GUI.color = Color.white;
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }
    }
}
