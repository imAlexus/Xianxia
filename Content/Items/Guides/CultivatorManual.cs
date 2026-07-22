using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Systems;

namespace Xianxia.Content.Items.Guides;

public class CultivatorManual : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.maxStack = 1;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 20;
		Item.useAnimation = 20;
		Item.UseSound = SoundID.MenuOpen;
		Item.value = Item.buyPrice(silver: 10);
		Item.rare = ItemRarityID.Blue;
	}

	public override bool? UseItem(Player player)
	{
		if (player.whoAmI == Main.myPlayer)
		{
			CultivationManualSystem.Toggle();
		}
		return true;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Book)
			.AddIngredient<SpiritStone>()
			.AddTile(TileID.WorkBenches)
			.Register();
	}
}
