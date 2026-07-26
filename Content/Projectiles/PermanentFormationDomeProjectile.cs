using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Xianxia.Common.Config;
using Xianxia.Content.TileEntities;

namespace Xianxia.Content.Projectiles;

public class PermanentFormationDomeProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.friendly = false;
		Projectile.hostile = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		int entityId = (int)Projectile.ai[0];
		if (!TileEntity.ByID.TryGetValue(entityId, out TileEntity entity)
			|| entity is not PermanentFormationCoreEntity core
			|| !core.Active)
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = core.WorldCenter;
		Projectile.timeLeft = 2;
		Projectile.rotation += 0.0025f;
		float diameter = core.RadiusPixels * 2f;
		Projectile.width = (int)MathHelper.Clamp(diameter, 2f, 5000f);
		Projectile.height = Projectile.width;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		int entityId = (int)Projectile.ai[0];
		if (!TileEntity.ByID.TryGetValue(entityId, out TileEntity entity)
			|| entity is not PermanentFormationCoreEntity core)
			return false;

		float intensity = CultivationClientConfig.VisualEffectIntensity;
		if (intensity <= 0f)
			return false;

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 center = core.WorldCenter - Main.screenPosition
			+ new Vector2(0f, 20f);
		float time = (float)Main.GameUpdateCount / 60f;
		Color formationColor = core.FormationColor;
		Color glow = new Color(formationColor.R, formationColor.G,
			formationColor.B, 32) * intensity;
		Color middle = new Color(formationColor.R, formationColor.G,
			formationColor.B, 95) * intensity;
		Color brightBase = Color.Lerp(formationColor, Color.White, 0.42f);
		Color bright = new Color(brightBase.R, brightBase.G,
			brightBase.B, 205) * intensity;

		// Permanent territory is represented at its source instead of covering
		// the entire screen. The array sits beneath the Core like a ground seal.
		Vector2 arrayCenter = center + new Vector2(0f, 13f);
		float arrayRadiusX = 72f + MathF.Sin(time * 1.4f) * 3f;
		float arrayRadiusY = 8f;
		DrawArc(pixel, arrayCenter, arrayRadiusX, arrayRadiusY,
			0f, MathHelper.TwoPi, glow, 48, 3f);
		DrawArc(pixel, arrayCenter, arrayRadiusX, arrayRadiusY,
			0f, MathHelper.TwoPi, middle, 48, 1.35f);
		DrawArc(pixel, arrayCenter, arrayRadiusX, arrayRadiusY,
			0f, MathHelper.TwoPi, bright, 48, 0.65f);
		for (int i = 0; i < 6; i++)
		{
			float angle = Projectile.rotation * 8f
				+ MathHelper.TwoPi * i / 6f;
			Vector2 rune = EllipsePoint(arrayCenter,
				arrayRadiusX, arrayRadiusY, angle);
			DrawDiamond(pixel, rune, 3.25f, bright, 0.85f);
		}

		float innerRadiusX = arrayRadiusX * 0.58f;
		float innerRadiusY = arrayRadiusY * 0.58f;
		DrawArc(pixel, arrayCenter, innerRadiusX, innerRadiusY,
			0f, MathHelper.TwoPi, middle, 36, 0.8f);
		for (int i = 0; i < 4; i++)
		{
			float angle = -Projectile.rotation * 5f
				+ MathHelper.PiOver2 * i;
			Vector2 outer = EllipsePoint(arrayCenter,
				arrayRadiusX * 0.88f, arrayRadiusY * 0.88f, angle);
			Vector2 inner = EllipsePoint(arrayCenter,
				innerRadiusX, innerRadiusY, angle + MathHelper.PiOver4);
			DrawLine(pixel, outer, inner, middle, 0.7f);
		}
		return false;
	}

	private static void DrawArc(Texture2D pixel, Vector2 center,
		float radiusX, float radiusY, float startAngle, float endAngle,
		Color color, int segments, float width)
	{
		Vector2 previous = EllipsePoint(center, radiusX, radiusY, startAngle);
		for (int i = 1; i <= segments; i++)
		{
			float angle = MathHelper.Lerp(startAngle, endAngle,
				i / (float)segments);
			Vector2 current = EllipsePoint(center, radiusX, radiusY, angle);
			DrawLine(pixel, previous, current, color, width);
			previous = current;
		}
	}

	private static Vector2 EllipsePoint(Vector2 center,
		float radiusX, float radiusY, float angle) =>
		center + new Vector2(MathF.Cos(angle) * radiusX,
			MathF.Sin(angle) * radiusY);

	private static void DrawDiamond(Texture2D pixel, Vector2 center,
		float size, Color color, float width = 2f)
	{
		Vector2 top = center + new Vector2(0f, -size);
		Vector2 right = center + new Vector2(size, 0f);
		Vector2 bottom = center + new Vector2(0f, size);
		Vector2 left = center + new Vector2(-size, 0f);
		DrawLine(pixel, top, right, color, width);
		DrawLine(pixel, right, bottom, color, width);
		DrawLine(pixel, bottom, left, color, width);
		DrawLine(pixel, left, top, color, width);
	}

	private static void DrawLine(Texture2D pixel, Vector2 start,
		Vector2 end, Color color, float width)
	{
		Vector2 difference = end - start;
		float length = difference.Length();
		if (!float.IsFinite(length) || length <= 0.01f || length > 5000f)
			return;
		Main.spriteBatch.Draw(pixel, start, null, color,
			difference.ToRotation(), Vector2.Zero,
			new Vector2(length / pixel.Width, width / pixel.Height),
			SpriteEffects.None, 0f);
	}
}
