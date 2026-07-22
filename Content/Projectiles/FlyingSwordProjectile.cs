using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles;

public class FlyingSwordProjectile : ModProjectile
{
	private const float TargetSearchRange = 960f;
	private const float HomingSpeed = 15.5f;
	private const float HomingStrength = 0.1f;
	private const int OutboundDuration = 50;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.width = 38;
		Projectile.height = 14;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 4;
		Projectile.timeLeft = 150;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 14;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead)
		{
			Projectile.Kill();
			return;
		}

		Projectile.rotation = Projectile.velocity.ToRotation();
		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Projectile.Center, 0.15f * visualIntensity,
			0.6f * visualIntensity, 0.7f * visualIntensity);
		if (Projectile.ai[0] == 0f)
		{
			Projectile.ai[1]++;
			int targetIndex = Projectile.FindTargetWithLineOfSight(TargetSearchRange);
			if (targetIndex >= 0)
			{
				NPC target = Main.npc[targetIndex];
				Vector2 desiredVelocity = Projectile.DirectionTo(target.Center) * HomingSpeed;
				Projectile.velocity = Vector2.Lerp(
					Projectile.velocity,
					desiredVelocity,
					HomingStrength
				);
			}
			else
			{
				Projectile.velocity *= 0.992f;
			}

			if (Projectile.ai[1] >= OutboundDuration)
			{
				Projectile.ai[0] = 1f;
				Projectile.netUpdate = true;
			}
		}
		else
		{
			Vector2 toOwner = owner.Center - Projectile.Center;
			if (toOwner.LengthSquared() < 32f * 32f)
			{
				Projectile.Kill();
				return;
			}

			Vector2 desiredVelocity = Vector2.Normalize(toOwner) * 17f;
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.12f);
		}

		if (Main.rand.NextBool(2) && CultivationClientConfig.ShouldSpawnParticle())
		{
			Dust dust = Dust.NewDustPerfect(
				Projectile.Center - Projectile.velocity * 0.7f,
				DustID.MagicMirror,
				-Projectile.velocity * 0.08f,
				newColor: Color.Cyan,
				Scale: 0.8f
			);
			dust.noGravity = true;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
		Projectile.ai[0] = 1f;
		Projectile.netUpdate = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Vector2 origin = texture.Size() * 0.5f;
		for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
		{
			float strength = 1f - i / (float)Projectile.oldPos.Length;
			Color color = Color.Cyan * strength * 0.35f
				* CultivationClientConfig.VisualEffectIntensity;
			Main.EntitySpriteDraw(
				texture,
				Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
				null,
				color,
				Projectile.oldRot[i],
				origin,
				Projectile.scale,
				SpriteEffects.None
			);
		}

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
			lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
		return false;
	}
}
