using HarmonyLib;
using Ionic.Zlib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using static PickUpAndHaul.WorkGiver_HaulToInventory;
using static Verse.AI.ReservationManager;

namespace StackReservationFix
{
    [StaticConstructorOnStartup]
    public static class PickupAndHaulHelper
    {
		static PickupAndHaulHelper()
        {
			StackReservationFixMod.harmony.Patch(AccessTools.Method(typeof(PickUpAndHaul.WorkGiver_HaulToInventory),
				nameof(PickUpAndHaul.WorkGiver_HaulToInventory.AllocateThingAtCell)), 
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(PickupAndHaulHelper), nameof(AllocateThingAtCellTranspiler))));
			StackReservationFixMod.harmony.Patch(AccessTools.Method(typeof(PickUpAndHaul.CompHauledToInventory),
				nameof(PickUpAndHaul.CompHauledToInventory.RegisterHauledItem)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(PickupAndHaulHelper), nameof(RegisterHauledItemPostfix))));
        }

        public static void RegisterHauledItemPostfix(Thing thing)
        {
			if (thing.ParentHolder is Pawn_InventoryTracker pawn_InventoryTracker)
			{
				Helpers.AddThingHaul(pawn_InventoryTracker.pawn, pawn_InventoryTracker.pawn.CurJob.targetB.Cell, thing, thing.stackCount);
                Log.Message("RegisterHauledItemPostfix: " + thing + " - " + thing.stackCount + pawn_InventoryTracker.pawn.CurJob.JobSummary(pawn_InventoryTracker.pawn));
            }
        }
        public static IEnumerable<CodeInstruction> AllocateThingAtCellTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
			var codes = codeInstructions.ToList();
			for (var i = 0; i < codes.Count; i++)
            {
				var code = codes[i];
				if (code.opcode == OpCodes.Ldc_I4_1 && codes[i + 1].opcode == OpCodes.Ret)
                {
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Ldloc_2);
					yield return new CodeInstruction(OpCodes.Ldarg_2);
					yield return new CodeInstruction(OpCodes.Ldloc_3);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PickupAndHaulHelper), nameof(RegisterThing)));


				}
				yield return code;
            }
        }

		public static void RegisterThing(Pawn hauler, PickUpAndHaul.WorkGiver_HaulToInventory.StoreTarget storeTarget, Thing thing, int count)
        {
			Log.Message("RegisterThing: " + hauler + " - storeTarget: " + storeTarget + " - thing: " + thing + " - count: " + count);
			Helpers.AddThingHaul(hauler, storeTarget.cell, thing, count);
        }
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
