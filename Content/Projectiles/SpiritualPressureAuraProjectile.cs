using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;
using Xianxia.Content.Buffs;

namespace Xianxia.Content.Projectiles;

public class SpiritualPressureAuraProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetDefaults()
	{
		Projectile.width = 720;
		Projectile.height = 720;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 75;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 30;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
		{
			Projectile.Kill();
			return;
		}

		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead)
		{
			Projectile.Kill();
			return;
		}

		float radius = MathHelper.Clamp(Projectile.ai[0], 240f, 600f);
		Vector2 center = owner.Center;
		Projectile.width = (int)(radius * 2f);
		Projectile.height = Projectile.width;
		Projectile.Center = center;
		Projectile.rotation += 0.012f;
		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(center, 0.14f * visualIntensity, 0.04f * visualIntensity,
			0.2f * visualIntensity);

		if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2)
			&& CultivationClientConfig.ShouldSpawnParticle())
		{
			float angle = Main.rand.NextFloat(MathHelper.TwoPi);
			float distance = Main.rand.NextFloat(radius * 0.25f, radius);
			Vector2 position = center + angle.ToRotationVector2() * distance;
			Dust dust = Dust.NewDustPerfect(position, DustID.Shadowflame,
				position.DirectionTo(center) * Main.rand.NextFloat(0.8f, 2.2f),
				80, new Color(190, 80, 255), Main.rand.NextFloat(0.8f, 1.25f));
			dust.noGravity = true;
		}
	}

	public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
	{
		float radius = Projectile.width * 0.5f;
		Vector2 closestPoint = Vector2.Clamp(
			Projectile.Center,
			targetHitbox.TopLeft(),
			targetHitbox.BottomRight());
		return Vector2.DistanceSquared(Projectile.Center, closestPoint) <= radius * radius;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(ModContent.BuffType<SpiritualPressureDebuff>(), 90);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 center = Projectile.Center - Main.screenPosition;
		// Keep the visual aura compact and readable. The projectile hit radius remains
		// unchanged, so enemies are still affected throughout the full gameplay area.
		float visualRadius = MathHelper.Clamp(Projectile.width * 0.22f, 105f, 220f);
		float intensity = CultivationClientConfig.VisualEffectIntensity;
		if (intensity <= 0f)
		{
			return false;
		}
		float time = (float)Main.GameUpdateCount * 0.025f;
		float pulse = 1f + MathF.Sin(time * 2.8f) * 0.035f;
		DrawDottedRing(pixel, center, visualRadius * 0.68f * pulse,
			Projectile.rotation, new Color(155, 70, 220, 95) * intensity,
			CultivationClientConfig.ScaleParticleCount(40, 8), 3, time);
		DrawDottedRing(pixel, center, visualRadius * pulse,
			-Projectile.rotation * 0.7f, new Color(210, 120, 255, 125) * intensity,
			CultivationClientConfig.ScaleParticleCount(56, 10), 4, -time);

		int moteCount = CultivationClientConfig.ScaleParticleCount(10, 2);
		for (int i = 0; i < moteCount; i++)
		{
			float angle = time * (i % 2 == 0 ? 0.8f : -0.55f)
				+ MathHelper.TwoPi * i / moteCount;
			float distance = visualRadius * (0.28f + (i % 3) * 0.16f);
			Vector2 position = center + angle.ToRotationVector2() * distance;
			int size = i % 3 == 0 ? 5 : 3;
			Main.spriteBatch.Draw(pixel,
				new Rectangle((int)position.X - size / 2, (int)position.Y - size / 2, size, size),
				new Color(225, 155, 255, 110) * intensity);
		}

		return false;
	}

	private static void DrawDottedRing(
		Texture2D pixel,
		Vector2 center,
		float radius,
		float rotation,
		Color color,
		int segments,
		int dotSize,
		float shimmerTime)
	{
		for (int i = 0; i < segments; i++)
		{
			float angle = rotation + MathHelper.TwoPi * i / segments;
			Vector2 position = center + angle.ToRotationVector2() * radius;
			float shimmer = 0.55f + 0.45f * MathF.Max(0f,
				MathF.Sin(angle * 3f + shimmerTime * 3f));
			int size = dotSize + (i % 8 == 0 ? 2 : 0);
			Main.spriteBatch.Draw(pixel,
				new Rectangle((int)position.X - size / 2, (int)position.Y - size / 2, size, size),
				color * shimmer);
		}
	}
}
