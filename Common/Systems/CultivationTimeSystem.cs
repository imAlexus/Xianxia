using Terraria;
using Terraria.ModLoader;
using Xianxia.Common.Config;
using Xianxia.Common.Players;

namespace Xianxia.Common.Systems;

public class CultivationTimeSystem : ModSystem
{
	public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate)
	{
		// The server configuration and meditation states are synchronized, so the
		// same rate is applied on every peer while the server remains authoritative.
		if (!CultivationServerConfig.Instance.EnableTimeAcceleration)
		{
			return;
		}

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player player = Main.player[i];
			if (player.active && !player.dead && player.GetModPlayer<CultivationPlayer>().IsMeditating)
			{
				timeRate *= CultivationServerConfig.Instance.TimeMultiplier;
				return;
			}
		}
	}
}
