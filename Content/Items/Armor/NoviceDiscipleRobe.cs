using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Items.Armor;

[AutoloadEquip(EquipType.Body)]
public class NoviceDiscipleRobe : ModItem
{
	public override void SetStaticDefaults()
	{
		ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true;
	}

	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.defense = 2;
		Item.value = Item.buyPrice(silver: 10);
		Item.rare = ItemRarityID.White;
	}

	public override void UpdateEquip(Player player)
	{
		player.GetDamage(DamageClass.Generic) += 0.03f;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Silk, 8)
			.AddTile(TileID.Loom)
			.Register();
	}
}
