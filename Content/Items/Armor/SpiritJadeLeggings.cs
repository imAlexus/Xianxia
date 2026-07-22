using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Armor;

[AutoloadEquip(EquipType.Legs)]
public class SpiritJadeLeggings : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.defense = 6;
		Item.value = Item.buyPrice(gold: 1, silver: 25);
		Item.rare = ItemRarityID.Green;
	}

	public override void UpdateEquip(Player player)
	{
		player.moveSpeed += 0.08f;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<SpiritJadeBar>(14)
			.AddIngredient<SpiritStone>()
			.AddTile(TileID.Anvils)
			.Register();
	}
}
