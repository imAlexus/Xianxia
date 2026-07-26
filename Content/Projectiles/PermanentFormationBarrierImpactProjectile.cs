using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.TileEntities;

namespace Xianxia.Content.Projectiles;

public sealed class PermanentFormationBarrierImpactProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetDefaults()
	{
		Projectile.width = 4;
		Projectile.height = 4;
		Projectile.timeLeft = 12;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.friendly = false;
		Projectile.hostile = false;
		Projectile.netImportant = true;
	}

	public override void AI()
	{
		if (Main.netMode == NetmodeID.Server || Projectile.localAI[0] != 0f)
			return;
		Projectile.localAI[0] = 1f;
		Color color = GetColor((PermanentFormationKind)(int)Projectile.ai[0]);
		for (int i = 0; i < 18; i++)
		{
			Vector2 velocity = Main.rand.NextVector2Circular(3.8f, 3.8f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center,
				i % 3 == 0 ? DustID.GemSapphire : DustID.MagicMirror,
				velocity, 40, color, Main.rand.NextFloat(0.8f, 1.3f));
			dust.noGravity = true;
		}
		SoundEngine.PlaySound(SoundID.NPCHit53 with
		{
			Volume = 0.55f,
			Pitch = 0.2f
		}, Projectile.Center);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		return false;
	}

	private static Color GetColor(PermanentFormationKind kind)
	{
		return kind switch
		{
			PermanentFormationKind.SpiritGathering => new Color(100, 255, 145),
			PermanentFormationKind.Suppression => new Color(195, 105, 255),
			PermanentFormationKind.Restoration => new Color(255, 210, 95),
			_ => new Color(80, 235, 225)
		};
	}
}
