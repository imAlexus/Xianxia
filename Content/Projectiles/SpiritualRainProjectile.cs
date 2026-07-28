using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Projectiles;

public class SpiritualRainProjectile : ModProjectile
{
	private const float DefaultRadiusInTiles = 12f;
	private float RadiusInTiles =>
		Projectile.ai[0] > 0f ? Projectile.ai[0] : DefaultRadiusInTiles;
	private float RadiusInPixels => RadiusInTiles * 16f;

	public override string Texture => "Xianxia/Content/Items/Alchemy/MoonDewFlower";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = 180;
		Projectile.netImportant = true;
	}

	public override void AI()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.localAI[0] == 0f)
		{
			Projectile.localAI[0] = 1f;
			AdvanceNearbyPlants();
		}

		if (Main.netMode != NetmodeID.Server)
			SpawnRainDust();

		Lighting.AddLight(Projectile.Center, 0.08f, 0.24f, 0.25f);
	}

	private void AdvanceNearbyPlants()
	{
		Point center = Projectile.Center.ToTileCoordinates();
		HashSet<Point> processedPlants = [];
		int scanRadius = (int)MathF.Ceiling(RadiusInTiles);
		int minimumX = Math.Max(1, center.X - scanRadius);
		int maximumX = Math.Min(Main.maxTilesX - 2, center.X + scanRadius);
		int minimumY = Math.Max(1, center.Y - scanRadius);
		int maximumY = Math.Min(Main.maxTilesY - 3, center.Y + scanRadius);

		for (int x = minimumX; x <= maximumX; x++)
		{
			for (int y = minimumY; y <= maximumY; y++)
			{
				if (Vector2.DistanceSquared(new Vector2(x, y), center.ToVector2())
					> RadiusInTiles * RadiusInTiles)
					continue;

				Tile tile = Main.tile[x, y];
				if (!tile.HasTile
					|| TileLoader.GetTile(tile.TileType) is not SpiritualHerbTile)
					continue;

				int left = x - tile.TileFrameX % 36 / 18;
				int top = y - tile.TileFrameY / 18;
				Point origin = new(left, top);
				if (processedPlants.Add(origin))
					SpiritualHerbTile.TryAdvanceGrowth(left, top);
			}
		}
	}

	private void SpawnRainDust()
	{
		for (int i = 0; i < 3; i++)
		{
			float horizontal = Main.rand.NextFloat(-RadiusInPixels, RadiusInPixels);
			float vertical = Main.rand.NextFloat(-RadiusInPixels, RadiusInPixels);
			if (horizontal * horizontal + vertical * vertical
				> RadiusInPixels * RadiusInPixels)
				continue;

			Vector2 spawn = Projectile.Center
				+ new Vector2(horizontal, vertical - 80f);
			Dust rain = Dust.NewDustPerfect(spawn, DustID.Water,
				new Vector2(Main.rand.NextFloat(-0.25f, 0.25f),
					Main.rand.NextFloat(5.5f, 8f)),
				80, new Color(90, 225, 255), Main.rand.NextFloat(0.8f, 1.15f));
			rain.noGravity = true;

			if (Main.rand.NextBool(5))
			{
				Dust qi = Dust.NewDustPerfect(spawn, DustID.GemSapphire,
					new Vector2(0f, Main.rand.NextFloat(2f, 4f)),
					110, new Color(125, 255, 215), 0.65f);
				qi.noGravity = true;
			}
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}
