using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Xianxia.Content.Items.Alchemy;

namespace Xianxia.Content.Tiles;

public abstract class SpiritualHerbTile : ModTile
{
	protected abstract int HerbItemType { get; }
	protected abstract int SeedItemType { get; }
	protected abstract Color MapColor { get; }

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;
		TileID.Sets.ReplaceTileBreakUp[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.Origin = new Point16(0, 2);
		TileObjectData.addTile(Type);

		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
		AddMapEntry(MapColor, CreateMapEntryName());
	}

	public override void RandomUpdate(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameY != 0 || tile.TileFrameX % 36 != 0
			|| tile.TileFrameX >= 108 || !Main.rand.NextBool(3))
			return;

		TryAdvanceGrowth(i, j);
	}

	public static bool TryAdvanceGrowth(int i, int j)
	{
		if (!WorldGen.InWorld(i, j, 2))
			return false;

		Tile tile = Main.tile[i, j];
		if (!tile.HasTile || TileLoader.GetTile(tile.TileType) is not SpiritualHerbTile
			|| tile.TileFrameY != 0 || tile.TileFrameX % 36 != 0
			|| tile.TileFrameX >= 108)
			return false;

		ushort tileType = tile.TileType;
		for (int x = 0; x < 2; x++)
		{
			for (int y = 0; y < 3; y++)
			{
				Tile part = Main.tile[i + x, j + y];
				if (!part.HasTile || part.TileType != tileType)
					return false;
			}
		}

		for (int x = 0; x < 2; x++)
		{
			for (int y = 0; y < 3; y++)
				Main.tile[i + x, j + y].TileFrameX += 36;
		}

		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendTileSquare(-1, i, j, 2, 3);
		return true;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		bool mature = Main.tile[i, j].TileFrameX >= 108;
		if (mature)
		{
			yield return new Item(HerbItemType, Main.rand.Next(2, 5));
			yield return new Item(SeedItemType, Main.rand.Next(1, 4));
		}
		else
		{
			yield return new Item(SeedItemType);
		}
	}
}

public class SpiritGrassTile : SpiritualHerbTile
{
	protected override int HerbItemType => ModContent.ItemType<SpiritGrass>();
	protected override int SeedItemType => ModContent.ItemType<SpiritGrassSeed>();
	protected override Color MapColor => new(75, 220, 135);
}

public class FireLotusTile : SpiritualHerbTile
{
	protected override int HerbItemType => ModContent.ItemType<FireLotus>();
	protected override int SeedItemType => ModContent.ItemType<FireLotusSeed>();
	protected override Color MapColor => new(245, 95, 45);
}

public class MoonDewFlowerTile : SpiritualHerbTile
{
	protected override int HerbItemType => ModContent.ItemType<MoonDewFlower>();
	protected override int SeedItemType => ModContent.ItemType<MoonDewFlowerSeed>();
	protected override Color MapColor => new(125, 175, 255);
}

public class IronrootTile : SpiritualHerbTile
{
	protected override int HerbItemType => ModContent.ItemType<Ironroot>();
	protected override int SeedItemType => ModContent.ItemType<IronrootSeed>();
	protected override Color MapColor => new(155, 175, 185);
}
