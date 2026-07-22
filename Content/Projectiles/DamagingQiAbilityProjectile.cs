using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Xianxia.Common.Config;

namespace Xianxia.Content.Projectiles;

/// <summary>
/// Base for offensive Qi abilities. Inheriting from this class makes an ability
/// destroy explosion-compatible terrain when its projectile hits a tile.
/// </summary>
public abstract class DamagingQiAbilityProjectile : ModProjectile
{
	protected abstract bool AbilityTerrainDestructionEnabled { get; }

	protected virtual int TerrainDestructionRadius => Math.Clamp(
		(int)MathF.Ceiling(Projectile.scale + Projectile.damage / 100f),
		1,
		4
	);

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		// Player-owned projectiles are simulated by their owning client. Gating the
		// terrain edit prevents every multiplayer client from breaking the same tiles.
		CultivationServerConfig config = CultivationServerConfig.Instance;
		if (Projectile.owner == Main.myPlayer
			&& config.EnableAbilityTerrainDestruction
			&& AbilityTerrainDestructionEnabled)
		{
			BreakTerrainAtImpact();
		}

		return true;
	}

	private void BreakTerrainAtImpact()
	{
		int radius = TerrainDestructionRadius;
		Point centerTile = Projectile.Center.ToTileCoordinates();
		int minTileX = Math.Clamp(centerTile.X - radius, 1, Main.maxTilesX - 2);
		int maxTileX = Math.Clamp(centerTile.X + radius, 1, Main.maxTilesX - 2);
		int minTileY = Math.Clamp(centerTile.Y - radius, 1, Main.maxTilesY - 2);
		int maxTileY = Math.Clamp(centerTile.Y + radius, 1, Main.maxTilesY - 2);

		Projectile.ExplodeTiles(
			Projectile.Center,
			radius,
			minTileX,
			maxTileX,
			minTileY,
			maxTileY,
			wallSplode: false
		);
	}
}
