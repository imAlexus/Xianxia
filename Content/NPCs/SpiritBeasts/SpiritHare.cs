using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials.SpiritBeasts;

namespace Xianxia.Content.NPCs.SpiritBeasts;

public class SpiritHare : SpiritBeastNPC
{
	protected override int BeastRealm => 0;
	protected override int MinimumStage => 1;
	protected override int MaximumStage => 5;
	protected override float MinimumSpawnDistanceBlocks => 0f;
	protected override string BestiaryKey => "SpiritHare";

	public override void SetDefaults()
	{
		NPC.width = 38;
		NPC.height = 28;
		NPC.damage = 10;
		NPC.defense = 2;
		NPC.lifeMax = 45;
		NPC.knockBackResist = 0.8f;
		NPC.value = Item.buyPrice(copper: 80);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.noGravity = false;
		NPC.noTileCollide = false;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) =>
		IsNaturalSurface(spawnInfo.Player) && Main.dayTime
			? GetSpawnWeight(spawnInfo, 0.16f)
			: 0f;

	public override void AI()
	{
		Player player = TargetClosestPlayer();
		float distance = NPC.Distance(player.Center);
		NPC.ai[0]++;
		float desiredSpeed = distance < 230f
			? Math.Sign(NPC.Center.X - player.Center.X) * 4.2f
			: MathF.Sin(NPC.ai[0] * 0.018f) * 1.1f;
		MoveHorizontally(desiredSpeed, 0.08f);
		if ((NPC.collideX || (NPC.collideY && Main.rand.NextBool(distance < 230f ? 35 : 110))))
		{
			NPC.velocity.Y = -5.5f;
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpiritFur>(), 1, 1, 3));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MortalBeastCore>(), 5));
	}
}
