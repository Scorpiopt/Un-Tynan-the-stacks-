using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
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


    public static class Log
    {
        private static bool debug = true;
        public static void Message(string message)
        {
            Verse.Log.ResetMessageCount();
            if (debug)
                Verse.Log.Message(message);
        }

        public static void Error(string error)
        {
            Verse.Log.ResetMessageCount();
            if (debug)
                Verse.Log.Error(error);
        }
    }

    [HarmonyPatch(typeof(ReservationManager), "Reserve")]
    public static class ReservationManager_Reserve_Patch
    {
        private static bool Prefix(ref bool __result, Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
        {
            if (target.Thing is null)
            {
                if (StackReservationFixMod.deepStorageLoaded)
                {
                    Log.Message("Reserve -------------------------- " + claimant + " job: " + job.JobSummary(claimant) + " - " + new StackTrace());
                    if (job.IsHaulingJob() && DeepStorageHelper.HasDeepStorageAndCanUse(null, job, claimant, target.Cell, out var canUse))
                    {
                        __result = canUse;
                        Log.Message($"Preventing reservation on {target} for pawn {claimant} - {job.targetA.Thing} - __result: {__result}");
                        return false;
                    }
                    else
                    {
                        Log.Error("Failed: " + claimant + " - " + job.JobSummary(claimant) + " - " + target + " - " + job.IsHaulingJob());
                        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
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
