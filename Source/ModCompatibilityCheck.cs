using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Share_The_Load
{
	[StaticConstructorOnStartup]
	public class ModCompatibilityCheck
	{
		public static bool ExtendedStorageIsActive
		{
			get
			{
				return ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name == "ExtendedStorageFluffyHarmonised");
			}
		}
	}
}