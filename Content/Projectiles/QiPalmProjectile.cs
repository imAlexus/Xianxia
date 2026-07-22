using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles;

public class QiPalmProjectile : DamagingQiAbilityProjectile
{
	protected override bool AbilityTerrainDestructionEnabled =>
		CultivationServerConfig.Instance.EnableQiPalmTerrainDestruction;

	public override void SetDefaults()
	{
		Projectile.width = 24;
		Projectile.height = 24;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 3;
		Projectile.timeLeft = 42;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 15;
	}

	public override void AI()
	{
		if (Projectile.localAI[0] == 0f)
		{
			Projectile.localAI[0] = 1f;
			Vector2 center = Projectile.Center;
			Projectile.scale = MathHelper.Clamp(Projectile.ai[0], 1f, 2.2f);
			Projectile.width = Math.Max(24, (int)(24f * Projectile.scale));
			Projectile.height = Math.Max(24, (int)(24f * Projectile.scale));
			Projectile.Center = center;
		}

		Projectile.velocity *= 0.985f;
		Projectile.rotation = Projectile.velocity.ToRotation();
		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Projectile.Center, 0.1f * visualIntensity,
			0.65f * visualIntensity, 0.7f * visualIntensity);

		int particleCount = CultivationClientConfig.ScaleParticleCount(2 + (int)Projectile.scale);
		for (int i = 0; i < particleCount; i++)
		{
			Vector2 offset = Main.rand.NextVector2Circular(Projectile.width * 0.45f, Projectile.height * 0.45f);
			Dust qiDust = Dust.NewDustPerfect(
				Projectile.Center + offset,
				DustID.MagicMirror,
				-Projectile.velocity * 0.06f + Main.rand.NextVector2Circular(0.7f, 0.7f),
				Alpha: 45,
				newColor: Color.Lerp(Color.Cyan, Color.White, Main.rand.NextFloat(0.25f, 0.7f)),
				Scale: Main.rand.NextFloat(0.85f, 1.25f) * Projectile.scale
			);
			qiDust.noGravity = true;
		}
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
		int particleCount = CultivationClientConfig.ScaleParticleCount(18);
		for (int i = 0; i < particleCount; i++)
		{
			Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
			Dust dust = Dust.NewDustPerfect(
				Projectile.Center,
				DustID.MagicMirror,
				direction * Main.rand.NextFloat(2f, 5f),
				Alpha: 30,
				newColor: Color.Cyan,
				Scale: Main.rand.NextFloat(0.9f, 1.4f) * Projectile.scale
			);
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Vector2 origin = texture.Size() * 0.5f;
		Vector2 drawPosition = Projectile.Center - Main.screenPosition;
		float visualScale = Projectile.scale * 0.78f;
		float pulse = 1f + MathF.Sin((float)Main.GameUpdateCount * 0.18f) * 0.05f;

		Main.EntitySpriteDraw(
			texture,
			drawPosition,
			null,
			new Color(30, 125, 255, 0) * 0.3f,
			Projectile.rotation,
			origin,
			visualScale * 1.2f * pulse,
			SpriteEffects.None
		);
		Main.EntitySpriteDraw(
			texture,
			drawPosition,
			null,
			Color.White,
			Projectile.rotation,
			origin,
			visualScale,
			SpriteEffects.None
		);
		return false;
	}
}
