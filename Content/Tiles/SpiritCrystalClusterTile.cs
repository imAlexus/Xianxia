using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Xianxia.Content.Items;

namespace Xianxia.Content.Tiles;

public class SpiritCrystalClusterTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		Main.tileLighted[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.addTile(Type);

		DustType = DustID.PurpleCrystalShard;
		HitSound = SoundID.Shatter;
		MineResist = 1.5f;
		MinPick = 55;
		AddMapEntry(new Color(125, 115, 245), CreateMapEntryName());
		RegisterItemDrop(ModContent.ItemType<SpiritStone>());
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = 0.08f;
		g = 0.18f;
		b = 0.32f;
	}
}
