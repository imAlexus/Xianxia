using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Projectiles;

public sealed class PermanentFormationUpgradeEffectProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetDefaults()
	{
		Projectile.width = 4;
		Projectile.height = 4;
		Projectile.timeLeft = 45;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.friendly = false;
		Projectile.hostile = false;
		Projectile.netImportant = true;
	}

	public override void AI()
	{
		if (Main.netMode == NetmodeID.Server)
			return;
		bool tierUpgrade = Projectile.ai[0] > 0.5f;
		if (Projectile.localAI[0] == 0f)
		{
			Projectile.localAI[0] = 1f;
			SoundEngine.PlaySound(SoundID.Item29 with
			{
				Volume = tierUpgrade ? 1f : 0.72f,
				Pitch = tierUpgrade ? -0.1f : 0.2f
			}, Projectile.Center);
		}

		int interval = tierUpgrade ? 2 : 3;
		if (Projectile.timeLeft % interval != 0)
			return;
		int count = tierUpgrade ? 16 : 9;
		float progress = 1f - Projectile.timeLeft / 45f;
		float radius = MathHelper.Lerp(18f, tierUpgrade ? 125f : 75f, progress);
		for (int i = 0; i < count; i++)
		{
			float angle = MathHelper.TwoPi * i / count
				+ progress * (tierUpgrade ? 3f : 1.8f);
			Vector2 position = Projectile.Center + angle.ToRotationVector2() * radius;
			Dust dust = Dust.NewDustPerfect(position,
				i % 3 == 0 ? DustID.MagicMirror : DustID.GemSapphire,
				-angle.ToRotationVector2() * (tierUpgrade ? 2.2f : 1.4f),
				45, tierUpgrade
					? new Color(105, 255, 225)
					: new Color(95, 215, 255),
				tierUpgrade ? 1.25f : 0.95f);
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}
