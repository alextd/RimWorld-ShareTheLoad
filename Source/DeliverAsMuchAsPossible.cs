using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using Harmony;

namespace Share_The_Load
{
	[HarmonyPatch(typeof(ItemAvailability), "ThingsAvailableAnywhere")]
	public static class DeliverAsMuchAsPossible
	{
		//public bool ThingsAvailableAnywhere(ThingCountClass need, Pawn pawn)
		public static bool Prefix(ThingCountClass need, Pawn pawn, ref bool __result)
		{
			if (Settings.Get().deliverAsMuchAsYouCan)
			{
				List<Thing> list = pawn.Map.listerThings.ThingsOfDef(need.thingDef);
				__result = list.Any(t => !t.IsForbidden(pawn));
				return false;
			}
			return true;
		}
	}

	//This is just to change a break into a continue
	//Honestly is a vanilla bug
	//Would only deliver resource #2 once there's enough resource #1 
	//Though resource #1 doesn't care if there's enough #2
	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResourceDeliverJobFor")]
	public static class BreakToContinue_Patch
	{
		//protected Job ResourceDeliverJobFor(Pawn pawn, IConstructible c, bool canRemoveExistingFloorUnderNearbyNeeders = true)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			Label forCheck = new Label();
			Label continueLabel = il.DefineLabel();
			bool firstBr = false;
			bool secondBr = false;
			bool setLabel = false;
			foreach (CodeInstruction inst in instructions)
			{
				if (inst.opcode == OpCodes.Br && !firstBr)
				{
					firstBr = true;
					forCheck = (Label)inst.operand;

					List<CodeInstruction> instructionsList = instructions.ToList();
					for (int i = instructionsList.Count() - 1; i >= 0; i--)
					{
						if (instructionsList[i].labels.Contains(forCheck))
						{
							instructionsList[i - 4].labels.Add(continueLabel);
							setLabel = true;
							break;
						}
					}
				}
				else if (inst.opcode == OpCodes.Br && !secondBr)
				{
					secondBr = true;
					if (setLabel)
						inst.operand = continueLabel;
				}
				yield return inst;
			}
		}
	}
}
