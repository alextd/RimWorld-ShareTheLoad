using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using UnityEngine;//For the Mathf.min of 3 things

namespace Share_The_Load
{
	[StaticConstructorOnStartup]
	public static class ExtendedStoragePatches
	{
		public static Type typeBuilding_ExtendedStorage;
		static ExtendedStoragePatches()
		{
			Log.Message($"Share The Load checking with Extended Storage!");
			typeBuilding_ExtendedStorage = AccessTools.TypeByName("ExtendedStorage.Building_ExtendedStorage");
			if (typeBuilding_ExtendedStorage == null) return;

			ApparentMaxStorageInfo = AccessTools.Property(typeBuilding_ExtendedStorage, "ApparentMaxStorage").GetGetMethod();
			StoredThingTotalInfo = AccessTools.Property(typeBuilding_ExtendedStorage, "StoredThingTotal").GetGetMethod();

			if(ApparentMaxStorageInfo == null || StoredThingTotalInfo == null)
			{
				Verse.Log.Warning("ShareTheLoad couldn't work with ExtendedStorage, whooops!");
				return;
			}
			Log.Message($"Share The Load patching with Extended Storage!");

			Harmony harmony = new Harmony("Uuugggg.rimworld.Share_The_Load-ES.main");
			harmony.Patch(AccessTools.Method(typeof(ReservationManager), "CanReserve"),
				new HarmonyMethod(typeof(CanReserve_Patch_ES), "Prefix"), null);
			harmony.Patch(AccessTools.Method(typeof(ReservationManager), "Reserve"),
				new HarmonyMethod(typeof(Reserve_Patch_ES), "Prefix"), null);
			harmony.Patch(AccessTools.Method(typeof(ReservationManager), "Release"),
				new HarmonyMethod(typeof(Release_Patch_ES), "Prefix"), null);
			harmony.Patch(AccessTools.Method(typeof(ReservationManager), "ReleaseClaimedBy"),
				new HarmonyMethod(typeof(ReleaseClaimedBy_Patch_ES), "Prefix"), null);
		}

		public static MethodInfo ApparentMaxStorageInfo;
		public static int ApparentMaxStorage(this Thing building)//Building_ExtendedStorage
		{
			return (int)ApparentMaxStorageInfo.Invoke(building, null);
		}

		public static MethodInfo StoredThingTotalInfo;
		public static int StoredThingTotal(this Thing building)//Building_ExtendedStorage
		{
			return (int)StoredThingTotalInfo.Invoke(building, null);
		}
	}

	//[HarmonyPatch(typeof(ReservationManager), "CanReserve")]
	static class CanReserve_Patch_ES
	{
		//public bool CanReserve(Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		public static bool Prefix(Pawn claimant, LocalTargetInfo target, ref bool __result)
		{
			if (claimant.IsFreeColonist && target.Cell != LocalTargetInfo.Invalid)
			{
				if(claimant.Map.thingGrid.ThingsAt(target.Cell)
					.FirstOrDefault(t => t.GetType() == ExtendedStoragePatches.typeBuilding_ExtendedStorage)
					is Thing storage)
				{
					Log.Message($"{claimant} can reserveES? {target.Cell} is {storage}");

					int canDo = storage.ApparentMaxStorage() - storage.StoredThingTotal();
					int expected = ExpectingComp.ExpectedCount(q => q.claimed == storage);

					if (canDo > expected)
					{
						Log.Message($"{claimant} can reserveES {storage}");
						__result = true;
						return false;
					}
				}
			}
			return true;
		}
	}

	//[HarmonyPatch(typeof(ReservationManager), "Reserve")]
	static class Reserve_Patch_ES
	{
		//public bool Reserve(Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null)
		public static bool Prefix(Pawn claimant, Job job, LocalTargetInfo target, ref bool __result)
		{
			if (claimant.IsFreeColonist && target.Cell != LocalTargetInfo.Invalid
				&& claimant.Map.thingGrid.ThingsAt(target.Cell)
					.FirstOrDefault(t => t.GetType() == ExtendedStoragePatches.typeBuilding_ExtendedStorage) is Thing storage
				&& job.def == JobDefOf.HaulToCell)
			{
				int canDo = storage.ApparentMaxStorage() - storage.StoredThingTotal();
				if (canDo > 0)
				{
					int count = job.count;
					Thing deliverThing = job.targetA.Thing;
					ThingDef resource = deliverThing.def;

					Log.Message($"{claimant} reservingES {storage} resource = {resource}({count})");
					Log.Message($"	out of: {canDo}");


					int availableCount = deliverThing.stackCount;// + job.targetQueueA?.Sum(tar => tar.Thing.stackCount) ?? 0;
																											 //HaulToCell doesn't queue up its reservations, and so we don't know if there are more to get
					count = Mathf.Min(new int[] { count, claimant.carryTracker.MaxStackSpaceEver(resource), availableCount, canDo });

					Log.Message($"{storage} was expecting {resource}(" + ExpectingComp.ExpectedCount(storage, resource) + ")");
					ExpectingComp.Add(claimant, job, storage, resource, count);
					Log.Message($"{storage} now expecting {resource}(" + ExpectingComp.ExpectedCount(storage, resource) + ")");

					__result = true;
					return false;
				}
			}
			return true;
		}
	}

	//[HarmonyPatch(typeof(ReservationManager), "Release")]
	static class Release_Patch_ES
	{
		//public void Release(LocalTargetInfo target, Pawn claimant, Job job)
		public static void Prefix(LocalTargetInfo target, Pawn claimant, Job job)
		{
			if (claimant.IsFreeColonist && target.Cell != LocalTargetInfo.Invalid
				&& claimant.Map.thingGrid.ThingsAt(target.Cell).FirstOrDefault(t => t.GetType() == ExtendedStoragePatches.typeBuilding_ExtendedStorage) is Thing thing
				&& job.def == JobDefOf.HaulToCell)
				ExpectingComp.Remove(q => q.claimant == claimant && q.job == job && q.claimed == thing);
		}
	}

	//[HarmonyPatch(typeof(ReservationManager), "ReleaseClaimedBy")]
	static class ReleaseClaimedBy_Patch_ES
	{
		//public void ReleaseClaimedBy(Pawn claimant, Job job)
		public static void Prefix(Pawn claimant, Job job)
		{
			if (job.def == JobDefOf.HaulToCell)
				ExpectingComp.Remove(q => q.claimant == claimant && q.job == job);
		}
	}

	//Redundant with normal Constructible
	//[HarmonyPatch(typeof(ReservationManager), "ReleaseAllClaimedBy")]
	//static class ReleaseAllClaimedBy_Patch_ES
	//{
	//	//public void ReleaseAllClaimedBy(Pawn claimant)
	//	public static void Prefix(Pawn claimant)
	//	{
	//		ExpectingComp.Remove(q => q.claimant == claimant);
	//	}
	//}
}