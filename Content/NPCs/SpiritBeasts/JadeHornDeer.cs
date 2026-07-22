using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials.SpiritBeasts;

namespace Xianxia.Content.NPCs.SpiritBeasts;

public class JadeHornDeer : SpiritBeastNPC
{
	protected override int BeastRealm => 1;
	protected override int MinimumStage => 2;
	protected override int MaximumStage => 8;
	protected override float MinimumSpawnDistanceBlocks => 200f;
	protected override string BestiaryKey => "JadeHornDeer";

	public override void SetDefaults()
	{
		NPC.width = 62;
		NPC.height = 52;
		NPC.damage = 32;
		NPC.defense = 8;
		NPC.lifeMax = 260;
		NPC.knockBackResist = 0.45f;
		NPC.value = Item.buyPrice(silver: 8);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) =>
		IsNaturalSurface(spawnInfo.Player) && Main.dayTime
			? GetSpawnWeight(spawnInfo, 0.07f)
			: 0f;

	public override void AI()
	{
		Player player = TargetClosestPlayer();
		NPC.ai[0]++;
		float distance = NPC.Distance(player.Center);
		if (NPC.ai[1] > 0f)
		{
			NPC.ai[1]--;
			MoveHorizontally(NPC.direction * 8.5f, 0.18f);
		}
		else
		{
			MoveHorizontally(NPC.DirectionTo(player.Center).X * (distance < 420f ? 2.5f : 1.2f), 0.06f);
			if (distance < 420f && NPC.ai[0] >= 150f)
			{
				NPC.ai[0] = 0f;
				NPC.ai[1] = 38f;
				NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
				NPC.netUpdate = true;
			}
		}

		if (NPC.collideX)
		{
			NPC.velocity.Y = -6.5f;
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpiritBeastBlood>(), 1, 1, 3));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<JadeAntler>(), 2, 1, 2));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<QiGatheringBeastCore>(), 3));
	}
}
