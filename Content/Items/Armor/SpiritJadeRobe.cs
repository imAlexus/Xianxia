using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Armor;

[AutoloadEquip(EquipType.Body)]
public class SpiritJadeRobe : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.defense = 7;
		Item.value = Item.buyPrice(gold: 1, silver: 50);
		Item.rare = ItemRarityID.Green;
	}

	public override void UpdateEquip(Player player)
	{
		player.GetDamage(DamageClass.Magic) += 0.07f;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<SpiritJadeBar>(18)
			.AddIngredient<SpiritStone>(2)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
