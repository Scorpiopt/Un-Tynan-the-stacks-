using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
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
            if (__result > 0)
            {
                Helpers.thingsByCell[thing] = cell;
            }
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
            Log.Message("HasDeepStorageAndCanUse ---------------------------------");
            if (thingToBeHauled != null)
            {
                Log.Message("thingToBeHauled: " + thingToBeHauled + " - " + thingToBeHauled.PositionHeld + " - " + thingToBeHauled.ParentHolder);
            }
            if (job != null)
            {
                var firstHauledThing = FirstHauledThingFromJob(job);
                if (firstHauledThing != null)
                {
                    Log.Message("firstHauledThing: " + firstHauledThing + " - " + firstHauledThing.PositionHeld + " - " + firstHauledThing.ParentHolder);
                }
            }
            hauler.jobs.debugLog = hauler.ShouldLog();
            canUse = true;
            var buildingStorage = cell.GetFirstThing<Building_Storage>(hauler.Map);
            if (buildingStorage != null)
            {
                var comp = buildingStorage.GetComp<LWM.DeepStorage.CompDeepStorage>();
                if (comp != null)
                {
                    var allStacks = 0;
                    Dictionary<ThingDef, Dictionary<Thing, int>> result = new Dictionary<ThingDef, Dictionary<Thing, int>>();
                    if (thingToBeHauled != null && job?.count > 0)
                    {
                        //Helpers.AddThingHaul(hauler, cell, thingToBeHauled, thingToBeHauled.stackCount);
                        Log.Message("1: " + hauler + " - added stack for thingToBeHauled: " + thingToBeHauled + " - job.count: " + job?.count);
                        AddStack(result, thingToBeHauled, job.count);
                    }
                    else if (thingToBeHauled != null)
                    {
                        Log.Message("Ignoring thingToBeHauled: " + thingToBeHauled + " - " + thingToBeHauled.PositionHeld + " - " + thingToBeHauled.ParentHolder);
                        hauler.Map.debugDrawer.FlashCell(thingToBeHauled.PositionHeld);
                    }
                    foreach (var thing in hauler.Map.thingGrid.ThingsListAt(cell).Where(x => x.def.EverStorable(willMinifyIfPossible: false)))
                    {
                        AddStack(result, thing, thing.stackCount);
                        Log.Message("2 Adding stack from " + thing + " - " + thing.Position);
                    }
                    foreach (var kvp in Helpers.haulers)
                    {
                        foreach (var thingsToHaul in kvp.Value.thingsToHaul)
                        {
                            if (thingsToHaul.Value.destination == cell)
                            {
                                if (thingsToHaul.Key != thingToBeHauled)
                                {
                                    AddStack(result, thingsToHaul.Key, thingsToHaul.Value.count);
                                    Log.Message("3 Adding stack from " + thingsToHaul.Key + " - " + thingsToHaul.Value.destination);
                                }
                            }
                        }
                    }
                    foreach (var r in result)
                    {
                        var count = 0;
                        foreach (var def in r.Value)
                        {
                            count += def.Value;
                            Log.Message(def.Key + " - PositionHeld: " + def.Key.PositionHeld + " - ParentHolder: " + def.Key.ParentHolder + " - def.Value: " + def.Value + " - count: " + count + " - stackCount: " + def.Key.stackCount);
                        }
                        var maxCount = comp.maxNumberStacks * r.Key.stackLimit;
                        Log.Message("maxCount: " + maxCount + " - count: " + count);
                        if (maxCount - count <= 0)
                        {
                            Log.Message("Cannot use: count is eq maxCount");
                            canUse = false;
                        }
                        allStacks += Mathf.CeilToInt(count / (float)r.Key.stackLimit);
                    }

                    if (allStacks > comp.maxNumberStacks)
                    {
                        Log.Message("Cannot use: allStacks > comp.maxNumberStacks");
                        canUse = false;
                    }

                    //if (firstHauledThing != null)
                    //{
                    //    canUse = comp.CapacityToStoreThingAt(firstHauledThing, hauler.Map, cell) > 0;
                    //}
                    Log.Message(hauler + " - All stacks: " + allStacks + " - for cell " + cell + " - " + canUse + " - job: " + job.JobSummary(hauler)
                        + " - " + new StackTrace());
                    return true;
                }
            }
            return false;
        }

        private static void AddStack(Dictionary<ThingDef, Dictionary<Thing, int>> dict, Thing thing, int count)
        {
            if (!dict.TryGetValue(thing.def, out var stackDict))
            {
                dict[thing.def] = stackDict = new Dictionary<Thing, int>();
            }
            if (stackDict.ContainsKey(thing))
            {
                stackDict[thing] += count;
            }
            else
            {
                stackDict[thing] = count;
            }
            Log.Message(thing + " - " + count);
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

        private static void GetNumOfStackFromPawn(IntVec3 targetCell, Pawn pawn)
        {
            GetNumOfStackFromJob(pawn, pawn.CurJob, targetCell);
            foreach (var queuedJob in pawn.jobs.jobQueue)
            {
                if (queuedJob.job != null)
                {
                    GetNumOfStackFromJob(pawn, queuedJob.job, targetCell);
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
                        //Helpers.AddThingHaul(pawn, targetCell, thing, thing.stackCount);
                        Log.Message("3: " + pawn + " has thing in inventory going to : " + storageCell);
                    }
                    else
                    {
                        Log.Message("3.5: " + pawn + " has thing in inventory going to : " + storageCell);
                    }
                }
            }
        }

        private static void GetNumOfStackFromJob(Pawn pawn, Job job, IntVec3 targetCell)
        {
            if (job != null && job.targetB.Cell == targetCell)
            {
                if (job.def.defName == "HaulToInventory")
                {
                    GetNumOfStacksTargetA(pawn, job, targetCell);
                    GetNumOfStacksTargetQueueA(pawn, job, targetCell);
                    Log.Message("Checking pawn: " + pawn + " - " + pawn.CurJob.JobSummary(pawn));
                }
                else if ((job.def == JobDefOf.HaulToCell || job.def.defName == "UnloadYourHauledInventory"))
                {
                    GetNumOfStacksTargetA(pawn, job, targetCell);
                    Log.Message("Checking pawn: " + pawn + " - " + pawn.CurJob.JobSummary(pawn));
                }
            }
        }

        private static void GetNumOfStacksTargetQueueA(Pawn pawn, Job job, IntVec3 targetCell)
        {
            if (job.targetQueueA != null)
            {
                foreach (var item in job.targetQueueA)
                {
                    var storageCell = Helpers.GetStorageCell(item.Thing, job.targetB.Cell);
                    if (targetCell == storageCell)
                    {
                        var index = job.targetQueueA.IndexOf(item);
                        //Helpers.AddThingHaul(pawn, targetCell, item.Thing, job.countQueue[index]);
                        Log.Message("4: " + pawn + " has thing from job to : " + storageCell);
                    }
                }
            }
        }

        private static void GetNumOfStacksTargetA(Pawn pawn, Job job, IntVec3 targetCell)
        {
            if (job.targetA.Thing != null)
            {
                var storageCell = Helpers.GetStorageCell(job.targetA.Thing, job.targetB.Cell);
                if (targetCell == storageCell)
                {
                    var haulCount = Mathf.Min(job.count, job.targetA.Thing.stackCount);
                    //Helpers.AddThingHaul(pawn, targetCell, job.targetA.Thing, haulCount);
                    Log.Message("5: " + pawn + " has thing from job to : " + storageCell);
                }
            }
        }
    }
}
