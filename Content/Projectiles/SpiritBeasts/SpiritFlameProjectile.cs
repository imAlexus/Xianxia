using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles.SpiritBeasts;

public class SpiritFlameProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/Projectile_15";

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.hostile = true;
		Projectile.friendly = false;
		Projectile.timeLeft = 180;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = false;
	}

	public override void AI()
	{
		Projectile.rotation += 0.16f;
		float intensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Projectile.Center, intensity, 0.22f * intensity, 0.03f);
		if (CultivationClientConfig.ShouldSpawnParticle())
		{
			Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
				DustID.Torch, -Projectile.velocity.X * 0.08f, -Projectile.velocity.Y * 0.08f,
				60, Color.OrangeRed, 1.1f);
			dust.noGravity = true;
		}
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo info) =>
		target.AddBuff(BuffID.OnFire3, 240);
}
