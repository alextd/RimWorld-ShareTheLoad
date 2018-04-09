using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Harmony;

namespace Share_The_Load
{
	public class DeliverQuota : IExposable
	{
		public Pawn claimant;
		public Job job;
		public Thing claimed;
		public ThingDef resource;
		public int count;

		public DeliverQuota() { }
		public DeliverQuota(Pawn p, Job j, Thing t, ThingDef r, int c)
		{
			claimant = p;
			job = j;
			claimed = t;
			resource = r;
			count = c;
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref claimant, "claimant");
			Scribe_References.Look(ref job, "job");
			Scribe_References.Look(ref claimed, "claimed");
			Scribe_Defs.Look(ref resource, "resource");
			Scribe_Values.Look(ref count, "count");
		}
	}

	public class ExpectingComp : GameComponent
	{
		public List<DeliverQuota> pawnQuotas;

		public ExpectingComp(Game game)
		{
			pawnQuotas = new List<DeliverQuota>();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref pawnQuotas, "pawnQuotas");
		}

		public static List<DeliverQuota> GetQuotas()
		{
			return Current.Game.GetComponent<ExpectingComp>().pawnQuotas;
		}

		public static void Remove(Predicate<DeliverQuota> filter)
		{
			GetQuotas().RemoveAll(filter);
		}

		public static void Add(Pawn claimant, Job job, Thing t, ThingDef def, int count)
		{
			GetQuotas().Add(new DeliverQuota(claimant, job, t, def, count));
		}

		public static int ExpectedCount(Thing b, ThingDef res)
		{
			return GetQuotas().FindAll(q => q.claimed == b && q.resource == res).Sum(q => q.count);
		}
	}


	[HarmonyPatch(typeof(Thing), "DeSpawn")]
	public static class DeSpawn_Patch
	{
		public static void Prefix(Thing __instance)
		{
			ExpectingComp.Remove(q => q.claimed == __instance);
		}
	}
}
