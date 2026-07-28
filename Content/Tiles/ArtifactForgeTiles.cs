using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Xianxia.Content.Items.Artifacts;

namespace Xianxia.Content.Tiles;

public abstract class ArtifactForgeTileBase : ModTile
{
	protected abstract int DropItem { get; }
	protected abstract Color MapColor { get; }
	protected abstract Color LightColor { get; }

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		TileID.Sets.DisableSmartCursor[Type] = true;

		// Style4x2 has left/right placement alternates. These forges use a
		// single 4x3 spritesheet, so an alternate would point outside it.
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
		AdjTiles = [TileID.Anvils, TileID.MythrilAnvil];
		AddMapEntry(MapColor, CreateMapEntryName());
		RegisterItemDrop(DropItem);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = LightColor.R / 255f * 0.55f;
		g = LightColor.G / 255f * 0.55f;
		b = LightColor.B / 255f * 0.55f;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer || !Main.rand.NextBool(14))
			return;
		Dust dust = Dust.NewDustPerfect(new Vector2(i * 16 + 8, j * 16 + 2),
			DustID.GemEmerald, new Vector2(0f, -0.4f), newColor: LightColor,
			Scale: 0.8f);
		dust.noGravity = true;
	}
}

public class ArtifactForgeTile : ArtifactForgeTileBase
{
	protected override int DropItem => ModContent.ItemType<ArtifactForge>();
	protected override Color MapColor => new(80, 150, 145);
	protected override Color LightColor => Color.Cyan;
}

public class SpiritJadeArtifactForgeTile : ArtifactForgeTileBase
{
	protected override int DropItem => ModContent.ItemType<SpiritJadeArtifactForge>();
	protected override Color MapColor => new(65, 190, 125);
	protected override Color LightColor => new(65, 255, 180);
}

public class ProfoundArtifactForgeTile : ArtifactForgeTileBase
{
	protected override int DropItem => ModContent.ItemType<ProfoundArtifactForge>();
	protected override Color MapColor => new(135, 75, 190);
	protected override Color LightColor => new(180, 80, 255);
}
