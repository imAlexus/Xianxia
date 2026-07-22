using System;
using Terraria;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Common.NPCs;

public class AlchemyPillSpawnGlobalNPC : GlobalNPC
{
	public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
	{
		AlchemyPillEffectPlayer effects = player.GetModPlayer<AlchemyPillEffectPlayer>();
		if (effects.SpiritBeastLure)
		{
			spawnRate = Math.Max(10, (int)(spawnRate * 0.7f));
			maxSpawns += 3;
		}
		if (effects.Concealment)
		{
			spawnRate = Math.Max(10, (int)(spawnRate * 1.8f));
			maxSpawns = Math.Max(1, (int)(maxSpawns * 0.6f));
		}
	}
}
