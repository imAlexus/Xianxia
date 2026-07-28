using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Xianxia.Content.Items.Alchemy;

namespace Xianxia.Content.Tiles;

public class AlchemyCauldronTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		TileID.Sets.DisableSmartCursor[Type] = true;

		// Style4x2 includes directional placement alternates, but this
		// cauldron has only one 4x3 spritesheet frame.
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.Origin = new Point16(2, 2);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(
			AnchorType.SolidTile | AnchorType.SolidWithTop,
			TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);

		DustType = DustID.Iron;
		HitSound = SoundID.Tink;
		AdjTiles = [TileID.Bottles, TileID.AlchemyTable];
		AddMapEntry(new Color(45, 155, 115), CreateMapEntryName());
		RegisterItemDrop(ModContent.ItemType<AlchemyCauldron>());
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = 0.05f;
		g = 0.35f;
		b = 0.38f;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer && Main.rand.NextBool(18))
		{
			Dust flame = Dust.NewDustPerfect(
				new Vector2(i * 16 + 8, j * 16 + 3),
				DustID.MagicMirror,
				new Vector2(0f, -0.35f),
				newColor: Color.Cyan,
				Scale: 0.75f
			);
			flame.noGravity = true;
		}
	}
}
