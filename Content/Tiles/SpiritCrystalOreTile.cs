using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items;

namespace Xianxia.Content.Tiles;

public class SpiritCrystalOreTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileSpelunker[Type] = true;
		Main.tileOreFinderPriority[Type] = 420;
		DustType = DustID.PurpleCrystalShard;
		HitSound = SoundID.Tink;
		MineResist = 2.5f;
		MinPick = 55;
		AddMapEntry(new Color(105, 105, 235), CreateMapEntryName());
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = 0.035f;
		g = 0.09f;
		b = 0.16f;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ModContent.ItemType<SpiritStone>(), Main.rand.Next(1, 3));
	}
}
