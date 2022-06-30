using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    [HarmonyPatch(typeof(ReservationManager), "Reserve")]
    public class ReservationManager_Reserve_Patch
    {
        private static bool Prefix(ref bool __result, Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
        {
            Log.ResetMessageCount();
            if (target.Thing is null)
            {
                if (StackReservationFixMod.deepStorageLoaded)
                {
                    if (DeepStorageHelper.HasDeepStorageAndCanUse(job, claimant, target.Cell, out var success))
                    {
                        __result = success;
                        Log.Message($"Preventing reserving for {target} for pawn {claimant} - {job.targetA.Thing}");
                        return false;
                    }
                }
                Log.Message(claimant + " is reserving " + target + " for " + job);
                if (job.def.defName == "UnloadYourHauledInventory")
                {
                    //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                }
            }
            return true;
        }
        private static void Postfix(ref bool __result, Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
        {
            Log.Message($"__result: {__result}, claimant: {claimant}, job: {job}, job.count: {job.count}, target: {target}, maxPawns: {maxPawns}, stackCount: {stackCount}, layer: {layer}");
        }
    }
}
