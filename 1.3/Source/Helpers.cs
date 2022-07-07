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
    }
}
