using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Alchemy;

namespace Xianxia.Common.Tiles;

public class SpiritualHerbGlobalTile : GlobalTile
{
	public override void Drop(int i, int j, int type)
	{
		if (WorldGen.gen || Main.netMode == NetmodeID.MultiplayerClient
			|| !IsNaturalPlant(type) || !Main.rand.NextBool(16))
			return;

		int playerIndex = Player.FindClosest(new Vector2(i * 16f, j * 16f), 16, 16);
		Player player = Main.player[playerIndex];
		int seedType = player.ZoneDesert
			? ModContent.ItemType<FireLotusSeed>()
			: player.ZoneJungle
				? ModContent.ItemType<MoonDewFlowerSeed>()
				: player.ZoneSnow
					? ModContent.ItemType<IronrootSeed>()
					: ModContent.ItemType<SpiritGrassSeed>();

		Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 16, seedType);
	}

	private static bool IsNaturalPlant(int type) => type is
		TileID.Plants or TileID.Plants2 or TileID.JunglePlants or TileID.JunglePlants2
		or TileID.OasisPlants;
}
