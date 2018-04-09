using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;
using UnityEngine;//For the Mathf.min of 3 things

namespace Share_The_Load
{
	[HarmonyPatch(typeof(ReservationManager), "CanReserve")]
	static class CanReserve_Patch
	{
		//public bool CanReserve(Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		public static bool Prefix(Pawn claimant, LocalTargetInfo target, ref bool __result)
		{
			if (target.Thing is IConstructible c && !(c is Blueprint_Install))
			{ 
				Log.Message(c + " needs " + c.MaterialsNeeded().ToStringSafeEnumerable());
				if (c.MaterialsNeeded().Count > 0)
				{
					Log.Message(claimant + " can reserve " + target.Thing);
					__result = true;
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ReservationManager), "Reserve")]
	static class Reserve_Patch
	{
		//public bool Reserve(Pawn claimant, Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null)
		public static bool Prefix(Pawn claimant, Job job, LocalTargetInfo target, ref bool __result)
		{
			if (target.Thing is IConstructible c && !(c is Blueprint_Install)
				&& job.def == JobDefOf.HaulToContainer)
			{
				int count = job.count;
				Thing deliverThing = job.targetA.Thing;
				int availableCount = deliverThing.stackCount + job.targetQueueA?.Sum(tar => tar.Thing.stackCount) ?? 0;
				count = Mathf.Min(new int[] { count, claimant.carryTracker.AvailableStackSpace(deliverThing.def), availableCount });
				Log.Message(c + " now expecting " + deliverThing.def + "(" + count + ")");
				Log.Message(c + " needs " + c.MaterialsNeeded().ToStringSafeEnumerable());
				if(c.MaterialsNeeded().Count > 0)
				{
					Log.Message(claimant + " reserving " + target.Thing);
					__result = true;
					return false;
				}
			}
			return true;
		}
	}
}
