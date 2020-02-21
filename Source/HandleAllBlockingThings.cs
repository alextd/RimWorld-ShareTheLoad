using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.Emit.OpCodes;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;

namespace Share_The_Load
{
	[HarmonyPatch(typeof(GenConstruct), "HandleBlockingThingJob")]
	class HandleAllBlockingThings
	{
		//public static Job HandleBlockingThingJob(Thing constructible, Pawn worker, bool forced = false)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo FirstBlockingThingInfo = AccessTools.Method(typeof(GenConstruct), "FirstBlockingThing");

			MethodInfo FirstReservableBlockingThingInfo = AccessTools.Method(typeof(HandleAllBlockingThings), nameof(FirstReservableBlockingThing));

			foreach (CodeInstruction i in instructions)
			{
				if (i.opcode == Call && i.operand.Equals(FirstBlockingThingInfo))
					i.operand = FirstReservableBlockingThingInfo;
				yield return i;
			}
		}

		public static Thing FirstReservableBlockingThing(Thing constructible, Pawn pawnToIgnore)
		{
			Thing thing = constructible is Blueprint b ? GenConstruct.MiniToInstallOrBuildingToReinstall(b) : null;

			for(var iterator = constructible.OccupiedRect().GetIterator(); !iterator.Done(); iterator.MoveNext())
				foreach(Thing t in iterator.Current.GetThingList(constructible.Map))
					if (GenConstruct.BlocksConstruction(constructible, t) && t != pawnToIgnore && t != thing && pawnToIgnore.CanReserve(t))
						return t;

			return null;
		}
	}

	[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	static class PawnBlockConstruction
	{
		static bool Prefix(ref bool __result, Thing t)
		{
			if (t is Pawn)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
