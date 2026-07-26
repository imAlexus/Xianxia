using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Xianxia.Content.Tiles;

namespace Xianxia.Common.Utilities;

public static class SpiritualQiConcentration
{
	public const int CrystalsPerLevel = 50;
	public const int MaximumLevel = 10;
	public const int FormationQiPerLevelPerSecond = 5;

	public static int GetLevel(int crystalCount) =>
		Math.Clamp((Math.Max(0, crystalCount) + CrystalsPerLevel - 1)
			/ CrystalsPerLevel, 0, MaximumLevel);

	public static int GetFormationQiPerSecond(int crystalCount) =>
		GetLevel(crystalCount) * FormationQiPerLevelPerSecond;

	public static int CountCrystals(Vector2 worldCenter, int radiusBlocks)
	{
		Point center = worldCenter.ToTileCoordinates();
		int radius = Math.Max(1, radiusBlocks);
		int radiusSquared = radius * radius;
		int minX = Math.Max(1, center.X - radius);
		int maxX = Math.Min(Main.maxTilesX - 2, center.X + radius);
		int minY = Math.Max(1, center.Y - radius);
		int maxY = Math.Min(Main.maxTilesY - 2, center.Y + radius);
		int spiritCrystalType = ModContent.TileType<SpiritCrystalOreTile>();
		int crystalCount = 0;

		for (int x = minX; x <= maxX; x++)
		{
			int offsetX = x - center.X;
			for (int y = minY; y <= maxY; y++)
			{
				int offsetY = y - center.Y;
				if (offsetX * offsetX + offsetY * offsetY > radiusSquared)
					continue;
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && tile.TileType == spiritCrystalType)
					crystalCount++;
			}
		}
		return crystalCount;
	}
}
