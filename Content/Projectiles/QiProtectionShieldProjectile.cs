using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles;

public class QiProtectionShieldProjectile : ModProjectile
{
	private const int ShieldLifetime = 24;

	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetDefaults()
	{
		Projectile.width = 64;
		Projectile.height = 80;
		Projectile.timeLeft = ShieldLifetime;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.friendly = false;
		Projectile.hostile = false;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
		{
			Projectile.Kill();
			return;
		}

		Player protectedPlayer = Main.player[Projectile.owner];
		if (!protectedPlayer.active || protectedPlayer.dead)
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = protectedPlayer.Center;
		Projectile.rotation += Projectile.ai[0] > 0.5f ? 0.055f : 0.035f;
		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Projectile.Center, 0.1f * visualIntensity,
			0.35f * visualIntensity, 0.48f * visualIntensity);

		if (Main.netMode != NetmodeID.Server)
		{
			SpawnShieldRing();
		}
	}

	private void SpawnShieldRing()
	{
		bool fullyBlocked = Projectile.ai[0] > 0.5f;
		int particleCount = CultivationClientConfig.ScaleParticleCount(fullyBlocked ? 5 : 3);
		float lifetimeProgress = 1f - Projectile.timeLeft / (float)ShieldLifetime;
		float radiusX = 27f + lifetimeProgress * 7f;
		float radiusY = 39f + lifetimeProgress * 7f;
		Color shieldColor = fullyBlocked ? new Color(110, 245, 255) : new Color(165, 110, 255);

		for (int i = 0; i < particleCount; i++)
		{
			float angle = Main.rand.NextFloat(MathHelper.TwoPi) + Projectile.rotation;
			Vector2 offset = new(MathF.Cos(angle) * radiusX, MathF.Sin(angle) * radiusY);
			Vector2 tangent = new(-MathF.Sin(angle), MathF.Cos(angle));
			Dust shieldDust = Dust.NewDustPerfect(
				Projectile.Center + offset,
				fullyBlocked ? DustID.GemDiamond : DustID.MagicMirror,
				tangent * Main.rand.NextFloat(0.5f, 1.25f),
				Alpha: 45,
				newColor: shieldColor,
				Scale: Main.rand.NextFloat(0.8f, 1.2f)
			);
			shieldDust.noGravity = true;
			shieldDust.fadeIn = 0.75f;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}
