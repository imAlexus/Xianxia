using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Accessories;

public class SpiritJadePendant : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.accessory = true;
		Item.value = Item.buyPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		player.GetModPlayer<CultivationPlayer>().EquipmentPassiveQiBonus += 1;
		player.statDefense += 2;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<SpiritJadeBar>(6)
			.AddIngredient<SpiritStone>(2)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
