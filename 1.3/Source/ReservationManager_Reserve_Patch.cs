using HarmonyLib;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    [HarmonyPatch(typeof(ReservationManager), "Reserve")]
    public class ReservationManager_Reserve_Patch
    {
        private static bool Prefix(ref bool __result, Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
        {
            Log.Message("claimant: " + claimant + " - " + job);
            if (target.Thing is null)
            {
                if (StackReservationFixMod.deepStorageLoaded)
                {
                    if (DeepStorageHelper.HasDeepStorageAndCanUse(job, claimant, target.Cell))
                    {
                        __result = true;
                        Log.Message($"Preventing reserving for {target} for pawn {claimant} - {job.targetA.Thing}");
                        return false;
                    }
                }
            }
            return true;
        }
        private static void Postfix(ref bool __result, Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
        {
            //Log.Message($"__result: {__result}, claimant: {claimant}, job: {job}, job.count: {job.count}, target: {target}, maxPawns: {maxPawns}, stackCount: {stackCount}, layer: {layer}");
        }
    }

    //[HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    //public class StartJobPatch
    //{
    //    private static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn, Job newJob, JobTag? tag)
    //    {
    //        try
    //        {
    //            if (___pawn.IsColonist)
    //            {
    //                ___pawn.jobs.debugLog = true;
    //            }
    //        }
    //        catch { }
    //    }
    //}
    //
    //
    //
    //[HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    //public class EndCurrentJobPatch
    //{
    //    private static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn, JobCondition condition, ref bool startNewJob, bool canReturnToPool = true)
    //    {
    //        try
    //        {
    //            if (___pawn.IsColonist)
    //            {
    //                Log.Message(___pawn + " is ending " + ___pawn.CurJob);
    //            }
    //        }
    //        catch { };
    //    }
    //}
    //
    //[HarmonyPatch(typeof(ThinkNode_JobGiver), "TryIssueJobPackage")]
    //public class TryIssueJobPackage
    //{
    //    private static void Postfix(ThinkNode_JobGiver __instance, ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
    //    {
    //        try
    //        {
    //            if (pawn.IsColonist && __result.Job != null)
    //            {
    //                Log.Message(pawn + " gets " + __result.Job + " from " + __instance);
    //            }
    //        }
    //        catch { }
    //    }
    //}
    //
    //[StaticConstructorOnStartup]
    //public static class Startup2
    //{
    //    static Startup2()
    //    {
    //        var postfix = AccessTools.Method(typeof(Startup2), "Postfix");
    //        foreach (var type2 in typeof(ThinkNode).AllSubclassesNonAbstract())
    //        {
    //            var methodToPatch2 = AccessTools.Method(type2, "TryIssueJobPackage");
    //            try
    //            {
    //                StackReservationFixMod.harmony.Patch(methodToPatch2, postfix: new HarmonyMethod(postfix));
    //            }
    //            catch { }
    //        }
    //        var postfix2 = AccessTools.Method(typeof(Startup2), "Postfix2");
    //        foreach (var type3 in typeof(ThinkNode_Conditional).AllSubclassesNonAbstract())
    //        {
    //            var methodToPatch3 = AccessTools.Method(type3, "Satisfied");
    //            try
    //            {
    //                StackReservationFixMod.harmony.Patch(methodToPatch3, postfix: new HarmonyMethod(postfix2));
    //            }
    //            catch { }
    //        }
    //        
    //        var postfix3 = AccessTools.Method(typeof(Startup2), "Postfix3");
    //        foreach (var type3 in typeof(WorkGiver_Scanner).AllSubclassesNonAbstract())
    //        {
    //            var methodToPatch3 = AccessTools.Method(type3, "HasJobOnThing");
    //            try
    //            {
    //                StackReservationFixMod.harmony.Patch(methodToPatch3, postfix: new HarmonyMethod(postfix3));
    //            }
    //            catch { }
    //        }
    //        
    //        var postfix4 = AccessTools.Method(typeof(Startup2), "Postfix4");
    //        foreach (var type3 in typeof(WorkGiver_Scanner).AllSubclassesNonAbstract())
    //        {
    //            var methodToPatch3 = AccessTools.Method(type3, "JobOnThing");
    //            try
    //            {
    //                StackReservationFixMod.harmony.Patch(methodToPatch3, postfix: new HarmonyMethod(postfix4));
    //            }
    //            catch { }
    //        }
    //    }
    //
    //    private static void Postfix(ThinkNode __instance, ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
    //    {
    //        if (pawn.IsColonist)
    //        {
    //            Log.Message(pawn + " gets " + __result.Job + " from " + __instance);
    //            if (__instance is ThinkNode_Subtree subtree)
    //            {
    //                Log.Message("Subtree: " + subtree.treeDef);
    //            }
    //        }
    //    }
    //    
    //    private static void Postfix2(ThinkNode_Conditional __instance, bool __result, Pawn pawn)
    //    {
    //        if (pawn.IsColonist)
    //        {
    //            Log.Message(pawn + " gets " + __result + " from " + __instance);
    //        }
    //    }
    //    
    //    private static void Postfix3(WorkGiver_Scanner __instance, bool __result, Pawn pawn)
    //    {
    //        if (pawn.IsColonist)
    //        {
    //            Log.Message(pawn + " gets " + __result + " from " + __instance);
    //        }
    //    }
    //    
    //    private static void Postfix4(WorkGiver_Scanner __instance, Job __result, Pawn pawn)
    //    {
    //        if (pawn.IsColonist)
    //        {
    //            Log.Message(pawn + " gets " + __result + " from " + __instance);
    //        }
    //    }
    //}
}
