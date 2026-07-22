using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Items.Materials;

namespace Xianxia.Content.Items.Weapons;

public class SpiritJadeSword : ModItem
{
	public override void SetDefaults()
	{
		Item.damage = 32;
		Item.DamageType = DamageClass.Melee;
		Item.knockBack = 6f;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 24;
		Item.useAnimation = 24;
		Item.autoReuse = true;
		Item.width = 48;
		Item.height = 48;
		Item.value = Item.buyPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
		Item.UseSound = SoundID.Item1;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<SpiritJadeBar>(8)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
