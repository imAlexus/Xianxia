using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Xianxia.Content.Items;
using Xianxia.Content.Items.Formations;
using Xianxia.Content.TileEntities;
using Xianxia.Common.Systems;
using Xianxia.Common.Utilities;

namespace Xianxia.Content.Tiles;

public class PermanentFormationCoreTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Origin = new Point16(1, 2);
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(
			ModContent.GetInstance<PermanentFormationCoreEntity>().Hook_AfterPlacement,
			-1, 0, true);
		TileObjectData.addTile(Type);

		DustType = DustID.GemSapphire;
		HitSound = SoundID.Tink;
		AddMapEntry(new Color(35, 205, 235), CreateMapEntryName());
		RegisterItemDrop(ModContent.ItemType<PermanentFormationCore>());
	}

	public override bool RightClick(int i, int j)
	{
		Point16 topLeft = GetTopLeft(i, j);
		bool deposit = Main.LocalPlayer.HeldItem.type == ModContent.ItemType<SpiritStone>();
		bool shift = Main.keyState.IsKeyDown(Keys.LeftShift)
			|| Main.keyState.IsKeyDown(Keys.RightShift);
		bool alt = Main.keyState.IsKeyDown(Keys.LeftAlt)
			|| Main.keyState.IsKeyDown(Keys.RightAlt);
		bool toggle = !deposit && shift && !alt;
		bool cycle = !deposit && alt && !shift;
		bool toggleMode = !deposit && alt && shift;
		if (!deposit && !shift && !alt
			&& TileEntity.ByPosition.TryGetValue(topLeft, out TileEntity panelEntity)
			&& panelEntity is PermanentFormationCoreEntity panelCore)
		{
			PermanentFormationUISystem.Open(panelCore.ID);
			return true;
		}
		if (Main.netMode == NetmodeID.MultiplayerClient)
			Xianxia.SendPermanentFormationAction(
				topLeft.X, topLeft.Y, deposit, toggle, cycle, toggleMode);
		else if (TileEntity.ByPosition.TryGetValue(topLeft, out TileEntity entity)
			&& entity is PermanentFormationCoreEntity core)
			core.HandleInteraction(Main.LocalPlayer, deposit, toggle, cycle, toggleMode);
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Main.LocalPlayer.noThrow = 2;
		Main.LocalPlayer.cursorItemIconEnabled = true;
		Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<PermanentFormationCore>();
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		ModContent.GetInstance<PermanentFormationCoreEntity>().Kill(i, j);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = 0.04f;
		g = 0.28f;
		b = 0.34f;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameX != 0 || tile.TileFrameY != 0)
			return;
		Point16 position = new(i, j);
		if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity entity)
			|| entity is not PermanentFormationCoreEntity core)
			return;
		FormationWorldBarDrawer.Draw(spriteBatch,
			new Vector2(core.WorldCenter.X, position.Y * 16f - 19f),
			72, core.StoredQi, core.MaximumStoredQi,
			core.Integrity, core.MaximumIntegrity, core.Active);
	}

	private static Point16 GetTopLeft(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		return new Point16(i - tile.TileFrameX / 18, j - tile.TileFrameY / 18);
	}
}
