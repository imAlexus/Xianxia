using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Armor;

[AutoloadEquip(EquipType.Head)]
public class SpiritJadeHeadpiece : ModItem
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
		Item.defense = 5;
		Item.value = Item.buyPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
	}

	public override void UpdateEquip(Player player)
	{
		player.GetCritChance(DamageClass.Magic) += 4f;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs) =>
		body.type == ModContent.ItemType<SpiritJadeRobe>()
		&& legs.type == ModContent.ItemType<SpiritJadeLeggings>();

	public override void UpdateArmorSet(Player player)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		cultivation.EquipmentPassiveQiBonus += 2;
		cultivation.EquipmentMeditationQiBonus += 1;
		player.GetDamage(DamageClass.Magic) += 0.1f;
		player.setBonus = Mod.GetLocalization("Items.SpiritJadeHeadpiece.SetBonus").Value;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<SpiritJadeBar>(10)
			.AddIngredient<SpiritStone>()
			.AddTile(TileID.Anvils)
			.Register();
	}
}
