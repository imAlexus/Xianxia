using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Projectiles.SpiritBeasts;

namespace Xianxia.Content.NPCs.SpiritBeasts;

public class ThunderclawTiger : SpiritBeastNPC
{
	protected override int BeastRealm => 3;
	protected override int MinimumStage => 4;
	protected override int MaximumStage => 9;
	protected override float MinimumSpawnDistanceBlocks => 700f;
	protected override string BestiaryKey => "ThunderclawTiger";

	public override void SetDefaults()
	{
		NPC.width = 86;
		NPC.height = 58;
		NPC.damage = 115;
		NPC.defense = 38;
		NPC.lifeMax = 4200;
		NPC.knockBackResist = 0.08f;
		NPC.value = Item.buyPrice(gold: 2);
		NPC.npcSlots = 5f;
		NPC.rarity = 3;
		NPC.HitSound = SoundID.NPCHit4;
		NPC.DeathSound = SoundID.NPCDeath14;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) =>
		spawnInfo.Player.ZoneJungle && spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime
			? GetSpawnWeight(spawnInfo, 0.012f)
			: 0f;

	public override void AI()
	{
		Player player = TargetClosestPlayer();
		NPC.ai[0]++;
		float distance = NPC.Distance(player.Center);
		MoveHorizontally(NPC.DirectionTo(player.Center).X * 5.2f, 0.075f);

		if (NPC.ai[0] % 100f == 0f && distance < 850f && Main.netMode != NetmodeID.MultiplayerClient)
		{
			for (int i = -1; i <= 1; i++)
			{
				Vector2 spawn = player.Center + new Vector2(i * 80f, -520f);
				Vector2 velocity = Vector2.UnitY * (10f + i * 0.35f);
				Projectile.NewProjectile(NPC.GetSource_FromAI(), spawn, velocity,
					ModContent.ProjectileType<SpiritLightningProjectile>(), NPC.damage / 2, 3f,
					Main.myPlayer);
			}
		}
		if (NPC.ai[0] % 150f == 75f && distance < 600f)
		{
			NPC.velocity = NPC.DirectionTo(player.Center) * 12f;
			NPC.velocity.Y = -8.5f;
			NPC.netUpdate = true;
		}
		if (NPC.collideX)
		{
			NPC.velocity.Y = -8f;
		}
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) =>
		target.AddBuff(BuffID.Electrified, 180);

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpiritBeastBlood>(), 1, 4, 8));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ThunderEssence>(), 1, 2, 5));
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CoreFormationBeastCore>(), 1));
	}
}
