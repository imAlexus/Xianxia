using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Abilities;
using Xianxia.Common.Config;
using Xianxia.Common.Players;
using Xianxia.Content.Buffs;

namespace Xianxia.Content.Projectiles;

public class SectProtectionFormationProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetDefaults()
	{
		Projectile.width = 600;
		Projectile.height = 300;
		Projectile.friendly = false;
		Projectile.hostile = false;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = 2;
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
		if (!owner.active || owner.dead
			|| !owner.HasBuff<SectProtectionFormationBuff>())
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = owner.Center;
		Projectile.timeLeft = 2;
		Projectile.rotation += 0.008f;

		float intensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(owner.Center, 0.08f * intensity,
			0.38f * intensity, 0.31f * intensity);

		if (Main.netMode != NetmodeID.Server
			&& Main.GameUpdateCount % 5 == 0
			&& CultivationClientConfig.ShouldSpawnParticle())
		{
			int level = owner.GetModPlayer<CultivationPlayer>()
				.GetAbilityLevel(CultivationAbility.SectProtectionFormation);
			float radiusX = owner.GetModPlayer<SectPlayer>().GetFormationRadius();
			float angle = Main.rand.NextFloat(MathHelper.TwoPi);
			Vector2 position = owner.Bottom
				+ new Vector2(MathF.Cos(angle) * radiusX, MathF.Sin(angle) * 42f);
			Dust dust = Dust.NewDustPerfect(position, DustID.GemEmerald,
				new Vector2(0f, Main.rand.NextFloat(-1.5f, -0.5f)),
				50, new Color(90, 255, 210), Main.rand.NextFloat(0.8f, 1.25f));
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float intensity = CultivationClientConfig.VisualEffectIntensity;
		if (intensity <= 0f)
			return false;

		Player owner = Main.player[Projectile.owner];
		int level = owner.GetModPlayer<CultivationPlayer>()
			.GetAbilityLevel(CultivationAbility.SectProtectionFormation);
		float supportScale = owner.GetModPlayer<SectPlayer>().FormationVisualScale;
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 center = owner.Bottom - Main.screenPosition + new Vector2(0f, 2f);
		float time = (float)Main.GameUpdateCount / 60f;
		float pulse = 1f + MathF.Sin(time * 3.2f) * 0.025f;
		float radiusX = MathHelper.Clamp(195f + level * 4.5f, 195f, 285f)
			* supportScale * pulse;
		float radiusY = MathHelper.Clamp(60f + level * 1.05f, 60f, 81f)
			* supportScale * pulse;
		Color bright = new Color(105, 255, 220, 210) * intensity;
		Color medium = new Color(45, 210, 180, 145) * intensity;
		Color faint = new Color(30, 155, 135, 70) * intensity;

		// Large ritual array on the ground.
		DrawEllipse(pixel, center, radiusX, radiusY, Projectile.rotation,
			bright, 72, 2.2f);
		DrawEllipse(pixel, center, radiusX - 7f, radiusY - 3f,
			-Projectile.rotation * 0.65f, faint, 72, 1.5f);
		DrawEllipse(pixel, center, radiusX * 0.58f, radiusY * 0.58f,
			-Projectile.rotation * 1.15f, medium, 54, 1.8f);

		// Two opposing triangles form the central six-point defensive seal.
		DrawPolygon(pixel, center, radiusX * 0.78f, radiusY * 0.78f,
			3, Projectile.rotation * 0.7f - MathHelper.PiOver2, medium, 2f);
		DrawPolygon(pixel, center, radiusX * 0.78f, radiusY * 0.78f,
			3, -Projectile.rotation * 0.7f + MathHelper.PiOver2, medium, 2f);

		// Twelve rotating nodes and their inward channels make the formation readable.
		for (int i = 0; i < 12; i++)
		{
			float angle = Projectile.rotation + MathHelper.TwoPi * i / 12f;
			Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
			Vector2 node = center + new Vector2(direction.X * radiusX, direction.Y * radiusY);
			Vector2 inner = center + new Vector2(
				direction.X * radiusX * 0.58f,
				direction.Y * radiusY * 0.58f);
			DrawLine(pixel, inner, node, faint, i % 3 == 0 ? 2f : 1f);
			DrawDiamond(pixel, node, i % 3 == 0 ? 7f : 4f,
				i % 3 == 0 ? bright : medium);
		}

		// A projected hemispherical grid makes the ward read as a 3D dome.
		// It uses the same turquoise Qi palette as the ground formation.
		float domePulse = 1f + MathF.Sin(time * 2.4f) * 0.018f;
		float domeRadiusX = radiusX * domePulse;
		float domeHeight = radiusX * 0.72f * domePulse;
		float domeDepth = radiusY * 0.92f;
		DrawArc(pixel, center, domeRadiusX, domeHeight,
			MathHelper.Pi, MathHelper.TwoPi, bright, 84, 2.6f);

		Color domeGrid = new Color(75, 235, 205, 105) * intensity;
		Color domeGridFaint = new Color(65, 215, 190, 72) * intensity;
		for (int i = 1; i <= 3; i++)
		{
			float elevation = MathHelper.PiOver2 * i / 4f;
			float horizontalScale = MathF.Cos(elevation);
			Vector2 latitudeCenter = center
				- new Vector2(0f, domeHeight * MathF.Sin(elevation));
			DrawEllipse(pixel, latitudeCenter,
				domeRadiusX * horizontalScale,
				domeDepth * horizontalScale,
				Projectile.rotation * (i % 2 == 0 ? -0.25f : 0.2f),
				i == 2 ? domeGrid : domeGridFaint, 58, 1.25f);
		}

		for (int i = 0; i < 8; i++)
		{
			float baseAngle = MathHelper.TwoPi * i / 8f
				+ Projectile.rotation * 0.35f;
			DrawDomeRib(pixel, center, domeRadiusX, domeDepth, domeHeight,
				baseAngle, i % 2 == 0 ? domeGrid : domeGridFaint, 1.35f);
		}

		return false;
	}

	private static void DrawEllipse(Texture2D pixel, Vector2 center,
		float radiusX, float radiusY, float rotation, Color color,
		int segments, float width)
	{
		Vector2 previous = EllipsePoint(center, radiusX, radiusY, rotation);
		for (int i = 1; i <= segments; i++)
		{
			float angle = MathHelper.TwoPi * i / segments + rotation;
			Vector2 current = EllipsePoint(center, radiusX, radiusY, angle);
			DrawLine(pixel, previous, current, color, width);
			previous = current;
		}
	}

	private static Vector2 EllipsePoint(Vector2 center, float radiusX,
		float radiusY, float angle) =>
		center + new Vector2(MathF.Cos(angle) * radiusX, MathF.Sin(angle) * radiusY);

	private static void DrawPolygon(Texture2D pixel, Vector2 center,
		float radiusX, float radiusY, int sides, float rotation,
		Color color, float width)
	{
		Vector2 previous = EllipsePoint(center, radiusX, radiusY, rotation);
		for (int i = 1; i <= sides; i++)
		{
			float angle = rotation + MathHelper.TwoPi * i / sides;
			Vector2 current = EllipsePoint(center, radiusX, radiusY, angle);
			DrawLine(pixel, previous, current, color, width);
			previous = current;
		}
	}

	private static void DrawArc(Texture2D pixel, Vector2 center,
		float radiusX, float radiusY, float startAngle, float endAngle,
		Color color, int segments, float width)
	{
		Vector2 previous = EllipsePoint(center, radiusX, radiusY, startAngle);
		for (int i = 1; i <= segments; i++)
		{
			float progress = i / (float)segments;
			float angle = MathHelper.Lerp(startAngle, endAngle, progress);
			Vector2 current = EllipsePoint(center, radiusX, radiusY, angle);
			DrawLine(pixel, previous, current, color, width);
			previous = current;
		}
	}

	private static void DrawDomeRib(Texture2D pixel, Vector2 center,
		float radiusX, float groundDepth, float domeHeight,
		float baseAngle, Color color, float width)
	{
		const int segments = 28;
		Vector2 previous = center + new Vector2(
			MathF.Cos(baseAngle) * radiusX,
			MathF.Sin(baseAngle) * groundDepth);
		for (int i = 1; i <= segments; i++)
		{
			float elevation = MathHelper.PiOver2 * i / segments;
			float horizontalScale = MathF.Cos(elevation);
			Vector2 current = center + new Vector2(
				MathF.Cos(baseAngle) * radiusX * horizontalScale,
				MathF.Sin(baseAngle) * groundDepth * horizontalScale
					- domeHeight * MathF.Sin(elevation));
			DrawLine(pixel, previous, current, color, width);
			previous = current;
		}
	}

	private static void DrawDiamond(Texture2D pixel, Vector2 center,
		float size, Color color)
	{
		Vector2 top = center + new Vector2(0f, -size);
		Vector2 right = center + new Vector2(size, 0f);
		Vector2 bottom = center + new Vector2(0f, size);
		Vector2 left = center + new Vector2(-size, 0f);
		DrawLine(pixel, top, right, color, 2f);
		DrawLine(pixel, right, bottom, color, 2f);
		DrawLine(pixel, bottom, left, color, 2f);
		DrawLine(pixel, left, top, color, 2f);
	}

	private static void DrawLine(Texture2D pixel, Vector2 start,
		Vector2 end, Color color, float width)
	{
		Vector2 difference = end - start;
		float length = difference.Length();
		if (!float.IsFinite(length) || length <= 0.01f || length > 700f)
			return;
		Main.spriteBatch.Draw(pixel, start, null, color,
			difference.ToRotation(), Vector2.Zero,
			new Vector2(length / pixel.Width, width / pixel.Height),
			SpriteEffects.None, 0f);
	}
}
