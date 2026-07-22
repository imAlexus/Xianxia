using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles;

public class QiFireballProjectile : DamagingQiAbilityProjectile
{
	protected override bool AbilityTerrainDestructionEnabled =>
		CultivationServerConfig.Instance.EnableFireballTerrainDestruction;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 6;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 180;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = false;
	}

	public override void AI()
	{
		if (Projectile.localAI[0] == 0f)
		{
			Projectile.localAI[0] = 1f;
			Vector2 center = Projectile.Center;
			Projectile.scale = MathHelper.Clamp(Projectile.ai[0], 1f, 2.5f);
			Projectile.width = Math.Max(18, (int)(18f * Projectile.scale));
			Projectile.height = Math.Max(18, (int)(18f * Projectile.scale));
			Projectile.Center = center;
		}

		Projectile.rotation += 0.18f * Projectile.direction;
		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Projectile.Center,
			0.9f * Projectile.scale * visualIntensity,
			0.25f * Projectile.scale * visualIntensity,
			0.05f * visualIntensity);

		int flameParticleCount = CultivationClientConfig.ScaleParticleCount(
			2 + (int)MathF.Ceiling(Projectile.scale));
		for (int i = 0; i < flameParticleCount; i++)
		{
			Vector2 sideways = Projectile.velocity.SafeNormalize(Vector2.UnitX)
				.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-5f, 5f) * Projectile.scale;
			Vector2 flamePosition = Projectile.Center
				- Projectile.velocity * Main.rand.NextFloat(0.15f, 0.8f)
				+ sideways;
			Dust dust = Dust.NewDustPerfect(
				flamePosition,
				DustID.Torch,
				-Projectile.velocity * Main.rand.NextFloat(0.06f, 0.16f)
					+ Main.rand.NextVector2Circular(0.7f, 0.7f),
				Alpha: Main.rand.Next(25, 90),
				newColor: Color.Lerp(Color.Yellow, Color.OrangeRed, Main.rand.NextFloat()),
				Scale: Main.rand.NextFloat(0.75f, 1.25f) * Projectile.scale
			);
			dust.noGravity = true;
		}

		if (Main.rand.NextBool(2) && CultivationClientConfig.ShouldSpawnParticle())
		{
			Vector2 sparkDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
			Dust spark = Dust.NewDustPerfect(
				Projectile.Center + sparkDirection * Projectile.width * 0.35f,
				DustID.SolarFlare,
				sparkDirection * Main.rand.NextFloat(1.2f, 3.2f),
				Alpha: 35,
				newColor: Color.Gold,
				Scale: Main.rand.NextFloat(0.55f, 0.9f) * Projectile.scale
			);
			spark.noGravity = true;
		}

		if (Main.rand.NextBool(5) && CultivationClientConfig.ShouldSpawnParticle())
		{
			Dust smoke = Dust.NewDustPerfect(
				Projectile.Center - Projectile.velocity,
				DustID.Smoke,
				-Projectile.velocity * 0.04f + new Vector2(0f, -0.35f),
				Alpha: 130,
				newColor: Color.DarkGray,
				Scale: Main.rand.NextFloat(0.7f, 1.1f) * Projectile.scale
			);
			smoke.noGravity = true;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(BuffID.OnFire3, 180);
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
		int particleCount = CultivationClientConfig.ScaleParticleCount(12);
		for (int i = 0; i < particleCount; i++)
		{
			Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
				DustID.Torch, Scale: Main.rand.NextFloat(1f, 1.5f));
			dust.velocity *= 1.8f;
			dust.noGravity = true;
		}
	}
}
