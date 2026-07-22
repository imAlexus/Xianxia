using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Projectiles.SpiritBeasts;

namespace Xianxia.Content.NPCs.SpiritBeasts;

public class FlameTailedFox : SpiritBeastNPC
{
	protected override int BeastRealm => 2;
	protected override int MinimumStage => 3;
	protected override int MaximumStage => 9;
	protected override float MinimumSpawnDistanceBlocks => 450f;
	protected override string BestiaryKey => "FlameTailedFox";

	public override void SetDefaults()
	{
		NPC.width = 58;
		NPC.height = 40;
		NPC.damage = 62;
		NPC.defense = 18;
		NPC.lifeMax = 850;
		NPC.knockBackResist = 0.25f;
		NPC.value = Item.buyPrice(silver: 30);
		NPC.HitSound = SoundID.NPCHit5;
		NPC.DeathSound = SoundID.NPCDeath6;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) =>
		IsNaturalSurface(spawnInfo.Player) && !Main.dayTime
			? GetSpawnWeight(spawnInfo, 0.035f)
			: 0f;

	public override void AI()
	{
		Player player = TargetClosestPlayer();
		NPC.ai[0]++;
		float distance = NPC.Distance(player.Center);
		MoveHorizontally(NPC.DirectionTo(player.Center).X * (distance > 210f ? 3.8f : -1.8f), 0.08f);

		if (NPC.ai[0] % 120f == 0f && distance < 700f && Main.netMode != NetmodeID.MultiplayerClient)
		{
			Vector2 velocity = NPC.DirectionTo(player.Center) * 8f;
			Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
				ModContent.ProjectileType<SpiritFlameProjectile>(), NPC.damage / 2, 2f, Main.myPlayer);
		}
		if (NPC.ai[0] % 180f == 90f && distance < 480f)
		{
			NPC.velocity = NPC.DirectionTo(player.Center) * 9f;
			NPC.velocity.Y -= 2f;
			NPC.netUpdate = true;
		}
		if (NPC.collideX)
		{
			NPC.velocity.Y = -6f;
		}
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) =>
		target.AddBuff(BuffID.OnFire3, 240);

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpiritBeastBlood>(), 1, 2, 5));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FlameEssence>(), 1, 1, 3));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FoundationBeastCore>(), 2));
	}
}
