using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    public class HaulState
    {
        public IntVec3 destination;
        public int count;
    }
    public class ThingsToHaul
    {
        public Dictionary<Thing, HaulState> thingsToHaul = new Dictionary<Thing, HaulState>();
        public void AddThingHaul(Thing thing, int count, IntVec3 destination)
        {
            if (!thingsToHaul.TryGetValue(thing, out var value))
            {
                thingsToHaul[thing] = value = new HaulState();
            }
            value.destination = destination;
            value.count = count;
            Log.Message("Adding thing to haul: " + thing + " - " + destination);
        }
    }

    [HarmonyPatch(typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow")]
    public class SavedGameLoaderNow_LoadGameFromSaveFileNow
    {
        public static void Prefix()
        {
            Helpers.Reset();
        }
    }
    public static class Helpers
    {
        public static Dictionary<Thing, IntVec3> thingsByCell = new Dictionary<Thing, IntVec3>();

        public static Dictionary<Pawn, ThingsToHaul> haulers = new Dictionary<Pawn, ThingsToHaul>();
        public static void Reset()
        {
            thingsByCell.Clear();
            haulers.Clear();
        }
        public static void AddThingHaul(Pawn hauler, IntVec3 destination, Thing thing, int count)
        {
            Log.Message("AddThingHaul: " + hauler + " - destination: " + destination + " - thing: " + thing + " - count: " + count);
            if (!haulers.TryGetValue(hauler, out var state))
            {
                haulers[hauler] = state = new ThingsToHaul();
            }
            state.AddThingHaul(thing, count, destination);
        }
        public static bool IsHaulingJob(this Job job)
        {
            return job != null && (job.def == JobDefOf.HaulToCell || job.def.defName == "HaulToInventory" || job.def.defName == "UnloadYourHauledInventory");
        }

        public static IntVec3 GetStorageCell(this Thing thing, IntVec3 fallback)
        {
            if (thingsByCell.TryGetValue(thing, out var cell))
            {
                return cell;
            }
            return fallback;
        }

        public static string JobSummary(this Job job, Pawn pawn)
        {
            if (job != null)
            {
                string text = job.def.ToString() + " (" + job.GetUniqueLoadID() + ")";
                if (job.targetA.IsValid)
                {
                    text = text + " A=" + job.targetA.ToString();
                }
                if (job.targetB.IsValid)
                {
                    text = text + " B=" + job.targetB.ToString();
                }
                if (job.targetC.IsValid)
                {
                    text = text + " C=" + job.targetC.ToString();
                }
                if (job.count > 0)
                {
                    text = text + " count =" + job.count.ToString();
                }
                if (job.targetQueueA != null)
                {
                    text = text + " targetQueueA=" + string.Join(", ", job.targetQueueA);
                }
                if (job.targetQueueB != null)
                {
                    text = text + " targetQueueB=" + string.Join(", ", job.targetQueueB);
                }
                if (job.countQueue != null)
                {
                    text = text + " countQueue=" + string.Join(", ", job.countQueue);
                }
                if (pawn?.jobs?.jobQueue?.jobs != null)
                {
                    foreach (var job2 in pawn.jobs.jobQueue.jobs)
                    {
                        text += "\njob queue: " + JobSummary(job, null);
                    }
                }
                return text;
            }
            return "null";
        }
    }
}
