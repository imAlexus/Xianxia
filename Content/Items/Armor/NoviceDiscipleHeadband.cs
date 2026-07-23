using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Content.Items.Armor;

[AutoloadEquip(EquipType.Head)]
public class NoviceDiscipleHeadband : ModItem
{
	public override void SetStaticDefaults()
	{
		ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = true;
		ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
	}

	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.defense = 1;
		Item.value = Item.buyPrice(silver: 5);
		Item.rare = ItemRarityID.White;
	}

	public override void UpdateEquip(Player player)
	{
		player.GetCritChance(DamageClass.Generic) += 2f;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs) =>
		body.type == ModContent.ItemType<NoviceDiscipleRobe>()
		&& legs.type == ModContent.ItemType<NoviceDiscipleTrousers>();

	public override void UpdateArmorSet(Player player)
	{
		player.GetModPlayer<CultivationPlayer>().EquipmentMeditationQiBonus += 1;
		player.setBonus = Mod.GetLocalization("Items.NoviceDiscipleHeadband.SetBonus").Value;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Silk, 2)
			.AddTile(TileID.Loom)
			.Register();
	}
}
