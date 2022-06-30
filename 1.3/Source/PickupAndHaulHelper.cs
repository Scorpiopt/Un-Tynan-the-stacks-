using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using static Verse.AI.ReservationManager;

namespace StackReservationFix
{
    public static class PickupAndHaulHelper
    {
        public static void PickupTest(WorkGiver_Scanner __instance, Pawn pawn, Thing thing, bool forced)
        {
            if (__instance is PickUpAndHaul.WorkGiver_HaulToInventory haul)
            {
                if (thing.def.IsMedicine)
                {
                    Log.Message(thing + " - " + PickUpAndHaul.WorkGiver_HaulToInventory.OkThingToHaul(thing, pawn) + " - " 
                        + PickUpAndHaul.WorkGiver_HaulToInventory.IsNotCorpseOrAllowed(thing) + " - " 
                        + HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, thing, forced) + " - " 
                        + TryFindBestBetterStorageFor(thing, pawn, pawn.Map, StoreUtility.CurrentStoragePriorityOf(thing), pawn.Faction, out _, out _, false));
                    Log.ResetMessageCount();
                    foreach (var storage in pawn.Map.listerThings.AllThings.OfType<Building_Storage>())
                    {
                        foreach (var cell in storage.AllSlotCells())
                        {
							for (int i = 0; i < pawn.Map.reservationManager.reservations.Count; i++)
							{
								Reservation reservation = pawn.Map.reservationManager.reservations[i];
								if (reservation.Target == cell && reservation.Claimant.Faction == pawn.Faction)
								{
									Log.Message(cell + " is reserved: " + pawn.Map.reservationManager.IsReservedByAnyoneOf(cell, pawn.Faction) + " - " + reservation.job);
								}
							}
                        }
                    }
                }
            }
        }

		public static bool TryFindBestBetterStorageFor(Thing t, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, out IHaulDestination haulDestination, bool needAccurateResult = true)
		{
			Log.Message("TEST currentPriority: " + currentPriority);
			IntVec3 foundCell2 = IntVec3.Invalid;
			StoragePriority storagePriority = StoragePriority.Unstored;
			if (StoreUtility.TryFindBestBetterStoreCellFor(t, carrier, map, currentPriority, faction, out foundCell2, needAccurateResult))
			{
				storagePriority = foundCell2.GetSlotGroup(map).Settings.Priority;
			}
            else
            {
                Log.Message("1 FAIL storagePriority: " + storagePriority);
            }
            
			if (!StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(t, carrier, map, currentPriority, faction, out var haulDestination2))
			{
				haulDestination2 = null;
			}
			if (storagePriority == StoragePriority.Unstored && haulDestination2 == null)
			{
				foundCell = IntVec3.Invalid;
				haulDestination = null;
				Log.Message("2 FAIL storagePriority: " + storagePriority);
				return false;
			}
			if (haulDestination2 != null && (storagePriority == StoragePriority.Unstored || (int)haulDestination2.GetStoreSettings().Priority > (int)storagePriority))
			{
				foundCell = IntVec3.Invalid;
				haulDestination = haulDestination2;
				return true;
			}
			foundCell = foundCell2;
			haulDestination = foundCell2.GetSlotGroup(map).parent;
			return true;
		}

	}
}
