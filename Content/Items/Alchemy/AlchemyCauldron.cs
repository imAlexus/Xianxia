using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Alchemy;

public class AlchemyCauldron : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<AlchemyCauldronTile>());
		Item.width = 60;
		Item.height = 52;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(gold: 2);
		Item.rare = ItemRarityID.Green;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<ProfoundIronBar>(10)
			.AddIngredient<SpiritJadeBar>(5)
			.AddIngredient<SpiritStone>(3)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
