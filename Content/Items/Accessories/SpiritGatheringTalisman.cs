using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Accessories;

public class SpiritGatheringTalisman : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.accessory = true;
		Item.value = Item.buyPrice(gold: 1, silver: 50);
		Item.rare = ItemRarityID.Green;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		player.GetModPlayer<CultivationPlayer>().EquipmentMeditationQiBonus += 2;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Silk, 5)
			.AddIngredient<SpiritJadeBar>(3)
			.AddIngredient<SpiritStone>(3)
			.AddTile(TileID.WorkBenches)
			.Register();
	}
}
