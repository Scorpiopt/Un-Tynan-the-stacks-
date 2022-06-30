using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    public static class DeepStorageHelper
    {
        public static bool HasDeepStorageAndCanUse(Job job, Pawn hauler, IntVec3 cell)
        {
            bool isVanillaHaulingJob = job.def == JobDefOf.HaulToCell;
            bool isPickupAndHaulJob = job.def.defName == "HaulToInventory";
            if (isVanillaHaulingJob || isPickupAndHaulJob)
            {
                var buildingStorage = cell.GetFirstThing<Building_Storage>(hauler.Map);
                if (buildingStorage != null)
                {
                    var comp = buildingStorage.GetComp<LWM.DeepStorage.CompDeepStorage>();
                    if (comp != null)
                    {
                        var pawnHaulCount = 0;
                        var otherPawns = hauler.Map.mapPawns.SpawnedPawnsInFaction(hauler.Faction).Where(x => x != hauler).Distinct();
                        if (isVanillaHaulingJob)
                        {
                            var existingCapacityToStore = comp.CapacityToStoreThingAt(job.targetA.Thing, hauler.Map, cell);
                            pawnHaulCount = Mathf.Min(hauler.CurJob.count, hauler.CurJob.targetA.Thing.stackCount);
                            pawnHaulCount += otherPawns.Where(x => x.CurJob.def == JobDefOf.HaulToCell && x.CurJob.targetB.Cell == cell)
                                            .Sum(x => Mathf.Min(x.CurJob.count, x.CurJob.targetA.Thing.stackCount));

                            Log.Message("pawnHaulCount: " + pawnHaulCount);
                            if (existingCapacityToStore - pawnHaulCount > 0)
                            {
                                return true;
                            }
                            else
                            {
                                Log.Message("Can't stack on " + buildingStorage);
                            }
                        }
                        else if (isPickupAndHaulJob)
                        {
                            pawnHaulCount = otherPawns.Where(x => x.CurJob.def.defName == "HaulToInventory" && x.CurJob.targetB.Cell == cell)
                            .Sum(x => x.CurJob.countQueue.Sum());
                            for (var i = 0; i < job.targetQueueA.Count; i++)
                            {
                                pawnHaulCount += Mathf.Min(job.countQueue[i], job.targetQueueA[i].Thing.stackCount);
                                var existingCapacityToStore = comp.CapacityToStoreThingAt(job.targetQueueA[i].Thing, hauler.Map, cell);
                                Log.Message(i + " - pawnHaulCount: " + pawnHaulCount);
                                if (existingCapacityToStore - pawnHaulCount <= 0)
                                {
                                    Log.Message(i + " - Can't stack on " + buildingStorage);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
        private static int TotalThingCountPickUpAndHaul(Thing thing, int count, Pawn hauler, IntVec3 cell, int existingCapacityToStore)
        {
            var otherPawns = hauler.Map.mapPawns.SpawnedPawnsInFaction(hauler.Faction).Where(x => x != hauler
                && x.CurJob.def == JobDefOf.HaulToCell && x.CurJob.targetB.Cell == cell).Distinct().ToList();
            var total = otherPawns.Sum(x => Mathf.Min(x.CurJob.count, x.CurJob.targetA.Thing.stackCount));
            Log.Message($"hauler: {hauler}, hauler.CurJob: {hauler.CurJob}, hauler.CurJob.count: {hauler.CurJob.count}," +
                $" thing: {thing}, otherReservations: {otherPawns.Count}, " +
                $"existingCapacityToStore: {existingCapacityToStore}, count: {count}, total: {total}");
            return total + Mathf.Min(count, hauler.CurJob.targetA.Thing.stackCount);
        }
    }
}
