using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles;

public class FlameStepProjectile : DamagingQiAbilityProjectile
{
	protected override bool AbilityTerrainDestructionEnabled =>
		CultivationServerConfig.Instance.EnableFlameStepTerrainDestruction;

	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetDefaults()
	{
		Projectile.width = 34;
		Projectile.height = 44;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 18;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 18;
	}

	public override void AI()
	{
		if (Projectile.localAI[0] == 0f)
		{
			Projectile.localAI[0] = 1f;
			Vector2 center = Projectile.Center;
			Projectile.scale = MathHelper.Clamp(Projectile.ai[0], 1f, 2f);
			Projectile.width = Math.Max(34, (int)(34f * Projectile.scale));
			Projectile.height = Math.Max(44, (int)(44f * Projectile.scale));
			Projectile.Center = center;
		}

		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Projectile.Center, visualIntensity, 0.28f * visualIntensity,
			0.04f * visualIntensity);
		int particleCount = CultivationClientConfig.ScaleParticleCount(5);
		for (int i = 0; i < particleCount; i++)
		{
			Vector2 offset = Main.rand.NextVector2Circular(Projectile.width * 0.45f, Projectile.height * 0.45f);
			Dust flame = Dust.NewDustPerfect(
				Projectile.Center + offset,
				DustID.Torch,
				-Projectile.velocity * Main.rand.NextFloat(0.08f, 0.18f)
					+ Main.rand.NextVector2Circular(1.2f, 1.2f),
				Alpha: 35,
				newColor: Color.Lerp(Color.Yellow, Color.OrangeRed, Main.rand.NextFloat()),
				Scale: Main.rand.NextFloat(1f, 1.55f) * Projectile.scale
			);
			flame.noGravity = true;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(BuffID.OnFire3, 240);
	}

	public override void OnKill(int timeLeft)
	{
		int particleCount = CultivationClientConfig.ScaleParticleCount(20);
		for (int i = 0; i < particleCount; i++)
		{
			Dust flame = Dust.NewDustDirect(
				Projectile.position,
				Projectile.width,
				Projectile.height,
				DustID.Torch,
				Scale: Main.rand.NextFloat(1f, 1.6f)
			);
			flame.velocity *= 1.8f;
			flame.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}
