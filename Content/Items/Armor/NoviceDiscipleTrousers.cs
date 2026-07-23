using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Xianxia.Content.Items.Armor;

[AutoloadEquip(EquipType.Legs)]
public class NoviceDiscipleTrousers : ModItem
{
	public override void SetStaticDefaults()
	{
		ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
		ArmorIDs.Legs.Sets.OverridesLegs[Item.legSlot] = true;
	}

	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.defense = 1;
		Item.value = Item.buyPrice(silver: 8);
		Item.rare = ItemRarityID.White;
	}

	public override void UpdateEquip(Player player)
	{
		player.moveSpeed += 0.04f;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Silk, 6)
			.AddTile(TileID.Loom)
			.Register();
	}
}
