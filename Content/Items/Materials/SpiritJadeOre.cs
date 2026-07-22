using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Materials;

public class SpiritJadeOre : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SpiritJadeOreTile>());
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.sellPrice(copper: 80);
		Item.rare = ItemRarityID.Blue;
	}
}
