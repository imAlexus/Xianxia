using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Items.Materials;

public class ProfoundIronBar : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 20;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.sellPrice(silver: 7);
		Item.rare = ItemRarityID.Orange;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<ProfoundIronOre>(4)
			.AddTile(TileID.Hellforge)
			.Register();
	}
}
