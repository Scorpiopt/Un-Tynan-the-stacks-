using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    public static class DeepStorageHelper
    {
        public static bool HasDeepStorageAndCanUse(int allStacks, Job job, Pawn hauler, IntVec3 cell, out bool canUse)
        {
            canUse = true;
            var buildingStorage = cell.GetFirstThing<Building_Storage>(hauler.Map);
            if (buildingStorage != null)
            {
                var comp = buildingStorage.GetComp<LWM.DeepStorage.CompDeepStorage>();
                if (comp != null)
                {
                    Log.Message("initial: " + allStacks);
                    allStacks += GetAllStacksGoingToStorage(hauler, cell, job);
                    if (allStacks > comp.maxNumberStacks + 1)
                    {
                        canUse = false;
                    }
                    Log.Message(hauler + " - All stacks: " + allStacks + " - for cell " + cell + " - " + canUse);
                    return true;
                }
            }
            return false;
        }

        public static int GetAllStacksGoingToStorage(Pawn hauler, IntVec3 cell, Job job)
        {
            var otherPawns = hauler.Map.mapPawns.SpawnedPawnsInFaction(hauler.Faction).Where(x => x != hauler).Distinct().ToList();
            var numOfStacks = hauler.Map.thingGrid.ThingsListAt(cell).Where(x => x.def.EverStorable(willMinifyIfPossible: false)).Count();
            Log.Message("already stored: " + numOfStacks);
            if (job != null)
            {
                numOfStacks += GetNumOfStackFromPawn(cell, hauler);
            }
            foreach (var otherPawn in otherPawns)
            {
                numOfStacks += GetNumOfStackFromPawn(cell, otherPawn);
            }
            return numOfStacks;
        }

        private static int GetNumOfStackFromPawn(IntVec3 targetCell, Pawn pawn)
        {
            var numOfStacks = GetNumOfStackFromJob(pawn, pawn.CurJob, targetCell);
            foreach (var queuedJob in pawn.jobs.jobQueue)
            {
                if (queuedJob.job != null)
                {
                    numOfStacks += GetNumOfStackFromJob(pawn, queuedJob.job, targetCell);
                }
            }

            //var comp = pawn.GetComp<PickUpAndHaul.CompHauledToInventory>();
            //if (comp != null)
            //{
            //    foreach (var thing in comp.GetHashSet())
            //    {
            //        var storageCell = GetStorageCell(thing, targetCell);
            //        if (storageCell == targetCell)
            //        {
            //            Log.Message(pawn + " - stored in inventory: " + thing + " to " + storageCell);
            //            numOfStacks += 1;
            //        }
            //    }
            //}
            return numOfStacks;
        }

        private static int GetNumOfStackFromJob(Pawn pawn, Job job, IntVec3 targetCell)
        {
            var numOfStacks = 0;
            if (job != null && job.targetB.Cell == targetCell)
            {
                if (job.def.defName == "HaulToInventory")
                {
                    numOfStacks += GetNumOfStacksTargetQueueA(job, targetCell);
                    if (GetNumOfStacksTargetQueueA(job, targetCell) != 0)
                    {
                        Log.Message(pawn + " - pawn job: " + GetNumOfStacksTargetQueueA(job, targetCell) + " - " + job);
                    }
                }
                else if ((job.def == JobDefOf.HaulToCell || job.def.defName == "UnloadYourHauledInventory"))
                {
                    numOfStacks += GetNumOfStacksTargetA(job, targetCell);
                    if (GetNumOfStacksTargetA(job, targetCell) != 0)
                    {
                        Log.Message(pawn + " - pawn job: " + GetNumOfStacksTargetA(job, targetCell) + " - " + job);
                    }
                }
            }
            return numOfStacks;
        }

        private static int GetNumOfStacksTargetQueueA(Job job, IntVec3 targetCell)
        {
            var numOfStacks = 0;
            var groups = job.targetQueueA.GroupBy(x => x.Thing.def);
            foreach (var group in groups)
            {
                var stackCount = 0;
                foreach (var item in group)
                {
                    var storageCell = GetStorageCell(item.Thing, job.targetB.Cell);
                    if (targetCell == storageCell)
                    {
                        var index = job.targetQueueA.IndexOf(item);
                        stackCount += job.countQueue[index];
                    }
                }
                var stackInGroup = stackCount / group.First().Thing.def.stackLimit;
                numOfStacks += stackInGroup;
            }
            return numOfStacks;
        }

        public static IntVec3 GetStorageCell(this Thing thing, IntVec3 fallback)
        {
            if (Helpers.thingsByCell.TryGetValue(thing, out var cell))
            {
                return cell;
            }
            return fallback;

        }
        private static int GetNumOfStacksTargetA(Job job, IntVec3 targetCell)
        {
            var storageCell = GetStorageCell(job.targetA.Thing, job.targetB.Cell);
            if (targetCell == storageCell)
            {
                var pawnHaulCount = Mathf.Min(job.count, job.targetA.Thing.stackCount);
                return Mathf.Max(1, pawnHaulCount / job.targetA.Thing.def.stackLimit);
            }
            return 0;
        }
    }
}
