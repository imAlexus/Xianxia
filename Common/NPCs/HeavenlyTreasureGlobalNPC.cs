using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Common.NPCs;

public sealed class HeavenlyTreasureGlobalNPC : GlobalNPC
{
	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		int treasureType = npc.type switch
		{
			NPCID.EyeofCthulhu => ModContent.ItemType<HeavenlyEyeEssence>(),
			NPCID.QueenBee => ModContent.ItemType<HeavenlyRoyalNectar>(),
			NPCID.SkeletronHead => ModContent.ItemType<HeavenlyBoneMarrow>(),
			_ => 0
		};
		if (treasureType > 0)
			npcLoot.Add(ItemDropRule.Common(treasureType));
	}
}
