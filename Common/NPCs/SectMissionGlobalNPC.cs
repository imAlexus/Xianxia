using Terraria;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.NPCs.SpiritBeasts;

namespace Xianxia.Common.NPCs;

public class SectMissionGlobalNPC : GlobalNPC
{
	public override void OnKill(NPC npc)
	{
		if (npc.ModNPC is not SpiritBeastNPC)
			return;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player player = Main.player[i];
			if (player.active && npc.playerInteraction[i])
				player.GetModPlayer<SectPlayer>().RecordSpiritBeastKill();
		}
	}
}
