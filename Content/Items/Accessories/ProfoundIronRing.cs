using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Accessories;

public class ProfoundIronRing : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.accessory = true;
		Item.value = Item.buyPrice(gold: 2);
		Item.rare = ItemRarityID.Orange;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		player.GetDamage(DamageClass.Generic) += 0.08f;
		player.GetArmorPenetration(DamageClass.Generic) += 4f;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<ProfoundIronBar>(6)
			.AddIngredient<SpiritStone>(2)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
