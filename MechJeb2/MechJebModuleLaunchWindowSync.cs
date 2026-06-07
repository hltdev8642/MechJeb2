extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleLaunchWindowSync : ComputerModule
    {
        [UsedImplicitly]
        public MechJebModuleLaunchWindowSync(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool EnableSync = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble TargetPhaseAngle = new EditableDouble(0);

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble PhaseAngleTolerance = new EditableDouble(1.0);

        public string StatusText { get; private set; } = "";

        public double CurrentPhaseAngle { get; private set; }
        public double PhaseAngleError { get; private set; }
        public double TimeToWindow { get; private set; }

        public override void OnUpdate()
        {
            if (!EnableSync || !Core.Target.NormalTargetExists)
            {
                StatusText = EnableSync ? "No target selected" : "Sync disabled";
                return;
            }

            try
            {
                double targetPhase = TargetPhaseAngle;

                // Compute current phase angle between our vessel and the target
                Vector3d vesselPos = Vessel.CoMD;
                Vector3d targetPos = Core.Target.Target.GetOrbit().getRelativePositionAtUT(Planetarium.GetUniversalTime());
                Vector3d bodyPos = Vessel.mainBody.position;

                Vector3d rVessel = vesselPos - bodyPos;
                Vector3d rTarget = targetPos - bodyPos;

                CurrentPhaseAngle = Vector3d.Angle(rVessel, rTarget);

                // Determine leading/trailing
                Vector3d normal = Vector3d.Cross(rVessel, Vessel.orbit.GetOrbitNormal());
                double sign = Math.Sign(Vector3d.Dot(Vector3d.Cross(rVessel, rTarget), normal));
                CurrentPhaseAngle *= sign;

                PhaseAngleError = targetPhase - CurrentPhaseAngle;

                // Estimate time to reach target phase angle using relative mean motion
                double vesselMeanMotion = Vessel.orbit.meanMotion;
                double targetMeanMotion = Core.Target.Target.GetOrbit().meanMotion;
                double relativeMeanMotion = vesselMeanMotion - targetMeanMotion;

                if (Math.Abs(relativeMeanMotion) > 1e-10)
                {
                    TimeToWindow = (PhaseAngleError * Mathf.Deg2Rad) / relativeMeanMotion;
                    if (TimeToWindow < 0)
                        TimeToWindow += Math.Abs(2 * Math.PI / relativeMeanMotion);

                    StatusText = $"Phase: {CurrentPhaseAngle:F1}° | Error: {PhaseAngleError:F1}° | Window in: {TimeToWindow / 60 / 60:F1}h";
                }
                else
                {
                    StatusText = $"Phase: {CurrentPhaseAngle:F1}° | Same orbit period";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
        }

        public void WarpToWindow()
        {
            if (TimeToWindow > 0 && TimeToWindow < 3600 * 24 * 365)
            {
                Core.Warp.WarpToUT(Planetarium.GetUniversalTime() + TimeToWindow);
            }
        }

        public double GetWarpUt()
        {
            if (TimeToWindow > 0 && TimeToWindow < 3600 * 24 * 365)
                return Planetarium.GetUniversalTime() + TimeToWindow;
            return -1;
        }
    }
}
