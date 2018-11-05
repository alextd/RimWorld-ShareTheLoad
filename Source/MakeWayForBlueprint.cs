using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;

namespace Share_The_Load
{
	static class MakeWayForBlueprint
	{
		public static IEnumerable<Thing> MakeWayFor(Thing blueprint, ThingCategory category)
		{
			Map map = blueprint.Map;
			foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(blueprint))
				foreach (Thing thing in cell.GetThingList(map))
					if (thing.def.category == category)
						if (GenConstruct.BlocksConstruction(blueprint, thing))
							yield return thing;
		}
	}

	//Same things that GenConstruct.HandleBlockingThingJob does : cutting and mining. But now cutters/miners will do it.
	//Deconstructing is already going ot be done by builders
	[HarmonyPatch(typeof(WorkGiver_PlantsCut), "PotentialWorkThingsGlobal")]
	static class MakeWay_Plant
	{
		//public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, Pawn pawn)
		{
			foreach (Thing t in __result)
				yield return t;

			foreach (Thing blueprint in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
				foreach(Thing t in MakeWayForBlueprint.MakeWayFor(blueprint, ThingCategory.Plant))
					yield return t;
		}
	}

	[HarmonyPatch(typeof(WorkGiver_PlantsCut), "JobOnThing")]
	static class MakeWay_Plant_Job
	{
		//public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//RimWorld.DesignationDefOf::CutPlant
			FieldInfo CutPlantInfo = AccessTools.Field(typeof(DesignationDefOf), "CutPlant");

			bool afterCutPlantDes = false;
			foreach (CodeInstruction i in instructions)
			{

				if (afterCutPlantDes && i.opcode == OpCodes.Ldnull)
				{
					Log.Message($"Transpiling in NewCutPlantJob");
					yield return new CodeInstruction(OpCodes.Ldarg_2) { labels = i.labels }; //Thing t
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MakeWay_Plant_Job), nameof(NewCutPlantJob)));
				}
				else
					yield return i;

				if (i.operand == CutPlantInfo)
					afterCutPlantDes = true;
			}
		}
		public static Job NewCutPlantJob(Thing thing)
		{
			if(thing.Position.GetFirstThing<Blueprint>(thing.Map) != null)
				return new Job(JobDefOf.CutPlant, thing);
			return null;
		}
	}

	[HarmonyPatch(typeof(WorkGiver_Miner), "PotentialWorkThingsGlobal")]
	static class MakeWay_Miner
	{
		//public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, Pawn pawn)
		{
			foreach (Thing t in __result)
				yield return t;

			foreach (Thing blueprint in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
				foreach(Thing t in MakeWayForBlueprint.MakeWayFor(blueprint, ThingCategory.Building))
					if(t.def.mineable)
						yield return t;
		}
	}

	[HarmonyPatch(typeof(WorkGiver_Miner), "JobOnThing")]
	static class MakeWay_Miner_Job
	{
		//public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//public Designation DesignationAt(IntVec3 c, DesignationDef def)
			MethodInfo DesignationAtInfo = AccessTools.Method(typeof(DesignationManager), "DesignationAt");
			
			foreach (CodeInstruction i in instructions)
			{
				yield return i;

				if (i.operand == DesignationAtInfo)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_2); //Thing t
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MakeWay_Miner_Job), nameof(IsNotNullOrUnderBlueprint)));
				}
			}
		}
		public static bool IsNotNullOrUnderBlueprint(Designation des, Thing thing)
		{
			Log.Message($"IsNullAndNotUnderBlueprint {des} {thing}");
			return des != null || thing.Position.GetFirstThing<Blueprint>(thing.Map) != null;
		}

		public static void Postfix(Job __result, Thing t)
		{
			if (__result != null && t.Position.GetFirstThing<Blueprint>(t.Map) != null)
				__result.ignoreDesignations = true;
		}
	}
}
