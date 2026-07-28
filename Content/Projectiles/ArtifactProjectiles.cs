using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles;

public class FlameSpiritFanProjectile : QiFireballProjectile
{
	public override string Texture => "Xianxia/Content/Projectiles/QiFireballProjectile";
	protected override bool AbilityTerrainDestructionEnabled => false;
}

public class VerdantAntlerBoltProjectile : ModProjectile
{
	public override string Texture =>
		"Xianxia/Content/Items/Materials/SpiritBeasts/JadeAntler";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 3;
		Projectile.timeLeft = 150;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 12;
	}

	public override void AI()
	{
		Projectile.rotation += 0.13f * Projectile.direction;
		int targetIndex = Projectile.FindTargetWithLineOfSight(720f);
		if (targetIndex >= 0)
		{
			Vector2 desired = Projectile.DirectionTo(Main.npc[targetIndex].Center)
				* 13.5f;
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.075f);
		}

		Lighting.AddLight(Projectile.Center, 0.08f, 0.45f, 0.17f);
		if (Main.rand.NextBool(2)
			&& CultivationClientConfig.ShouldSpawnParticle())
		{
			Dust leaf = Dust.NewDustPerfect(Projectile.Center,
				DustID.GemEmerald,
				-Projectile.velocity * 0.08f
					+ Main.rand.NextVector2Circular(0.8f, 0.8f),
				newColor: new Color(70, 240, 125), Scale: 0.8f);
			leaf.noGravity = true;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(BuffID.Poisoned, 180);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Vector2 origin = texture.Size() * 0.5f;
		for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
		{
			float strength = 1f - i / (float)Projectile.oldPos.Length;
			Main.EntitySpriteDraw(texture,
				Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
				null, new Color(75, 235, 125) * strength * 0.28f,
				Projectile.oldRot[i], origin, Projectile.scale * 0.72f,
				SpriteEffects.None);
		}
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition,
			null, lightColor, Projectile.rotation, origin, Projectile.scale * 0.72f,
			SpriteEffects.None);
		return false;
	}
}

public class ThunderclapOrbProjectile : ModProjectile
{
	public override string Texture =>
		"Xianxia/Content/Items/Materials/SpiritBeasts/ThunderEssence";

	public override void SetDefaults()
	{
		Projectile.width = 14;
		Projectile.height = 14;
		Projectile.scale = 0.55f;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 4;
		Projectile.timeLeft = 55;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.extraUpdates = 1;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 10;
	}

	public override void AI()
	{
		Projectile.rotation = Projectile.velocity.ToRotation();
		Lighting.AddLight(Projectile.Center, 0.2f, 0.55f, 1f);

		if (Main.netMode != NetmodeID.Server
			&& CultivationClientConfig.ShouldSpawnParticle())
		{
			Vector2 trailPosition = Projectile.Center
				- Projectile.velocity * Main.rand.NextFloat(0.15f, 0.85f)
				+ Main.rand.NextVector2Circular(2.5f, 2.5f);
			Dust spark = Dust.NewDustPerfect(trailPosition,
				DustID.Electric,
				-Projectile.velocity * Main.rand.NextFloat(0.025f, 0.07f)
					+ Main.rand.NextVector2Circular(0.45f, 0.45f),
				80, new Color(75, 220, 255),
				Main.rand.NextFloat(0.65f, 0.95f));
			spark.noGravity = true;

			if (Main.rand.NextBool(3))
			{
				Dust glow = Dust.NewDustPerfect(trailPosition,
					DustID.GemSapphire,
					Main.rand.NextVector2Circular(0.25f, 0.25f),
					110, new Color(190, 245, 255),
					Main.rand.NextFloat(0.45f, 0.7f));
				glow.noGravity = true;
			}
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(BuffID.Electrified, 150);
	}

	public override bool PreDraw(ref Color lightColor) => false;
}

public class BeastSoulGuardianProjectile : ModProjectile
{
	public const float MaxOwnerDistance = 320f;
	public const int AccessoryRefreshTime = 10;

	public override string Texture =>
		"Xianxia/Content/Items/Artifacts/BeastSoulBanner";

	public override void SetStaticDefaults()
	{
		Main.projPet[Type] = true;
		ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		ProjectileID.Sets.MinionSacrificable[Type] = false;
	}

	public override void SetDefaults()
	{
		Projectile.width = 34;
		Projectile.height = 42;
		Projectile.friendly = true;
		Projectile.minion = true;
		Projectile.minionSlots = 0f;
		Projectile.netImportant = true;
		Projectile.DamageType = DamageClass.Summon;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = AccessoryRefreshTime;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead)
		{
			Projectile.Kill();
			return;
		}

		float angle = Main.GameUpdateCount * 0.025f + Projectile.identity;
		Vector2 idle = owner.Center
			+ new Vector2(82f, 0f).RotatedBy(angle);
		if (Vector2.DistanceSquared(Projectile.Center, owner.Center)
			> MaxOwnerDistance * MaxOwnerDistance)
		{
			Projectile.Center = idle;
			Projectile.velocity = Vector2.Zero;
			if (Projectile.owner == Main.myPlayer)
				Projectile.netUpdate = true;
		}

		int targetIndex = Projectile.FindTargetWithLineOfSight(780f);
		Vector2 destination = idle;
		Vector2 desired = owner.velocity + Projectile.DirectionTo(destination)
			* Math.Min(15f, Projectile.Distance(destination) * 0.12f + 2f);
		float maxSpeed = Math.Max(15f, owner.velocity.Length() + 12f);
		if (desired.LengthSquared() > maxSpeed * maxSpeed)
			desired = desired.SafeNormalize(Vector2.Zero) * maxSpeed;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.14f);
		Projectile.rotation = MathHelper.Lerp(Projectile.rotation,
			Projectile.velocity.X * 0.025f, 0.1f);

		if (targetIndex >= 0 && ++Projectile.ai[0] >= 45f
			&& Projectile.owner == Main.myPlayer)
		{
			Projectile.ai[0] = 0f;
			Vector2 velocity = Projectile.DirectionTo(Main.npc[targetIndex].Center)
				* 15f;
			Projectile.NewProjectile(Projectile.GetSource_FromThis(),
				Projectile.Center, velocity,
				ModContent.ProjectileType<BeastSoulBoltProjectile>(),
				Projectile.damage, Projectile.knockBack, Projectile.owner);
		}

		Lighting.AddLight(Projectile.Center, 0.18f, 0.32f, 0.7f);
	}

	public override bool? CanDamage() => false;
}

public class BeastSoulBoltProjectile : ModProjectile
{
	public override string Texture =>
		"Xianxia/Content/Items/Materials/SpiritBeasts/CoreFormationBeastCore";

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Summon;
		Projectile.penetrate = 2;
		Projectile.timeLeft = 90;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		Projectile.rotation += 0.2f;
		int target = Projectile.FindTargetWithLineOfSight(560f);
		if (target >= 0)
			Projectile.velocity = Vector2.Lerp(Projectile.velocity,
				Projectile.DirectionTo(Main.npc[target].Center) * 16f, 0.1f);
		Lighting.AddLight(Projectile.Center, 0.18f, 0.25f, 0.75f);
		Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.ShadowbeamStaff,
			-Projectile.velocity * 0.05f, Scale: 0.85f);
		dust.noGravity = true;
	}
}
