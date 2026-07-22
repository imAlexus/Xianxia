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
		TileID.Sets.SwaysInWindBasic[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.addTile(Type);

		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
		AddMapEntry(MapColor, CreateMapEntryName());
	}

	public override void RandomUpdate(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameY != 0 || tile.TileFrameX >= 36 || !Main.rand.NextBool(3))
			return;

		short nextFrame = (short)(tile.TileFrameX + 18);
		Main.tile[i, j].TileFrameX = nextFrame;
		Main.tile[i, j + 1].TileFrameX = nextFrame;
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendTileSquare(-1, i, j, 1, 2);
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		bool mature = Main.tile[i, j].TileFrameX >= 36;
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
