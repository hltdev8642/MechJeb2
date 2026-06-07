extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    /// <summary>
    ///     RCS Translation Hold — maintains the vessel's current surface-relative velocity vector
    ///     ("cruise control" for translation) even without a target selected.
    ///     Useful for rover waypoint navigation and precision orbital translation.
    /// </summary>
    [UsedImplicitly]
    public class MechJebModuleRCSHold : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleRCSHold(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))]
        public bool HoldHorizontalVelocity = true;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))]
        public bool HoldVerticalVelocity = true;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))]
        public readonly EditableDouble HoldSpeed = new EditableDouble(5.0);

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))]
        public readonly EditableDouble SpeedTolerance = new EditableDouble(0.5);

        private Vector3d _targetVelocity;
        private bool     _velocityCaptured;

        public string Status
        {
            get
            {
                if (!Enabled) return "Off";
                if (!_velocityCaptured) return "Initializing...";
                return $"Holding {_targetVelocity.magnitude:F1} m/s";
            }
        }

        protected override void OnModuleEnabled()
        {
            _velocityCaptured = false;
            Core.RCS.Users.Add(this);
        }

        protected override void OnModuleDisabled()
        {
            Core.RCS.Users.Remove(this);
            Core.RCS.rcsDeactivate();
            _velocityCaptured = false;
        }

        public override void OnFixedUpdate()
        {
            if (!Enabled) return;

            // Capture current velocity on first tick after activation
            if (!_velocityCaptured)
            {
                if (VesselState.surfaceVelocity.magnitude > 0.01)
                {
                    _targetVelocity = VesselState.surfaceVelocity;
                    _velocityCaptured = true;
                }
                return;
            }

            // Decompose target velocity into horizontal and vertical components
            Vector3d verticalComponent   = Vector3d.Project(_targetVelocity, VesselState.up);
            Vector3d horizontalComponent = _targetVelocity - verticalComponent;

            // Build desired velocity based on which axes are held
            Vector3d desiredVelocity = Vector3d.zero;

            if (HoldHorizontalVelocity)
                desiredVelocity += horizontalComponent;
            else
                desiredVelocity += Vector3d.Exclude(VesselState.up, VesselState.surfaceVelocity);

            if (HoldVerticalVelocity)
                desiredVelocity += verticalComponent;
            else
                desiredVelocity += Vector3d.Project(VesselState.surfaceVelocity, VesselState.up);

            // Clamp to hold speed
            if (desiredVelocity.magnitude > HoldSpeed)
                desiredVelocity = desiredVelocity.normalized * HoldSpeed;

            // Compute error and set RCS target
            Vector3d velocityError = desiredVelocity - VesselState.surfaceVelocity;

            // Don't waste RCS if we're within tolerance
            if (velocityError.magnitude < SpeedTolerance)
            {
                Core.RCS.rcsDeactivate();
                return;
            }

            Core.RCS.targetVelocity = desiredVelocity;
        }

        // Called from the window to capture a new target velocity
        public void CaptureCurrentVelocity()
        {
            if (VesselState.surfaceVelocity.magnitude > 0.01)
            {
                _targetVelocity = VesselState.surfaceVelocity;
                _velocityCaptured = true;
            }
        }

        // Called from the window to zero out target velocity
        public void ZeroTargetVelocity()
        {
            _targetVelocity = Vector3d.zero;
            _velocityCaptured = true;
        }
    }
}
