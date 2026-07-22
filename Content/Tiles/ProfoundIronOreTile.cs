using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Tiles;

public class ProfoundIronOreTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileSpelunker[Type] = true;
		Main.tileOreFinderPriority[Type] = 440;
		DustType = DustID.Iron;
		HitSound = SoundID.Tink;
		MineResist = 3f;
		MinPick = 65;
		AddMapEntry(new Color(65, 80, 115), CreateMapEntryName());
	}
}
