using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace StackReservationFix
{
    public static class Helpers
    {
        public static Dictionary<Thing, IntVec3> thingsByCell = new Dictionary<Thing, IntVec3>();
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

        public static string JobSummary(this Job job)
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
                if (job.targetQueueA != null)
                {
                    text = text + " targetQueueA=" + string.Join(", ", job.targetQueueA);
                }
                if (job.targetQueueB != null)
                {
                    text = text + " targetQueueB=" + string.Join(", ", job.targetQueueB);
                }
                return text;
            }
            return "null";
        }
    }
}
