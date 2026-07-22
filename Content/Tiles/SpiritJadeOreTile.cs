using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Tiles;

public class SpiritJadeOreTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileSpelunker[Type] = true;
		Main.tileOreFinderPriority[Type] = 380;
		DustType = DustID.GreenMoss;
		HitSound = SoundID.Tink;
		MineResist = 2f;
		MinPick = 50;
		AddMapEntry(new Color(50, 205, 125), CreateMapEntryName());
	}
}
