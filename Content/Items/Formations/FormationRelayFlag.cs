using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Formations;

public sealed class FormationRelayFlag : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<FormationRelayFlagTile>());
		Item.width = 40;
		Item.height = 52;
		Item.maxStack = 99;
		Item.value = Item.buyPrice(gold: 3);
		Item.rare = ItemRarityID.Orange;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<ProfoundIronBar>(6)
			.AddIngredient<SpiritJadeBar>(4)
			.AddIngredient<SpiritStone>(5)
			.AddTile(TileID.MythrilAnvil)
			.Register();
	}
}
