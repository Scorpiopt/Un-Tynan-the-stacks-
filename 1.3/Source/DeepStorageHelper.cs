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
    [StaticConstructorOnStartup]
    public static class DeepStorageHelper
    {
        static DeepStorageHelper()
        {
            StackReservationFixMod.harmony.Patch(AccessTools.Method(typeof(LWM.DeepStorage.CompDeepStorage), "CapacityToStoreThingAt"),
                postfix: new HarmonyMethod(typeof(DeepStorageHelper), "CapacityToStoreThingAtPostfix"));
        }
        public static void CapacityToStoreThingAtPostfix(LWM.DeepStorage.CompDeepStorage __instance, ref int __result, Thing thing, Map map, IntVec3 cell)
        {
            Log.ResetMessageCount();
            var curPawn = JobGiver_Work_TryIssueJobPackage_Patch.curPawn;
            if (curPawn != null)
            {
                Log.Message("2 cur pawn: " + curPawn + " ------------------------");
                Log.Message("Things by cell: ");
                foreach (var kvp in Helpers.thingsByCell)
                {
                    Log.Message(kvp.Key + " - " + kvp.Value);
                }
                var additionalThings = new List<Thing>();
                Dictionary<ThingDef, int> result = new Dictionary<ThingDef, int>();
                var otherPawns = map.mapPawns.SpawnedPawnsInFaction(curPawn.Faction).Where(x => x != curPawn).Distinct().ToList();
                Log.Message("otherPawns: " + otherPawns.Count);
                foreach (var otherPawn in otherPawns)
                {
                    Log.Message("Checking other pawn: " + otherPawn + " - for " + cell + " - job: " + otherPawn.CurJob.JobSummary());
                    GetNumOfStackFromPawn(result, cell, otherPawn);
                }
                foreach (var r in result)
                {
                    Log.Message("Cell: " + cell + ", subtracting " + r.Value + " from " + __result + " result: " + (__result - r.Value));
                    __result -= r.Value;
                }
                if (__result > 0)
                {
                    Log.Message("Registering thing " + thing + " at cell " + cell);
                    Helpers.thingsByCell[thing] = cell;
                }
                Log.Message("CapacityToStoreThingAtPostfix: __instance: " + __instance + " - thing: " + thing + " - cell: " + cell + " - __result: " + __result);
            }
            else
            {
                Log.Message("Missing cur pawn");
            }
            Log.Message("------------------------");
        }

        public static int CapacityToStoreThingAt(LWM.DeepStorage.CompDeepStorage __instance, Thing thing, List<Thing> additionalThings,
            Map map, IntVec3 cell)
        {
            int capacity = 0;
            if (__instance.limitingFactorForItem > 0f && thing.GetStatValue(__instance.stat) > __instance.limitingFactorForItem)
            {
                return 0;
            }
            float totalWeightStoredHere = 0f;
            List<Thing> list = map.thingGrid.ThingsListAt(cell);
            int stacksStoredHere = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing2 = list[i];
                if (!thing2.def.EverStorable(willMinifyIfPossible: false))
                {
                    continue;
                }
                stacksStoredHere++;
                if (__instance.limitingTotalFactorForCell > 0f)
                {
                    totalWeightStoredHere += thing2.GetStatValue(__instance.stat) * (float)thing2.stackCount;
                    if (totalWeightStoredHere > __instance.limitingTotalFactorForCell && stacksStoredHere >= __instance.minNumberStacks)
                    {
                        return 0;
                    }
                }
                if (thing2 == thing)
                {
                    if (stacksStoredHere > __instance.maxNumberStacks)
                    {
                        return 0;
                    }
                    return thing.stackCount;
                }
                if (thing2.CanStackWith(thing) && thing2.stackCount < thing2.def.stackLimit)
                {
                    capacity += thing2.def.stackLimit - thing2.stackCount;
                }
            }
            if (__instance.limitingTotalFactorForCell > 0f)
            {
                if (stacksStoredHere <= __instance.minNumberStacks)
                {
                    capacity += (__instance.minNumberStacks - stacksStoredHere) * thing.def.stackLimit;
                    totalWeightStoredHere += (float)(__instance.minNumberStacks - stacksStoredHere) * thing.GetStatValue(__instance.stat) * (float)thing.def.stackLimit;
                    stacksStoredHere = __instance.minNumberStacks;
                }
                totalWeightStoredHere = __instance.limitingTotalFactorForCell - totalWeightStoredHere;
                if (totalWeightStoredHere <= 0f)
                {
                    if (stacksStoredHere > __instance.minNumberStacks)
                    {
                        return 0;
                    }
                    return capacity;
                }
                if (stacksStoredHere < __instance.maxNumberStacks)
                {
                    capacity += Math.Min((__instance.maxNumberStacks - stacksStoredHere) * thing.def.stackLimit, (int)(totalWeightStoredHere / thing.GetStatValue(__instance.stat)));
                }
                return capacity;
            }
            if (__instance.maxNumberStacks > stacksStoredHere)
            {
                capacity += (__instance.maxNumberStacks - stacksStoredHere) * thing.def.stackLimit;
            }
            return capacity;
        }
        public static bool HasDeepStorageAndCanUse(Thing thingToBeHauled, Job job, Pawn hauler, IntVec3 cell, out bool canUse)
        {
            canUse = true;
            var buildingStorage = cell.GetFirstThing<Building_Storage>(hauler.Map);
            if (buildingStorage != null)
            {
                var comp = buildingStorage.GetComp<LWM.DeepStorage.CompDeepStorage>();
                if (comp != null)
                {
                    Dictionary<ThingDef, int> result = new Dictionary<ThingDef, int>();
                    if (thingToBeHauled != null)
                    {
                        result.AddStack(hauler, cell, thingToBeHauled, thingToBeHauled.stackCount);
                        Log.Message("1: " + hauler + " - added stack for thingToBeHauled: " + thingToBeHauled);
                    }
                    GetAllStacksGoingToStorage(result, hauler, cell, job);
                    var allStacks = 0;
                    foreach (var r in result)
                    {
                        allStacks += r.Value / r.Key.stackLimit;
                    }
                    if (allStacks > comp.maxNumberStacks)
                    {
                        canUse = false;
                    }
                    var firstHauledThing = FirstHauledThingFromJob(job);
                    //if (firstHauledThing != null)
                    //{
                    //    canUse = comp.CapacityToStoreThingAt(firstHauledThing, hauler.Map, cell) > 0;
                    //}
                    Log.Message(hauler + " - All stacks: " + allStacks + " - for cell " + cell + " - " + canUse + " - job: " + job.JobSummary() + " - firstHauledThing: " + firstHauledThing);
                    return true;
                }
            }
            return false;
        }

        public static Thing FirstHauledThingFromJob(Job job)
        {
            if (job != null)
            {
                if (job.def.defName == "HaulToInventory")
                {
                    return job.targetA.Thing;
                }
                else if (job.def.defName == "UnloadYourHauledInventory")
                {
                    if (!(job.targetA.Thing is Pawn))
                    {
                        return job.targetA.Thing;
                    }
                }
                else if (job.def == JobDefOf.HaulToCell)
                {
                    return job.targetA.Thing;
                }
            }
            return null;
        }

        public static void AddStack(this Dictionary<ThingDef, int> result, Pawn hauler, IntVec3 cell, Thing thing, int count)
        {
            if (result.ContainsKey(thing.def))
            {
                result[thing.def] += count;
            }
            else
            {
                result[thing.def] = count;
            }
            Helpers.thingsByCell[thing] = cell;
            Log.Message(hauler + " - Adding stack for " + thing + " to cell " + cell + " count " + count + " total count " + result[thing.def] / thing.def.stackLimit);
        }

        public static void GetAllStacksGoingToStorage(Dictionary<ThingDef, int> result, Pawn hauler, IntVec3 cell, Job job)
        {
            var otherPawns = hauler.Map.mapPawns.SpawnedPawnsInFaction(hauler.Faction).Where(x => x != hauler).Distinct().ToList();
            foreach (var thing in hauler.Map.thingGrid.ThingsListAt(cell).Where(x => x.def.EverStorable(willMinifyIfPossible: false)))
            {
                result.AddStack(null, cell, thing, thing.stackCount);
                Log.Message("2: already existing things: " + cell);
            }
            Log.Message("Checking job: " + job.JobSummary());
            if (job != null)
            {
                GetNumOfStackFromPawn(result, cell, hauler);
            }
            foreach (var otherPawn in otherPawns)
            {
                Log.Message("Checking pawn: " + otherPawn + " - " + otherPawn.CurJob.JobSummary());
                GetNumOfStackFromPawn(result, cell, otherPawn);
            }
        }

        private static void GetNumOfStackFromPawn(Dictionary<ThingDef, int> result, IntVec3 targetCell, Pawn pawn)
        {
            GetNumOfStackFromJob(result, pawn, pawn.CurJob, targetCell);
            foreach (var queuedJob in pawn.jobs.jobQueue)
            {
                if (queuedJob.job != null)
                {
                    GetNumOfStackFromJob(result, pawn, queuedJob.job, targetCell);
                }
            }

            var comp = pawn.GetComp<PickUpAndHaul.CompHauledToInventory>();
            if (comp != null)
            {
                foreach (var thing in comp.GetHashSet())
                {
                    var storageCell = Helpers.GetStorageCell(thing, IntVec3.Invalid);
                    if (storageCell == targetCell)
                    {
                        result.AddStack(pawn, targetCell, thing, thing.stackCount);
                        Log.Message("3: " + pawn + " has thing in inventory going to : " + storageCell);
                    }
                    else
                    {
                        Log.Message("3.5: " + pawn + " has thing in inventory going to : " + storageCell);
                    }
                }
            }
        }

        private static void GetNumOfStackFromJob(Dictionary<ThingDef, int> result, Pawn pawn, Job job, IntVec3 targetCell)
        {
            if (job != null && job.targetB.Cell == targetCell)
            {
                if (job.def.defName == "HaulToInventory")
                {
                    GetNumOfStacksTargetA(result, pawn, job, targetCell);
                    GetNumOfStacksTargetQueueA(result, pawn, job, targetCell);
                }
                else if ((job.def == JobDefOf.HaulToCell || job.def.defName == "UnloadYourHauledInventory"))
                {
                    GetNumOfStacksTargetA(result, pawn, job, targetCell);
                }
            }
        }

        private static void GetNumOfStacksTargetQueueA(Dictionary<ThingDef, int> result, Pawn pawn, Job job, IntVec3 targetCell)
        {
            if (job.targetQueueA != null)
            {
                foreach (var item in job.targetQueueA)
                {
                    var storageCell = Helpers.GetStorageCell(item.Thing, job.targetB.Cell);
                    if (targetCell == storageCell)
                    {
                        var index = job.targetQueueA.IndexOf(item);
                        result.AddStack(pawn, targetCell, item.Thing, job.countQueue[index]);
                        Log.Message("4: " + pawn + " has thing from job to : " + storageCell);
                    }
                }
            }
        }

        private static void GetNumOfStacksTargetA(Dictionary<ThingDef, int> result, Pawn pawn, Job job, IntVec3 targetCell)
        {
            if (job.targetA.Thing != null)
            {
                var storageCell = Helpers.GetStorageCell(job.targetA.Thing, job.targetB.Cell);
                if (targetCell == storageCell)
                {
                    var haulCount = Mathf.Min(job.count, job.targetA.Thing.stackCount);
                    result.AddStack(pawn, targetCell, job.targetA.Thing, haulCount);
                    Log.Message("5: " + pawn + " has thing from job to : " + storageCell);
                }
            }
        }
    }
}
