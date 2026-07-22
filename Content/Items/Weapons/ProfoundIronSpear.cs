using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Weapons;

public class ProfoundIronSpear : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.DarkLance);
		Item.width = 56;
		Item.height = 56;
		Item.damage = 48;
		Item.useTime = 25;
		Item.useAnimation = 25;
		Item.value = Item.buyPrice(gold: 2);
		Item.rare = ItemRarityID.Orange;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<ProfoundIronBar>(10)
			.AddIngredient<SpiritStone>(2)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
