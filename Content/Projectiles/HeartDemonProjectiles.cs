using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Projectiles;

public sealed class HeartDemonBolt : ModProjectile
{
	public override string Texture =>
		$"Terraria/Images/Projectile_{ProjectileID.ShadowBeamHostile}";

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.hostile = true;
		Projectile.friendly = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = 300;
		Projectile.penetrate = 1;
	}

	public override void AI()
	{
		int owner = (int)Projectile.ai[0];
		if (owner < 0 || owner >= Main.maxPlayers
			|| !Main.player[owner].active || Main.player[owner].dead)
		{
			Projectile.Kill();
			return;
		}
		Player target = Main.player[owner];
		if (Projectile.ai[1] > 0f)
		{
			Vector2 desired = Projectile.DirectionTo(target.Center)
				* Projectile.velocity.Length();
			Projectile.velocity = Vector2.Lerp(
				Projectile.velocity, desired, 0.018f);
		}
		Projectile.rotation = Projectile.velocity.ToRotation();
		Lighting.AddLight(Projectile.Center, 0.28f, 0.04f, 0.34f);
		if (Main.rand.NextBool(2))
		{
			Dust dust = Dust.NewDustDirect(Projectile.position,
				Projectile.width, Projectile.height, DustID.Shadowflame,
				-Projectile.velocity.X * 0.08f,
				-Projectile.velocity.Y * 0.08f, 60,
				Color.MediumPurple, 1f);
			dust.noGravity = true;
		}
	}

	public override bool CanHitPlayer(Player target) =>
		target.whoAmI == (int)Projectile.ai[0];
}
