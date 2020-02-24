using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;

namespace Share_The_Load
{
	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "FindNearbyNeeders")]
	public static class FindNearbyNeeders_Patch
	{
		//private HashSet<Thing> FindNearbyNeeders(Pawn pawn, ThingDefCountClass need, IConstructible c, int resTotalAvailable, bool canRemoveExistingFloorUnderNearbyNeeders, out int neededTotal, out Job jobToMakeNeederAvailable)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo AmountNeededByOfInfo = AccessTools.Method(typeof(GenConstruct), "AmountNeededByOf");
			FieldInfo CountThingDefInfo = AccessTools.Field(typeof(ThingDefCountClass), "thingDef");

			MethodInfo AdjustForExpectedInfo = AccessTools.Method(typeof(FindNearbyNeeders_Patch), nameof(AdjustForExpected));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if(i.Calls(AmountNeededByOfInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldloc_2);
					yield return new CodeInstruction(OpCodes.Ldarg_2);
					yield return new CodeInstruction(OpCodes.Ldfld, CountThingDefInfo);
					yield return new CodeInstruction(OpCodes.Call, AdjustForExpectedInfo);
				}
			}
		}

		public static int AdjustForExpected(int needed, Thing c, ThingDef resource)
		{
			return needed - ExpectingComp.ExpectedCount(c, resource);
		}
	}

	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResourceDeliverJobFor")]
	public static class MaterialsNeeded_Patch
	{
		//protected Job ResourceDeliverJobFor(Pawn pawn, IConstructible c, bool canRemoveExistingFloorUnderNearbyNeeders = true)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo MaterialsNeededInfo = AccessTools.Method(typeof(IConstructible), "MaterialsNeeded");

			MethodInfo FilterForExpectedInfo = AccessTools.Method(typeof(MaterialsNeeded_Patch), nameof(MaterialsNeeded_Patch.FilterForExpected));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if(i.Calls(MaterialsNeededInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_2);//constructible thing
					yield return new CodeInstruction(OpCodes.Call, FilterForExpectedInfo);
				}
			}
		}

		public static List<ThingDefCountClass> FilterForExpected(List<ThingDefCountClass> materialsNeeded, Thing c)
		{
			List<ThingDefCountClass> needs = new List<ThingDefCountClass>(materialsNeeded.Count);
			foreach(var t in materialsNeeded)
			{
				needs.Add(new ThingDefCountClass(t.thingDef, t.count));
			}
			
			for (int i = 0; i < needs.Count; i++)
			{
				ThingDefCountClass thingNeeds = needs[i];
				thingNeeds.count -= ExpectingComp.ExpectedCount(c, thingNeeds.thingDef);
				if(thingNeeds.count <= 0)
				{
					needs.Remove(thingNeeds);
					i--;
				}
			}
			return needs;
		}
	}
}
