extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleRCSHoldWindow : DisplayModule
    {
        [UsedImplicitly]
        public MechJebModuleRCSHoldWindow(MechJebCore core) : base(core) { }

        private MechJebModuleRCSHold _hold;

        public override void OnStart(PartModule.StartState state)
        {
            _hold = Core.GetComputerModule<MechJebModuleRCSHold>();
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (_hold == null)
            {
                GUILayout.Label("RCS Hold module not available.");
                base.WindowGUI(windowID);
                return;
            }

            // Engage / Disengage
            if (!_hold.Enabled)
            {
                if (GUILayout.Button("Engage RCS Hold"))
                    _hold.Enabled = true;
            }
            else
            {
                if (GUILayout.Button("Disengage RCS Hold"))
                    _hold.Enabled = false;
            }

            if (_hold.Enabled)
            {
                GUILayout.Label("Status: " + _hold.Status);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Capture Current"))
                    _hold.CaptureCurrentVelocity();
                if (GUILayout.Button("Zero Velocity"))
                    _hold.ZeroTargetVelocity();
                GUILayout.EndHorizontal();

                GuiUtils.SimpleTextBox("Hold Speed:", _hold.HoldSpeed, "m/s", 50);
                GuiUtils.SimpleTextBox("Tolerance:", _hold.SpeedTolerance, "m/s", 50);

                _hold.HoldHorizontalVelocity =
                    GUILayout.Toggle(_hold.HoldHorizontalVelocity, "Hold Horizontal Velocity");
                _hold.HoldVerticalVelocity =
                    GUILayout.Toggle(_hold.HoldVerticalVelocity, "Hold Vertical Velocity");

                GUILayout.Label("Target: " + _hold.Status);
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(250), GUILayout.Height(100) };

        public override string GetName() => "RCS Hold";

        public override string IconName() => "RCS Hold";
    }
}
