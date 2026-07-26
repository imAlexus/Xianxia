using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Formations;

public class PermanentFormationCore : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<PermanentFormationCoreTile>());
		Item.width = 48;
		Item.height = 48;
		Item.maxStack = 99;
		Item.value = Item.buyPrice(gold: 12);
		Item.rare = ItemRarityID.LightRed;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<ProfoundIronBar>(12)
			.AddIngredient<SpiritJadeBar>(8)
			.AddIngredient<SpiritStone>(15)
			.AddTile(TileID.MythrilAnvil)
			.Register();
	}
}
