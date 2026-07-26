using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Xianxia.Content.Items.Formations;
using Xianxia.Content.TileEntities;
using Xianxia.Common.Utilities;
using Xianxia.Common.Systems;

namespace Xianxia.Content.Tiles;

public sealed class FormationRelayFlagTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.Origin = new Point16(0, 3);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16];
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(
			ModContent.GetInstance<FormationRelayFlagEntity>().Hook_AfterPlacement,
			-1, 0, true);
		TileObjectData.addTile(Type);
		DustType = DustID.GemSapphire;
		HitSound = SoundID.Tink;
		AddMapEntry(new Color(65, 205, 225), CreateMapEntryName());
		RegisterItemDrop(ModContent.ItemType<FormationRelayFlag>());
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		ModContent.GetInstance<FormationRelayFlagEntity>().Kill(i, j);
	}

	public override bool RightClick(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		int left = i - tile.TileFrameX / 18 % 2;
		int top = j - tile.TileFrameY / 18 % 4;
		if (TileEntity.ByPosition.TryGetValue(new Point16(left, top),
			out TileEntity entity)
			&& entity is FormationRelayFlagEntity flag)
		{
			FormationRelayUISystem.Open(flag.ID);
			return true;
		}
		return false;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameX != 0 || tile.TileFrameY != 0)
			return;
		Point16 position = new(i, j);
		if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity entity)
			|| entity is not FormationRelayFlagEntity flag)
			return;
		if (flag.TryGetLinkedCore(out PermanentFormationCoreEntity core))
		{
			FormationWorldBarDrawer.Draw(spriteBatch,
				new Vector2(flag.WorldCenter.X, position.Y * 16f - 19f),
				58, core.StoredQi, core.MaximumStoredQi,
				core.Integrity, core.MaximumIntegrity, core.Active);
		}
		else
		{
			FormationWorldBarDrawer.Draw(spriteBatch,
				new Vector2(flag.WorldCenter.X, position.Y * 16f - 19f),
				58, 0, 1, 0, 1, false);
		}
	}
}
