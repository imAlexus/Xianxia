using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Materials;

public class ProfoundIronOre : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<ProfoundIronOreTile>());
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.sellPrice(silver: 1, copper: 50);
		Item.rare = ItemRarityID.Green;
	}
}
