using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Share_The_Load
{
	public class Settings : ModSettings
	{
		public bool makeWayJobs = false;

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);
			
			options.CheckboxLabeled("TD.SettingBlockingJobs".Translate(), ref makeWayJobs, "TD.SettingBlockingJobsDesc".Translate());
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref makeWayJobs, "makeWayJobs", true);
		}
	}
}