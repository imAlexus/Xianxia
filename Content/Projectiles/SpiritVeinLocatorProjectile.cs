using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles;

public class SpiritVeinLocatorProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.timeLeft = 600;
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

		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead)
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = owner.Center;
		Vector2 target = new(Projectile.ai[0], Projectile.ai[1]);
		Vector2 direction = owner.Center.DirectionTo(target);
		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(owner.Center + direction * 24f, 0.08f * visualIntensity,
			0.22f * visualIntensity, 0.3f * visualIntensity);

		if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(4)
			&& CultivationClientConfig.ShouldSpawnParticle())
		{
			Dust dust = Dust.NewDustPerfect(owner.Center + direction * 32f,
				DustID.PurpleCrystalShard, direction * 1.5f, 80, new Color(125, 235, 255), 0.8f);
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Player owner = Main.player[Projectile.owner];
		Vector2 target = new(Projectile.ai[0], Projectile.ai[1]);
		Vector2 direction = owner.Center.DirectionTo(target);
		Vector2 normal = new(-direction.Y, direction.X);
		Vector2 center = owner.Top - Main.screenPosition + new Vector2(0f, -55f);
		float pulse = 1f + MathF.Sin((float)Main.GameUpdateCount * 0.12f) * 0.08f;
		Color color = new Color(95, 235, 255) * 0.95f;
		Texture2D pixel = TextureAssets.MagicPixel.Value;

		Vector2 tip = center + direction * (27f * pulse);
		Vector2 tail = center - direction * 13f;
		DrawLine(pixel, tail, tip, color, 5f);
		DrawLine(pixel, tip, tip - direction * 12f + normal * 9f, color, 5f);
		DrawLine(pixel, tip, tip - direction * 12f - normal * 9f, color, 5f);

		int blocks = (int)MathF.Round(Vector2.Distance(owner.Center, target) / 16f);
		string distanceText = Mod.GetLocalization("Items.SpiritVeinCompass.Distance").Format(blocks);
		Utils.DrawBorderString(Main.spriteBatch, distanceText, center + new Vector2(0f, 29f),
			Color.White, 0.75f, 0.5f, 0.5f);
		return false;
	}

	private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
	{
		Vector2 difference = end - start;
		Main.spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), color, difference.ToRotation(),
			new Vector2(0f, 0.5f), new Vector2(difference.Length(), width),
			SpriteEffects.None, 0f);
	}
}
