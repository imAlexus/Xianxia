using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Xianxia.Content.Items.Alchemy;

namespace Xianxia.Content.Tiles;

public abstract class UpgradedAlchemyCauldronTile : ModTile
{
	protected abstract int DropItemType { get; }
	protected abstract Color MapColor { get; }
	protected abstract Color LightColor { get; }
	protected abstract int[] CraftingAdjacencies { get; }

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		TileID.Sets.DisableSmartCursor[Type] = true;

		// Style4x2 includes directional placement alternates, but these
		// cauldrons have only one 4x3 spritesheet frame.
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.Origin = new Point16(2, 2);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(
			AnchorType.SolidTile | AnchorType.SolidWithTop,
			TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);

		DustType = DustID.GemEmerald;
		HitSound = SoundID.Tink;
		AdjTiles = CraftingAdjacencies;
		AddMapEntry(MapColor, CreateMapEntryName());
		RegisterItemDrop(DropItemType);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = LightColor.R / 255f;
		g = LightColor.G / 255f;
		b = LightColor.B / 255f;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer || !Main.rand.NextBool(13))
			return;
		Dust dust = Dust.NewDustPerfect(new Vector2(i * 16 + 8, j * 16 + 3),
			DustID.MagicMirror, new Vector2(0f, -0.45f), newColor: MapColor, Scale: 0.85f);
		dust.noGravity = true;
	}
}

public class SpiritJadeCauldronTile : UpgradedAlchemyCauldronTile
{
	protected override int DropItemType => ModContent.ItemType<SpiritJadeCauldron>();
	protected override Color MapColor => new(65, 225, 170);
	protected override Color LightColor => new(12, 75, 55);
	protected override int[] CraftingAdjacencies =>
		[ModContent.TileType<AlchemyCauldronTile>(), TileID.Bottles, TileID.AlchemyTable];
}

public class ProfoundAlchemyCauldronTile : UpgradedAlchemyCauldronTile
{
	protected override int DropItemType => ModContent.ItemType<ProfoundAlchemyCauldron>();
	protected override Color MapColor => new(125, 115, 245);
	protected override Color LightColor => new(45, 30, 105);
	protected override int[] CraftingAdjacencies =>
		[ModContent.TileType<AlchemyCauldronTile>(), ModContent.TileType<SpiritJadeCauldronTile>(),
			TileID.Bottles, TileID.AlchemyTable];
}
