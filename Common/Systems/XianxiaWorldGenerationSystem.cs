using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Xianxia.Content.Tiles;

namespace Xianxia.Common.Systems;

public class XianxiaWorldGenerationSystem : ModSystem
{
	private enum SpiritMineSize
	{
		Small,
		Medium,
		Large
	}

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		int shiniesIndex = tasks.FindIndex(pass => pass.Name == "Shinies");
		int insertionIndex = shiniesIndex >= 0 ? shiniesIndex + 1 : tasks.Count;
		tasks.Insert(insertionIndex, new PassLegacy("Xianxia Spiritual Ores", GenerateOres));
	}

	private void GenerateOres(GenerationProgress progress, GameConfiguration configuration)
	{
		progress.Message = Mod.GetLocalization("WorldGeneration.SpiritualOres").Value;

		GenerateSpiritStoneMines();
		GenerateOre(
			ModContent.TileType<SpiritJadeOreTile>(),
			clusterDivisor: 105_000,
			minY: (int)Main.rockLayer,
			maxY: Main.UnderworldLayer - 100,
			strength: 5.5,
			steps: 6
		);
		GenerateOre(
			ModContent.TileType<ProfoundIronOreTile>(),
			clusterDivisor: 145_000,
			minY: Math.Max((int)Main.rockLayer + 180, (int)(Main.maxTilesY * 0.55f)),
			maxY: Main.UnderworldLayer - 80,
			strength: 5.5,
			steps: 7
		);
	}

	private static void GenerateSpiritStoneMines()
	{
		int mineCount = Math.Max(4, Main.maxTilesX * Main.maxTilesY / 480_000);
		int generated = 0;
		int attempts = mineCount * 80;
		for (int attempt = 0; attempt < attempts && generated < mineCount; attempt++)
		{
			SpiritMineSize size = SelectSpiritMineSize(generated);
			(int minimumX, int maximumX, int minimumY, int maximumY) = size switch
			{
				SpiritMineSize.Small => (8, 13, 5, 8),
				SpiritMineSize.Medium => (14, 21, 8, 12),
				_ => (22, 31, 12, 18)
			};
			int radiusX = WorldGen.genRand.Next(minimumX, maximumX);
			int radiusY = WorldGen.genRand.Next(minimumY, maximumY);
			int centerX = WorldGen.genRand.Next(140 + radiusX, Main.maxTilesX - 140 - radiusX);
			int centerY = WorldGen.genRand.Next((int)Main.rockLayer + 35,
				Main.UnderworldLayer - 140 - radiusY);
			if (!IsSuitableSpiritMine(centerX, centerY, radiusX, radiusY))
				continue;

			CarveSpiritMine(centerX, centerY, radiusX, radiusY);
			generated++;
		}
	}

	private static SpiritMineSize SelectSpiritMineSize(int generatedMineCount)
	{
		// Guarantee that even a small world receives at least one mine of every size.
		if (generatedMineCount < 3)
			return (SpiritMineSize)generatedMineCount;

		int roll = WorldGen.genRand.Next(100);
		return roll switch
		{
			< 55 => SpiritMineSize.Small,
			< 85 => SpiritMineSize.Medium,
			_ => SpiritMineSize.Large
		};
	}

	private static bool IsSuitableSpiritMine(int centerX, int centerY, int radiusX, int radiusY)
	{
		int samples = 0;
		int naturalStone = 0;
		for (int x = centerX - radiusX; x <= centerX + radiusX; x += 2)
		{
			for (int y = centerY - radiusY; y <= centerY + radiusY; y += 2)
			{
				double normalized = Math.Pow((x - centerX) / (double)radiusX, 2)
					+ Math.Pow((y - centerY) / (double)radiusY, 2);
				if (normalized > 1d)
					continue;

				samples++;
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && IsNaturalCavernTile(tile.TileType))
					naturalStone++;
			}
		}

		return samples > 0 && naturalStone >= samples * 0.72f;
	}

	private static void CarveSpiritMine(int centerX, int centerY, int radiusX, int radiusY)
	{
		int oreType = ModContent.TileType<SpiritCrystalOreTile>();
		int clusterType = ModContent.TileType<SpiritCrystalClusterTile>();
		for (int x = centerX - radiusX - 2; x <= centerX + radiusX + 2; x++)
		{
			for (int y = centerY - radiusY - 2; y <= centerY + radiusY + 2; y++)
			{
				double normalized = Math.Pow((x - centerX) / (double)radiusX, 2)
					+ Math.Pow((y - centerY) / (double)radiusY, 2);
				double irregularity = WorldGen.genRand.NextDouble() * 0.14d - 0.07d;
				Tile tile = Main.tile[x, y];
				if (normalized <= 0.70d + irregularity)
				{
					if (tile.HasTile && IsNaturalCavernTile(tile.TileType))
					{
						tile.HasTile = false;
					}
					tile.LiquidAmount = 0;
				}
				else if (normalized <= 1.08d + irregularity
					&& (!tile.HasTile || IsNaturalCavernTile(tile.TileType)))
				{
					tile.HasTile = true;
					tile.TileType = (ushort)oreType;
					tile.IsHalfBlock = false;
					tile.Slope = SlopeType.Solid;
					tile.LiquidAmount = 0;
				}
				else if (normalized <= 1.25d + irregularity
					&& tile.HasTile
					&& IsNaturalCavernTile(tile.TileType)
					&& WorldGen.genRand.NextBool(2))
				{
					tile.TileType = (ushort)oreType;
					tile.IsHalfBlock = false;
					tile.Slope = SlopeType.Solid;
				}
			}
		}

		// Add exposed crystal formations on natural ledges inside the chamber.
		for (int x = centerX - radiusX + 2; x <= centerX + radiusX - 2; x++)
		{
			if (!WorldGen.genRand.NextBool(3))
				continue;

			for (int y = centerY - radiusY; y <= centerY + radiusY + 1; y++)
			{
				Tile empty = Main.tile[x, y];
				Tile floor = Main.tile[x, y + 1];
				if (!empty.HasTile && floor.HasTile && Main.tileSolid[floor.TileType])
				{
					WorldGen.PlaceTile(x, y, clusterType, mute: true, forced: true);
					break;
				}
			}
		}

		WorldGen.RangeFrame(centerX - radiusX - 3, centerY - radiusY - 3,
			centerX + radiusX + 3, centerY + radiusY + 3);
	}

	private static bool IsNaturalCavernTile(ushort tileType) => tileType is
		TileID.Stone or TileID.Dirt or TileID.ClayBlock or TileID.Mud
		or TileID.Silt or TileID.Slush;

	private static void GenerateOre(
		int tileType,
		int clusterDivisor,
		int minY,
		int maxY,
		double strength,
		int steps)
	{
		int clusters = Math.Max(1, Main.maxTilesX * Main.maxTilesY / clusterDivisor);
		for (int i = 0; i < clusters; i++)
		{
			int x = WorldGen.genRand.Next(100, Main.maxTilesX - 100);
			int y = WorldGen.genRand.Next(minY, maxY);
			WorldGen.OreRunner(x, y, strength, steps, (ushort)tileType);
		}
	}
}
