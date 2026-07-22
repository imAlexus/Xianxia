using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Items.Materials;

public class SpiritJadeBar : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 20;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.sellPrice(silver: 4);
		Item.rare = ItemRarityID.Green;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<SpiritJadeOre>(4)
			.AddTile(TileID.Furnaces)
			.Register();
	}
}
