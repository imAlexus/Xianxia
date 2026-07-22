using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Alchemy;

public class SpiritJadeCauldron : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SpiritJadeCauldronTile>());
		Item.width = 60;
		Item.height = 52;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(gold: 6);
		Item.rare = ItemRarityID.Orange;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient<AlchemyCauldron>()
		.AddIngredient<SpiritJadeBar>(12)
		.AddIngredient<SpiritStone>(8)
		.AddTile(TileID.Anvils)
		.Register();
}

public class ProfoundAlchemyCauldron : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<ProfoundAlchemyCauldronTile>());
		Item.width = 60;
		Item.height = 52;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(gold: 15);
		Item.rare = ItemRarityID.LightRed;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient<SpiritJadeCauldron>()
		.AddIngredient<ProfoundIronBar>(15)
		.AddIngredient<SpiritStone>(15)
		.AddTile(TileID.MythrilAnvil)
		.Register();
}
