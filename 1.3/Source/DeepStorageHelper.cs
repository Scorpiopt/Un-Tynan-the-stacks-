using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    [HarmonyPatch(typeof(GenSpawn), "Spawn", new System.Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) })]
    public static class GenSpawn_Spawn_Patch
    {
        public static void Postfix(Thing __result, bool respawningAfterLoad)
        {
            if (__result.def.IsMedicine)
            {
                if (__result.Position.GetFirstThing<Building_Storage>(__result.Map) is null)
                {
                    Log.Message("Spawning medifice in " + __result.Position);
                    //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                }
            }
        }
    }
    public static class DeepStorageHelper
    {

        public static bool HasDeepStorageAndCanUse(Job job, Pawn hauler, IntVec3 cell, out bool success)
        {
            success = true;
            bool isVanillaHaulingJob = job.def == JobDefOf.HaulToCell;
            bool isPickupAndHaulJob = job.def.defName == "HaulToInventory";
            bool isUnloadInventoryJob = job.def.defName == "UnloadYourHauledInventory";
            if (isVanillaHaulingJob || isPickupAndHaulJob || isUnloadInventoryJob)
            {
                var buildingStorage = cell.GetFirstThing<Building_Storage>(hauler.Map);
                if (buildingStorage != null)
                {
                    var comp = buildingStorage.GetComp<LWM.DeepStorage.CompDeepStorage>();
                    if (comp != null)
                    {
                        if (isUnloadInventoryJob && buildingStorage.AllSlotCells().Contains(cell))
                        {
                            Log.Message("Allowing to unload into " + buildingStorage);
                            return true;
                        }
                        var pawnHaulCount = 0;
                        var otherPawns = hauler.Map.mapPawns.SpawnedPawnsInFaction(hauler.Faction).Where(x => x != hauler).Distinct();
                        if (isVanillaHaulingJob)
                        {
                            var existingCapacityToStore = comp.CapacityToStoreThingAt(job.targetA.Thing, hauler.Map, cell);
                            pawnHaulCount = Mathf.Min(job.count, job.targetA.Thing.stackCount);
                            pawnHaulCount += otherPawns.Where(x => x.CurJob.def == JobDefOf.HaulToCell && x.CurJob.targetB.Cell == cell)
                                            .Sum(x => Mathf.Min(x.CurJob.count, x.CurJob.targetA.Thing.stackCount));

                            Log.Message("pawnHaulCount: " + pawnHaulCount);
                            if (existingCapacityToStore - pawnHaulCount > 0)
                            {
                                Log.Message("1 Can stack on " + buildingStorage);
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
                            var numOfStacks = hauler.Map.thingGrid.ThingsListAt(cell).Count;
                            for (var i = 0; i < job.targetQueueA.Count; i++)
                            {
                                var stackToAdd = Mathf.Min(job.countQueue[i], job.targetQueueA[i].Thing.stackCount);
                                pawnHaulCount += stackToAdd;
                                var existingCapacityToStore = comp.CapacityToStoreThingAt(job.targetQueueA[i].Thing, hauler.Map, cell);
                                Log.Message(i + " - pawnHaulCount: " + pawnHaulCount);
                                if (existingCapacityToStore - pawnHaulCount <= 0)
                                {
                                    Log.Message(i + " - Can't stack on " + buildingStorage);
                                    return false;
                                }
                            }

                            numOfStacks += GetNumOfStacks(job);
                            foreach (var otherPawn in otherPawns)
                            {
                                if (otherPawn.CurJob.def.defName == "HaulToInventory" && otherPawn.CurJob.targetB.Cell == cell)
                                {
                                    numOfStacks += GetNumOfStacks(otherPawn.CurJob);
                                }
                            }

                            Log.Message(hauler + " - " + cell + " - numOfStacks: " + numOfStacks + " - comp.maxNumberStacks: " + comp.maxNumberStacks 
                                + " - " + comp.parent.def);
                            if (numOfStacks > comp.maxNumberStacks)
                            {
                                Log.Message(numOfStacks + " numOfStacks - Can't stack on " + buildingStorage);
                                //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                                success = false;
                                return false;
                            }
                            Log.Message("2 Can stack on " + buildingStorage);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static int GetNumOfStacks(Job job)
        {
            var numOfStacks = 0;
            var groups = job.targetQueueA.GroupBy(x => x.Thing.def);
            foreach (var group in groups)
            {
                var stackCount = 0;
                foreach (var item in group)
                {
                    var index = job.targetQueueA.IndexOf(item);
                    stackCount += job.countQueue[index];
                }
                var stackInGroup = stackCount / group.First().Thing.def.stackLimit;
                numOfStacks += stackInGroup;
                Log.Message("stackInGroup: " + numOfStacks);
            }

            return numOfStacks;
        }
    }
}
