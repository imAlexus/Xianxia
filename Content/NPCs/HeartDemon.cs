using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.Projectiles;

namespace Xianxia.Content.NPCs;

public sealed class HeartDemon : ModNPC
{
	private const float MaximumOwnerDistance = 2200f;
	private int OwnerIndex => Math.Clamp((int)NPC.ai[0], 0, Main.maxPlayers - 1);
	private int Realm => Math.Clamp((int)NPC.ai[1], 0, 4);
	private int Stage => Math.Clamp((int)NPC.ai[2], 1, 9);
	private int DemonPoints => Math.Clamp((int)NPC.ai[3], 1, 9);

	public override string Texture =>
		$"Terraria/Images/NPC_{NPCID.Tim}";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Tim];
		NPCID.Sets.MPAllowedEnemies[Type] = true;
	}

	public override void SetDefaults()
	{
		NPC.width = 38;
		NPC.height = 52;
		NPC.damage = 60;
		NPC.defense = 12;
		NPC.lifeMax = 1800;
		NPC.knockBackResist = 0f;
		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.lavaImmune = true;
		NPC.boss = true;
		NPC.npcSlots = 10f;
		NPC.value = 0f;
		NPC.aiStyle = -1;
		NPC.HitSound = SoundID.NPCHit54;
		NPC.DeathSound = SoundID.NPCDeath52;
		NPC.netAlways = true;
	}

	public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
	{
		float lifeMultiplier = 1f + Realm * 1.8f
			+ (Stage - 1) * 0.10f;
		float heartMultiplier = 1f + DemonPoints * 0.12f;
		float difficulty = Main.masterMode ? 1.55f
			: Main.expertMode ? 1.25f : 1f;
		NPC.lifeMax = Math.Max(800,
			(int)MathF.Round(1800f * lifeMultiplier
				* heartMultiplier * difficulty));
		NPC.life = NPC.lifeMax;
		float damageMultiplier = 1f + Realm * 0.55f
			+ (Stage - 1) * 0.05f;
		float heartDamageMultiplier = 1f + DemonPoints * 0.08f;
		NPC.damage = Math.Max(35,
			(int)MathF.Round(60f * damageMultiplier
				* heartDamageMultiplier * difficulty));
		NPC.defense = 12 + Realm * 18 + DemonPoints * 3;
	}

	public override bool? CanBeHitByItem(Player player, Item item) =>
		player.whoAmI == OwnerIndex ? null : false;

	public override bool? CanBeHitByProjectile(Projectile projectile) =>
		projectile.owner == OwnerIndex ? null : false;

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) =>
		target.whoAmI == OwnerIndex;

	public override void AI()
	{
		Player owner = Main.player[OwnerIndex];
		if (!owner.active || owner.dead
			|| Vector2.DistanceSquared(NPC.Center, owner.Center)
				> MaximumOwnerDistance * MaximumOwnerDistance)
		{
			owner.GetModPlayer<CultivationPlayer>()
				.FailHeartDemonTrial(showMessage: true);
			NPC.active = false;
			NPC.netUpdate = true;
			return;
		}

		NPC.target = OwnerIndex;
		NPC.timeLeft = 300;
		NPC.spriteDirection = NPC.direction =
			owner.Center.X >= NPC.Center.X ? 1 : -1;
		NPC.localAI[0]++;
		float aggression = 1f + DemonPoints * 0.035f;
		float hoverDistance = Math.Max(130f, 270f - Realm * 24f);
		float angle = NPC.localAI[0] * (0.012f + Realm * 0.002f);
		Vector2 desiredPosition = owner.Center
			+ new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.45f)
				* hoverDistance
			- Vector2.UnitY * (80f + Realm * 12f);
		Vector2 desiredVelocity = NPC.DirectionTo(desiredPosition)
			* (5.5f + Realm * 1.15f) * aggression;
		NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.045f);

		int attackInterval = Math.Max(38,
			95 - Realm * 8 - DemonPoints * 3);
		if ((int)NPC.localAI[0] % attackInterval == 0)
			PerformRealmAttack(owner);
		if (Realm >= 4 && (int)NPC.localAI[0] % 240 == 0)
		{
			Vector2 teleportOffset =
				Main.rand.NextVector2CircularEdge(320f, 190f);
			NPC.Center = owner.Center + teleportOffset;
			NPC.velocity = Vector2.Zero;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
		}
	}

	private void PerformRealmAttack(Player owner)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (Realm == 0)
		{
			NPC.velocity = NPC.DirectionTo(owner.Center)
				* (12f + DemonPoints * 0.4f);
			NPC.netUpdate = true;
			return;
		}

		int projectileCount = Realm switch
		{
			1 => 1,
			2 => 3,
			3 => 5,
			_ => 7
		};
		float speed = 8f + Realm * 1.4f;
		Vector2 baseVelocity = NPC.DirectionTo(owner.Center) * speed;
		for (int i = 0; i < projectileCount; i++)
		{
			float spread = projectileCount <= 1 ? 0f
				: MathHelper.Lerp(-0.42f, 0.42f,
					i / (float)(projectileCount - 1));
			Projectile.NewProjectile(
				NPC.GetSource_FromAI(), NPC.Center,
				baseVelocity.RotatedBy(spread),
				ModContent.ProjectileType<HeartDemonBolt>(),
				Math.Max(20, NPC.damage / 2), 2f,
				Main.myPlayer, OwnerIndex,
				Realm >= 3 ? 1f : 0f);
		}
		if (Realm >= 2 && Main.rand.NextBool(2))
			NPC.velocity = NPC.DirectionTo(owner.Center)
				* (13f + Realm * 1.2f);
		SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter += 0.18;
		NPC.frame.Y = (int)NPC.frameCounter
			% Main.npcFrameCount[NPCID.Tim] * frameHeight;
	}

	public override void PostDraw(SpriteBatch spriteBatch,
		Vector2 screenPos, Color drawColor)
	{
		Texture2D texture = TextureAssets.Npc[Type].Value;
		Vector2 origin = NPC.frame.Size() * 0.5f;
		Color aura = Color.Lerp(Color.MediumPurple,
			Color.OrangeRed, DemonPoints / 9f) * 0.32f;
		for (int i = 0; i < 6; i++)
		{
			float angle = MathHelper.TwoPi * i / 6f
				+ (float)Main.GameUpdateCount * 0.025f;
			Vector2 offset = angle.ToRotationVector2()
				* (5f + DemonPoints * 0.5f);
			spriteBatch.Draw(texture,
				NPC.Center - screenPos + offset,
				NPC.frame, aura, NPC.rotation, origin,
				NPC.scale, NPC.spriteDirection == -1
					? SpriteEffects.FlipHorizontally
					: SpriteEffects.None, 0f);
		}
	}

	public override void OnKill()
	{
		NPC.value = 0f;
		if (OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers
			&& Main.player[OwnerIndex].active)
		{
			Main.player[OwnerIndex]
				.GetModPlayer<CultivationPlayer>()
				.CompleteHeartDemonTrial(NPC.whoAmI);
		}
	}
}
