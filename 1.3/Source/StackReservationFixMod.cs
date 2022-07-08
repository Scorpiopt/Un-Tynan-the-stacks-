using HarmonyLib;
using RimWorld;
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
        private static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn, JobCondition condition, ref bool startNewJob, bool canReturnToPool = true)
        {
            try
            {
                if (___pawn.CurJob.haulMode == HaulMode.ToCellStorage)
                {
                    Thing thing = ___pawn.CurJob.GetTarget(TargetIndex.A).Thing;
                    if (!___pawn.jobs.curJob.GetTarget(TargetIndex.B).Cell.IsValidStorageFor(___pawn.Map, thing))
                    {
                        Log.Message("Is not valid storage anymore: " + ___pawn.jobs.curJob.GetTarget(TargetIndex.B).Cell + " - " + thing);
                    }
                }
            }
            catch { };
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
        public static bool ShouldLog(this Pawn pawn) => pawn.IsColonist && Find.Selector.IsSelected(pawn);
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
