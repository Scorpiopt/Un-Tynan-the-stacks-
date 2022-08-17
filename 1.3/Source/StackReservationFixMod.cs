using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    public class StackReservationFixMod : Mod
    {
        public static bool deepStorageLoaded;
        public static Harmony harmony;
        public StackReservationFixMod(ModContentPack pack) : base(pack)
        {
            harmony = new Harmony("StackReservationFix.Mod");
            harmony.PatchAll();
            deepStorageLoaded = ModsConfig.IsActive("LWM.DeepStorage");
        }
    }

    //[HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    //public class StartJobPatch
    //{
    //    private static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn, Job newJob, JobTag? tag)
    //    {
    //        try
    //        {
    //            ___pawn.jobs.debugLog = ___pawn.ShouldLog();
    //        }
    //        catch { }
    //    }
    //}
    
    
    
    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    public class EndCurrentJobPatch
    {
        private static void Prefix(out Pawn __state, Pawn_JobTracker __instance, Pawn ___pawn, JobCondition condition, ref bool startNewJob, bool canReturnToPool = true)
        {
            __state = JobGiver_Work_TryIssueJobPackage_Patch.curPawn;
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = ___pawn;
            if (___pawn.jobs?.jobQueue?.jobs is null || ___pawn.jobs.jobQueue.jobs.Any(x => x.job.def.defName == "HaulToInventory") is false)
            {
                if (Helpers.haulers.TryGetValue(___pawn, out var list))
                {
                    list.thingsToHaul.RemoveAll(x => x.Key.ParentHolder is not Pawn_InventoryTracker);
                }
            }
        }
        public static void Postfix(Pawn __state)
        {
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = __state;
        }
    }

    [HarmonyPatch]
    public static class Toils_Haul_CheckForGetOpportunityDuplicate_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase GetMethod()
        {
            return typeof(Toils_Haul).GetNestedTypes(AccessTools.all).SelectMany(x => x.GetMethods(AccessTools.all).Where(x => x.Name.Contains("<CheckForGetOpportunityDuplicate>"))).ToList()[1];
        }
        public static void Prefix(Pawn ___actor, out Pawn __state)
        {
            __state = JobGiver_Work_TryIssueJobPackage_Patch.curPawn;
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = ___actor;
        }

        public static void Postfix(Pawn __state)
        {
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = __state;
        }
    }

    [HarmonyPatch]
    public static class JobDriver_HaulToCell_FailCondition_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase GetMethod()
        {
            return typeof(JobDriver_HaulToCell).GetNestedTypes(AccessTools.all).First().GetMethods(AccessTools.all)
                .FirstOrDefault(x => x.Name.Contains("<MakeNewToils>"));
        }
        public static void Prefix(Toil ___toilGoto, out Pawn __state)
        {
            __state = JobGiver_Work_TryIssueJobPackage_Patch.curPawn;
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = ___toilGoto.actor;
        }

        public static void Postfix(Pawn __state)
        {
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = __state;
        }
    }

    [HarmonyPatch(typeof(StoreUtility), "TryFindBestBetterStoreCellForWorker")]
    public static class StoreUtility_TryFindBestBetterStoreCellForWorker_Patch
    {
        public static void Prefix(Thing t, Pawn carrier, out Pawn __state)
        {
            __state = JobGiver_Work_TryIssueJobPackage_Patch.curPawn;
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = carrier;
        }
        public static void Postfix(Pawn __state)
        {
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = __state;
        }
    }

    [HarmonyPatch(typeof(StoreUtility), "IsGoodStoreCell")]
    public static class StoreUtility_IsGoodStoreCell_Patch
    {
        public static void Prefix(Thing t, Pawn carrier, out Pawn __state)
        {
            __state = JobGiver_Work_TryIssueJobPackage_Patch.curPawn;
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = carrier;
        }
        private static void Postfix(ref bool __result, IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction, Pawn __state)
        {
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = __state;
            if (__result)
            {
                Log.Message("IsGoodStoreCell --------------------- " + carrier + " - t: " + t + " job " + carrier.CurJob.JobSummary(carrier) + " - " + new StackTrace());
                var job = carrier.CurJob;
                bool carryingJob = job.IsHaulingJob();
                if (StackReservationFixMod.deepStorageLoaded
                    && DeepStorageHelper.HasDeepStorageAndCanUse(t, carryingJob ? job : null, carrier, c, out var canUse))
                {
                    __result = canUse;
                    Log.Message("good cell: " + c + " - " + __result + " for " + t);
                    if (__result)
                    {
                        Helpers.thingsByCell[t] = c;

                        Log.Message("Registering " + t + " for " + c);
                    }
                }
                Log.Message("---------------------");
            }
        }
    }

    [HarmonyPatch(typeof(ThingOwner<Thing>), "TryAdd", new Type[]
    {
        typeof(Thing),
        typeof(bool)
    })]
    public static class ThingOwner_TryAdd_Patch
    {
        public static void Postfix(ThingOwner<Thing> __instance, bool __result, Thing item)
        {
            if (__result)
            {
                if (__instance.Owner is Pawn_CarryTracker equipmentTracker)
                {
                    Log.Message(equipmentTracker.pawn + " is carrying " + item + " stackCount: " + item.stackCount + " - job: " + equipmentTracker.pawn.CurJob.JobSummary(equipmentTracker.pawn) + new StackTrace());
                }
                else if (__instance.Owner is Pawn_InventoryTracker inventoryTracker)
                {
                    Log.Message(inventoryTracker.pawn + " grabbed " + item + " stackCount: " + item.stackCount + " - " + " job: " + inventoryTracker.pawn.CurJob.JobSummary(inventoryTracker.pawn) + new StackTrace());
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                }
            }
        }
    }

    [HarmonyPatch(typeof(JobQueue), "EnqueueFirst")]
    public static class JobQueue_EnqueueFirst_Patch
    {
        public static void Postfix(Job j)
        {
            Log.Message("Enqueue first: " + j.JobSummary(null) + " - " + new StackTrace());
        }
    }

    [HarmonyPatch(typeof(JobQueue), "EnqueueLast")]
    public static class JobQueue_EnqueueLast_Patch
    {
        public static void Postfix(Job j)
        {
            Log.Message("Enqueue last: " + j.JobSummary(null) + " - " + new StackTrace());
        }
    }

    [HarmonyPatch(typeof(StoreUtility), "IsValidStorageFor")]
    public static class StoreUtility_IsValidStorageFor_Patch
    {
        public static void Prefix(IntVec3 c, Map map, Thing storable, out Pawn __state)
        {
            __state = JobGiver_Work_TryIssueJobPackage_Patch.curPawn;
            if (storable.ParentHolder is Pawn_CarryTracker carryTracker)
            {
                JobGiver_Work_TryIssueJobPackage_Patch.curPawn = carryTracker.pawn;
            }
        }
        public static void Postfix(Pawn __state)
        {
            JobGiver_Work_TryIssueJobPackage_Patch.curPawn = __state;
        }
    }
    [HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
    public class JobGiver_Work_TryIssueJobPackage_Patch
    {
        public static Pawn curPawn;
        private static void Prefix(ThinkNode_JobGiver __instance, ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
        {
            curPawn = pawn;
            Log.Message("pawn: " + curPawn);
        }

        private static void Postfix(ThinkNode_JobGiver __instance, ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
        {
            curPawn = null;
            Log.Message("pawn: " + curPawn);
            try
            {
                pawn.jobs.debugLog = pawn.ShouldLog();
                if (pawn.ShouldLog() && __result.Job != null)
                {
                    Log.Message(pawn + " gets " + __result.Job + " from " + __instance);
                }
            }
            catch { }
        }
    }
    //
    [StaticConstructorOnStartup]
    public static class Startup2
    {
        public static bool ShouldLog(this Pawn pawn) => pawn.IsColonist;
        static Startup2()
        {
            //var postfix = AccessTools.Method(typeof(Startup2), "Postfix");
            //foreach (var type2 in typeof(ThinkNode).AllSubclassesNonAbstract())
            //{
            //    var methodToPatch2 = AccessTools.Method(type2, "TryIssueJobPackage");
            //    try
            //    {
            //        StackReservationFixMod.harmony.Patch(methodToPatch2, postfix: new HarmonyMethod(postfix));
            //    }
            //    catch { }
            //}
            //var postfix2 = AccessTools.Method(typeof(Startup2), "Postfix2");
            //foreach (var type3 in typeof(ThinkNode_Conditional).AllSubclassesNonAbstract())
            //{
            //    var methodToPatch3 = AccessTools.Method(type3, "Satisfied");
            //    try
            //    {
            //        StackReservationFixMod.harmony.Patch(methodToPatch3, postfix: new HarmonyMethod(postfix2));
            //    }
            //    catch { }
            //}
            //
            //var postfix3 = AccessTools.Method(typeof(Startup2), "Postfix3");
            //foreach (var type3 in typeof(WorkGiver_Scanner).AllSubclassesNonAbstract())
            //{
            //    var methodToPatch3 = AccessTools.Method(type3, "HasJobOnThing");
            //    try
            //    {
            //        StackReservationFixMod.harmony.Patch(methodToPatch3, postfix: new HarmonyMethod(postfix3));
            //    }
            //    catch { }
            //}
            //
            //var postfix4 = AccessTools.Method(typeof(Startup2), "Postfix4");
            //foreach (var type3 in typeof(WorkGiver_Scanner).AllSubclassesNonAbstract())
            //{
            //    var methodToPatch3 = AccessTools.Method(type3, "JobOnThing");
            //    try
            //    {
            //        StackReservationFixMod.harmony.Patch(methodToPatch3, postfix: new HarmonyMethod(postfix4));
            //    }
            //    catch { }
            //}
        }
    
        private static void Postfix(ThinkNode __instance, ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
        {
            if (pawn.ShouldLog())
            {
                Log.Message(pawn + " gets " + __result.Job + " from " + __instance);
                if (__instance is ThinkNode_Subtree subtree)
                {
                    Log.Message("Subtree: " + subtree.treeDef);
                }
            }
        }
        
        private static void Postfix2(ThinkNode_Conditional __instance, bool __result, Pawn pawn)
        {
            if (pawn.ShouldLog())
            {
                Log.Message(pawn + " gets " + __result + " from " + __instance);
            }
        }
        
        private static void Postfix3(WorkGiver_Scanner __instance, bool __result, Pawn pawn, Thing thing, bool forced = false)
        {    
            if (pawn.ShouldLog())
            {
                Log.Message(pawn + " TEST gets " + __result + " from " + __instance);
            }
        }
    
        private static void Postfix4(WorkGiver_Scanner __instance, Job __result, Pawn pawn)
        {
            if (pawn.ShouldLog())
            {
                Log.Message(pawn + " gets " + __result + " from " + __instance);
            }
        }
    }
}
