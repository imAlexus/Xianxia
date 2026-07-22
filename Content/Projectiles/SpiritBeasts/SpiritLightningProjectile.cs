using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles.SpiritBeasts;

public class SpiritLightningProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetDefaults()
	{
		Projectile.width = 14;
		Projectile.height = 44;
		Projectile.hostile = true;
		Projectile.friendly = false;
		Projectile.timeLeft = 100;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.extraUpdates = 1;
	}

	public override void AI()
	{
		Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.08f, -16f, 18f);
		float intensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Projectile.Center, 0.4f * intensity, 0.65f * intensity, intensity);
		if (CultivationClientConfig.ShouldSpawnParticle())
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 18f),
				DustID.Electric, -Projectile.velocity * 0.05f, 20, Color.Cyan, 1.15f);
			dust.noGravity = true;
		}
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo info) =>
		target.AddBuff(BuffID.Electrified, 180);
}
