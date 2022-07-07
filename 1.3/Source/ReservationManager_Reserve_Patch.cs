using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    //[HarmonyPatch(typeof(GenSpawn), "Spawn", new System.Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) })]
    //public static class GenSpawn_Spawn_Patch
    //{
    //    public static void Postfix(Thing __result, bool respawningAfterLoad)
    //    {
    //        if (__result.def.IsMedicine)
    //        {
    //            if (__result.Position.GetFirstThing<Building_Storage>(__result.Map) is null)
    //            {
    //                Log.Message("Spawning medifice in " + __result.Position);
    //                //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
    //            }
    //        }
    //    }
    //}
    //
    //[HarmonyPatch(typeof(JobQueue), "EnqueueFirst")]
    //public static class JobQueue_EnqueueuFirst_Patch
    //{
    //    public static void Postfix(Job j)
    //    {
    //        Log.Message(j + " was queued first");
    //    }
    //}
    //
    //[HarmonyPatch(typeof(JobQueue), "EnqueueLast")]
    //public static class JobQueue_EnqueueuLast_Patch
    //{
    //    public static void Postfix(Job j)
    //    {
    //        Log.Message(j + " was queued last");
    //    }
    //}

    [HarmonyPatch(typeof(StoreUtility), "IsGoodStoreCell")]
    public static class StoreUtility_IsGoodStoreCell_Patch
    {
        private static void Postfix(ref bool __result, IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction)
        {
            if (__result)
            {
                var job = carrier.CurJob;
                bool carryingJob = job.IsHaulingJob();
                Log.Message(job + " - carryingJob: " + carryingJob + " - t: " + t);
                if (StackReservationFixMod.deepStorageLoaded
                    && DeepStorageHelper.HasDeepStorageAndCanUse(!carryingJob ? Mathf.Max(1, t.stackCount / t.def.stackLimit) : 0,
                    carryingJob ? job : null, carrier, c, out var canUse))
                {
                    __result = canUse;
                    Log.Message("good cell: " + c + " - " + __result + " for " + t);
                    if (__result)
                    {
                        Helpers.thingsByCell[t] = c;
                        Log.Message("Registering " + t + " for " + c);
                    }
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(ReservationManager), "Reserve")]
    public static class ReservationManager_Reserve_Patch
    {
        private static bool Prefix(ref bool __result, Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
        {
            Log.ResetMessageCount();
            if (target.Thing is null)
            {
                if (StackReservationFixMod.deepStorageLoaded)
                {
                    Log.Message("--------------------------");
                    if (job.IsHaulingJob() && DeepStorageHelper.HasDeepStorageAndCanUse(0, job, claimant, target.Cell, out var canUse))
                    {
                        __result = canUse;
                        Log.Message($"Preventing reservation on {target} for pawn {claimant} - {job.targetA.Thing} - __result: {__result}");
                        return false;
                    }
                    else
                    {
                        Log.Message("Failed: " + claimant + " - " + job + " - " + target + " - " + job.IsHaulingJob());
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                    }
                    Log.Message("--------------------------");
                }
            }
            return true;
        }
        private static void Postfix(ref bool __result, Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
        {
            //Log.Message($"__result: {__result}, claimant: {claimant}, job: {job}, job.count: {job.count}, target: {target}, maxPawns: {maxPawns}, stackCount: {stackCount}, layer: {layer}");
        }
    }
}
