/*****************************************************************************
 * RasterPropMonitor
 * =================
 * Plugin for Kerbal Space Program
 *
 *  by Mihara (Eugene Medvedev), MOARdV, and other contributors
 *
 * RasterPropMonitor is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, revision
 * date 29 June 2007, or (at your option) any later version.
 *
 * RasterPropMonitor is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with RasterPropMonitor.  If not, see <http://www.gnu.org/licenses/>.
****************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using MuMech;
using UnityEngine;

namespace JSI
{
    /// <summary>
    /// MechJebProxy handles MechJeb initialization, operation caching,
    /// and complex multi-step MechJeb interactions.
    /// Simple one-liner accessors are called directly on MJ types from MechJebRPM.
    /// </summary>
    public static class MechJebProxy
    {
        #region FieldInfo Cache for Menu Binding
        // Used by MechJebRPM's AddToggleItem(obj, FieldInfo) for simple boolean field toggles.
        internal static FieldInfo f_SmartASS_ForceRol;
        internal static FieldInfo f_Ascent_ForceRoll;
        internal static FieldInfo f_Ascent_LimitAoA;
        internal static FieldInfo f_Ascent_CorrectiveSteering;
        internal static FieldInfo f_Ascent_AutoPath;
        internal static FieldInfo f_Landing_DeployGears;
        internal static FieldInfo f_Landing_DeployChutes;
        internal static FieldInfo f_Landing_UseRCS;
        internal static FieldInfo f_Landing_ShowTrajectory;
        internal static FieldInfo f_Thrust_LimitToPreventOverheats;
        internal static FieldInfo f_Thrust_LimitToTerminalVelocity;
        internal static FieldInfo f_Thrust_LimitDynamicPressure;
        internal static FieldInfo f_Thrust_LimitAcceleration;
        internal static FieldInfo f_Thrust_LimitThrottle;
        internal static FieldInfo f_Thrust_LimitToPreventFlameout;
        internal static FieldInfo f_Thrust_SmoothThrottle;
        internal static FieldInfo f_Thrust_ManageIntakes;
        internal static FieldInfo f_Thrust_DifferentialThrottle;
        internal static FieldInfo f_Thrust_TransKillH;
        internal static FieldInfo f_Node_Autowarp;
        internal static FieldInfo f_Staging_DropSolids;
        internal static FieldInfo f_Docking_ForceRoll;
        internal static FieldInfo f_Docking_OverrideSafeDistance;
        internal static FieldInfo f_Docking_OverrideTargetSize;
        internal static FieldInfo f_Docking_DrawBoundingBox;
        internal static FieldInfo f_Rover_ControlHeading;
        internal static FieldInfo f_Rover_ControlSpeed;
        internal static FieldInfo f_Rover_StabilityControl;
        internal static FieldInfo f_Rover_BrakeOnEject;
        internal static FieldInfo f_Rover_BrakeOnEnergyDepletion;
        internal static FieldInfo f_Rover_WarpToDaylight;
        internal static FieldInfo f_Generic_PlanCapture;
        internal static FieldInfo f_Generic_Coplanar;
        internal static FieldInfo f_InterplanetaryTransfer_WaitForPhaseAngle;
        internal static FieldInfo f_AdvancedTransfer_IncludeCaptureBurn;
        #endregion

        #region Initialization
        private static bool initialized = false;
        private static bool mjAvailable = false;
        private static string initError = null;

        public static bool IsAvailable { get { return mjAvailable; } }
        public static string InitializationError { get { return initError; } }

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            try
            {
                bool found = false;
                foreach (AssemblyLoader.LoadedAssembly la in AssemblyLoader.loadedAssemblies)
                {
                    if (la.assembly.GetName().Name == "MechJeb2")
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    initError = "MechJeb2 assembly not found";
                    return;
                }

                foreach (var op in Operation.GetAvailableOperations())
                {
                    if (op is OperationAdvancedTransfer oat) OpAdvancedTransfer = oat;
                    else if (op is OperationCircularize oc) OpCircularize = oc;
                    else if (op is OperationApoapsis oa) OpChangeApoapsis = oa;
                    else if (op is OperationPeriapsis ope) OpChangePeriapsis = ope;
                    else if (op is OperationSemiMajor osm) OpChangeSemiMajorAxis = osm;
                    else if (op is OperationInclination oi) OpChangeInclination = oi;
                    else if (op is OperationLan ol) OpChangeLAN = ol;
                    else if (op is OperationGeneric og) OpGeneric = og;
                    else if (op is OperationEllipticize oe) OpEllipticize = oe;
                    else if (op is OperationEccentricity oec) OpEccentricity = oec;
                    else if (op is OperationCourseCorrection occ) OpCourseCorrection = occ;
                    else if (op is OperationLambert olm) OpLambert = olm;
                    else if (op is OperationLongitude olo) OpLongitude = olo;
                    else if (op is OperationResonantOrbit oro) OpResonantOrbit = oro;
                    else if (op is OperationMoonReturn omr) OpMoonReturn = omr;
                    else if (op is OperationInterplanetaryTransfer oit) OpInterplanetaryTransfer = oit;
                    else if (op is OperationPlane opl) OpMatchPlane = opl;
                    else if (op is OperationKillRelVel okr) OpMatchVelocity = okr;
                }

                InitializeFieldInfoCache();

                mjAvailable = true;
                JUtil.LogMessage(null, "MechJebProxy: Successfully initialized");
            }
            catch (Exception ex)
            {
                initError = ex.Message;
                JUtil.LogErrorMessage(null, "MechJebProxy initialization failed: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }
        #endregion

        private static void InitializeFieldInfoCache()
        {
            f_SmartASS_ForceRol = typeof(MechJebModuleSmartASS).GetField(nameof(MechJebModuleSmartASS.forceRol), BindingFlags.Public | BindingFlags.Instance);
            f_Ascent_ForceRoll = typeof(MechJebModuleAscentSettings).GetField(nameof(MechJebModuleAscentSettings.ForceRoll), BindingFlags.Public | BindingFlags.Instance);
            f_Ascent_LimitAoA = typeof(MechJebModuleAscentSettings).GetField(nameof(MechJebModuleAscentSettings.LimitAoA), BindingFlags.Public | BindingFlags.Instance);
            f_Ascent_CorrectiveSteering = typeof(MechJebModuleAscentSettings).GetField(nameof(MechJebModuleAscentSettings.CorrectiveSteering), BindingFlags.Public | BindingFlags.Instance);
            f_Ascent_AutoPath = typeof(MechJebModuleAscentSettings).GetField(nameof(MechJebModuleAscentSettings.AutoPath), BindingFlags.Public | BindingFlags.Instance);
            f_Landing_DeployGears = typeof(MechJebModuleLandingAutopilot).GetField(nameof(MechJebModuleLandingAutopilot.DeployGears), BindingFlags.Public | BindingFlags.Instance);
            f_Landing_DeployChutes = typeof(MechJebModuleLandingAutopilot).GetField(nameof(MechJebModuleLandingAutopilot.DeployChutes), BindingFlags.Public | BindingFlags.Instance);
            f_Landing_UseRCS = typeof(MechJebModuleLandingAutopilot).GetField(nameof(MechJebModuleLandingAutopilot.RCSAdjustment), BindingFlags.Public | BindingFlags.Instance);
            f_Landing_ShowTrajectory = typeof(MechJebModuleLandingPredictions).GetField(nameof(MechJebModuleLandingPredictions.showTrajectory), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_LimitToPreventOverheats = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.LimitToPreventOverheats), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_LimitToTerminalVelocity = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.LimitToTerminalVelocity), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_LimitDynamicPressure = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.LimitDynamicPressure), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_LimitAcceleration = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.LimitAcceleration), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_LimitThrottle = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.LimitThrottle), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_LimitToPreventFlameout = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.LimitToPreventFlameout), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_SmoothThrottle = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.SmoothThrottle), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_ManageIntakes = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.ManageIntakes), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_DifferentialThrottle = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.DifferentialThrottle), BindingFlags.Public | BindingFlags.Instance);
            f_Thrust_TransKillH = typeof(MechJebModuleThrustController).GetField(nameof(MechJebModuleThrustController.TransKillH), BindingFlags.Public | BindingFlags.Instance);
            f_Node_Autowarp = typeof(MechJebModuleNodeExecutor).GetField(nameof(MechJebModuleNodeExecutor.Autowarp), BindingFlags.Public | BindingFlags.Instance);
            f_Staging_DropSolids = typeof(MechJebModuleStagingController).GetField(nameof(MechJebModuleStagingController.DropSolids), BindingFlags.Public | BindingFlags.Instance);
            f_Docking_ForceRoll = typeof(MechJebModuleDockingAutopilot).GetField(nameof(MechJebModuleDockingAutopilot.forceRol), BindingFlags.Public | BindingFlags.Instance);
            f_Docking_OverrideSafeDistance = typeof(MechJebModuleDockingAutopilot).GetField(nameof(MechJebModuleDockingAutopilot.overrideSafeDistance), BindingFlags.Public | BindingFlags.Instance);
            f_Docking_OverrideTargetSize = typeof(MechJebModuleDockingAutopilot).GetField(nameof(MechJebModuleDockingAutopilot.overrideTargetSize), BindingFlags.Public | BindingFlags.Instance);
            f_Docking_DrawBoundingBox = typeof(MechJebModuleDockingAutopilot).GetField("drawBoundingBox", BindingFlags.Public | BindingFlags.Instance);
            f_Rover_ControlHeading = typeof(MechJebModuleRoverController).GetField(nameof(MechJebModuleRoverController.ControlHeading), BindingFlags.Public | BindingFlags.Instance);
            f_Rover_ControlSpeed = typeof(MechJebModuleRoverController).GetField(nameof(MechJebModuleRoverController.ControlSpeed), BindingFlags.Public | BindingFlags.Instance);
            f_Rover_StabilityControl = typeof(MechJebModuleRoverController).GetField(nameof(MechJebModuleRoverController.StabilityControl), BindingFlags.Public | BindingFlags.Instance);
            f_Rover_BrakeOnEject = typeof(MechJebModuleRoverController).GetField(nameof(MechJebModuleRoverController.BrakeOnEject), BindingFlags.Public | BindingFlags.Instance);
            f_Rover_BrakeOnEnergyDepletion = typeof(MechJebModuleRoverController).GetField(nameof(MechJebModuleRoverController.BrakeOnEnergyDepletion), BindingFlags.Public | BindingFlags.Instance);
            f_Rover_WarpToDaylight = typeof(MechJebModuleRoverController).GetField(nameof(MechJebModuleRoverController.WarpToDaylight), BindingFlags.Public | BindingFlags.Instance);
            f_Generic_PlanCapture = typeof(OperationGeneric).GetField(nameof(OperationGeneric.PlanCapture), BindingFlags.Public | BindingFlags.Instance);
            f_Generic_Coplanar = typeof(OperationGeneric).GetField(nameof(OperationGeneric.Coplanar), BindingFlags.Public | BindingFlags.Instance);
            f_InterplanetaryTransfer_WaitForPhaseAngle = typeof(OperationInterplanetaryTransfer).GetField(nameof(OperationInterplanetaryTransfer.WaitForPhaseAngle), BindingFlags.Public | BindingFlags.Instance);
            f_AdvancedTransfer_IncludeCaptureBurn = typeof(OperationAdvancedTransfer).GetField("includeCaptureBurn", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        #region Operation Cache
        public static OperationAdvancedTransfer OpAdvancedTransfer { get; private set; }
        public static OperationCircularize OpCircularize { get; private set; }
        public static OperationApoapsis OpChangeApoapsis { get; private set; }
        public static OperationPeriapsis OpChangePeriapsis { get; private set; }
        public static OperationSemiMajor OpChangeSemiMajorAxis { get; private set; }
        public static OperationInclination OpChangeInclination { get; private set; }
        public static OperationLan OpChangeLAN { get; private set; }
        public static OperationGeneric OpGeneric { get; private set; }
        public static OperationEllipticize OpEllipticize { get; private set; }
        public static OperationEccentricity OpEccentricity { get; private set; }
        public static OperationCourseCorrection OpCourseCorrection { get; private set; }
        public static OperationLambert OpLambert { get; private set; }
        public static OperationLongitude OpLongitude { get; private set; }
        public static OperationResonantOrbit OpResonantOrbit { get; private set; }
        public static OperationMoonReturn OpMoonReturn { get; private set; }
        public static OperationInterplanetaryTransfer OpInterplanetaryTransfer { get; private set; }
        public static OperationPlane OpMatchPlane { get; private set; }
        public static OperationKillRelVel OpMatchVelocity { get; private set; }
        #endregion

        #region Autopilot Engagement

        public static void SetAscentAutopilotEngaged(MechJebCore core, bool engaged)
        {
            var autopilot = core?.AscentSettings?.AscentAutopilot;
            if (autopilot == null) return;

            // the stock MJ code uses the ascent menu as the user, so let's do the same to keep in sync
            var user = (object)core.GetComputerModule<MechJebModuleAscentMenu>() ?? core;

            if (engaged)
                autopilot.Users.Add(user);
            else
                autopilot.Users.Remove(user);
        }

        public static void SetRendezvousAutopilotEngaged(MechJebCore core, bool engaged)
        {
            var rendezvous = core?.GetComputerModule<MechJebModuleRendezvousAutopilot>();
            if (rendezvous == null) return;

            var user = (object)core.GetComputerModule<MechJebModuleRendezvousAutopilotWindow>() ?? core;

            if (engaged)
                rendezvous.Users.Add(user);
            else
                rendezvous.Users.Remove(user);
        }

        #endregion

        #region Stage Stats

        public static double GetTotalVacuumDeltaV(MechJebCore core)
        {
            var stats = core.StageStats.VacStats;
            if (stats == null) return 0;
            double total = 0;
            foreach (var stage in stats) total += stage.DeltaV;
            return total;
        }

        public static double GetTotalAtmoDeltaV(MechJebCore core)
        {
            var stats = core.StageStats.AtmoStats;
            if (stats == null) return 0;
            double total = 0;
            foreach (var stage in stats) total += stage.DeltaV;
            return total;
        }
        #endregion

        #region Advanced Transfer (Reflection-based access to private API)

        private static FieldInfo advTransfer_selectionMode;
        private static FieldInfo advTransfer_worker;
        private static FieldInfo advTransfer_lastTargetCelestial;
        private static MethodInfo advTransfer_ComputeTimes;
        private static MethodInfo advTransfer_ComputeStuff;

        private static void InitializeAdvancedTransferReflection()
        {
            if (advTransfer_selectionMode != null) return;
            var t = typeof(OperationAdvancedTransfer);
            advTransfer_selectionMode = t.GetField("selectionMode", BindingFlags.NonPublic | BindingFlags.Instance);
            advTransfer_worker = t.GetField("worker", BindingFlags.NonPublic | BindingFlags.Instance);
            advTransfer_lastTargetCelestial = t.GetField("lastTargetCelestial", BindingFlags.NonPublic | BindingFlags.Instance);
            advTransfer_ComputeTimes = t.GetMethod("ComputeTimes", BindingFlags.NonPublic | BindingFlags.Instance);
            advTransfer_ComputeStuff = t.GetMethod("ComputeStuff", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void StartAdvancedTransferCompute(OperationAdvancedTransfer operation, Orbit orbit, double ut, MechJebModuleTargetController targetController)
        {
            if (operation == null || targetController == null) return;
            InitializeAdvancedTransferReflection();

            Orbit targetOrbit = targetController.TargetOrbit;
            if (targetOrbit == null) return;

            // Set selection mode via reflection
            if (advTransfer_selectionMode != null)
            {
                // Mode.LIMITED_TIME = 0
                advTransfer_selectionMode.SetValue(operation, 0);
            }

            try
            {
                advTransfer_ComputeTimes?.Invoke(operation, new object[] { orbit, targetOrbit, ut });
                advTransfer_ComputeStuff?.Invoke(operation, new object[] { orbit, ut, targetController });
            }
            catch (Exception ex)
            {
                JUtil.LogErrorMessage(null, "AdvancedTransfer compute failed: {0}", ex.Message);
            }
        }

        public static bool IsAdvancedTransferFinished(OperationAdvancedTransfer operation, out int progress)
        {
            progress = 0;
            if (operation == null) return false;
            InitializeAdvancedTransferReflection();

            var worker = advTransfer_worker?.GetValue(operation);
            if (worker == null) return false;

            var progressProp = worker.GetType().GetProperty("Progress");
            var finishedProp = worker.GetType().GetProperty("Finished");
            if (progressProp != null)
                progress = (int)progressProp.GetValue(worker);
            if (finishedProp != null)
                return (bool)finishedProp.GetValue(worker);

            return false;
        }

        public static void SelectAdvancedTransferLowestDV(OperationAdvancedTransfer operation)
        {
            // BestDate/BestDuration on the worker already represent lowest DV by default
        }

        public static void SelectAdvancedTransferASAP(OperationAdvancedTransfer operation)
        {
            if (operation == null) return;
            InitializeAdvancedTransferReflection();

            var worker = advTransfer_worker?.GetValue(operation);
            if (worker == null) return;
            var wt = worker.GetType();

            var computedField = wt.GetField("Computed", BindingFlags.Public | BindingFlags.Instance);
            double[,] computed = computedField != null
                ? computedField.GetValue(worker) as double[,]
                : wt.GetProperty("Computed")?.GetValue(worker) as double[,];
            if (computed == null) return;

            int bestDuration = 0;
            int durationCount = computed.GetLength(1);
            for (int i = 1; i < durationCount; i++)
            {
                if (computed[0, bestDuration] > computed[0, i])
                    bestDuration = i;
            }

            var bestDateField = wt.GetField("BestDate", BindingFlags.Public | BindingFlags.Instance);
            var bestDurField = wt.GetField("BestDuration", BindingFlags.Public | BindingFlags.Instance);
            bestDateField?.SetValue(worker, 0);
            bestDurField?.SetValue(worker, bestDuration);
        }

        public static bool GetAdvancedTransferSelection(OperationAdvancedTransfer operation, out double departureUT, out double duration, out double deltaV)
        {
            departureUT = 0;
            duration = 0;
            deltaV = 0;

            if (operation == null) return false;
            InitializeAdvancedTransferReflection();

            var worker = advTransfer_worker?.GetValue(operation);
            if (worker == null) return false;
            var wt = worker.GetType();

            var bestDateField = wt.GetField("BestDate", BindingFlags.Public | BindingFlags.Instance);
            var bestDurField = wt.GetField("BestDuration", BindingFlags.Public | BindingFlags.Instance);
            int bestDateIdx = bestDateField != null ? (int)bestDateField.GetValue(worker) : 0;
            int bestDurIdx = bestDurField != null ? (int)bestDurField.GetValue(worker) : 0;

            var dateFromIdx = wt.GetMethod("DateFromIndex");
            var durFromIdx = wt.GetMethod("DurationFromIndex");
            if (dateFromIdx != null)
                departureUT = (double)dateFromIdx.Invoke(worker, new object[] { bestDateIdx });
            if (durFromIdx != null)
                duration = (double)durFromIdx.Invoke(worker, new object[] { bestDurIdx });

            var computedField = wt.GetField("Computed", BindingFlags.Public | BindingFlags.Instance);
            var computed = computedField?.GetValue(worker) as double[,];
            if (computed != null && bestDateIdx < computed.GetLength(0) && bestDurIdx < computed.GetLength(1))
            {
                deltaV = computed[bestDateIdx, bestDurIdx];
            }

            return true;
        }
        #endregion

        #region Operation Execution

        public static bool CreateNodesFromOperation(Operation operation, Orbit orbit, double ut, MechJebModuleTargetController targetController, Vessel vessel)
        {
            if (operation == null || vessel == null) return false;

            if (operation is OperationAdvancedTransfer advTransfer)
            {
                CelestialBody targetBody = FlightGlobals.fetch.VesselTarget as CelestialBody;
                if (targetBody != null)
                {
                    InitializeAdvancedTransferReflection();
                    advTransfer_lastTargetCelestial?.SetValue(advTransfer, targetBody);
                }
            }

            var nodes = operation.MakeNodes(orbit, ut, targetController);
            if (nodes == null) return false;

            foreach (var node in nodes)
            {
                if (node == null) continue;
                vessel.PlaceManeuverNode(orbit, node.dV, node.UT);
            }

            return true;
        }

        public static bool ExecuteOperation(Operation operation, MechJebCore core, Vessel vessel)
        {
            if (operation == null || core == null || vessel == null) return false;
            return CreateNodesFromOperation(operation, vessel.orbit, Planetarium.GetUniversalTime(), core.Target, vessel);
        }

        public static TimeSelector GetTimeSelector(this Operation operation)
        {
            FieldInfo field = operation.GetType().GetField("_timeSelector",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null) return null;
            return field.GetValue(null) as TimeSelector;
        }
        #endregion
    }
}
