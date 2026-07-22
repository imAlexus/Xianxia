using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.Projectiles;

namespace Xianxia.Content.Items;

public class QiPalmTechnique : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.damage = 22;
		Item.DamageType = DamageClass.Magic;
		Item.knockBack = 9f;
		Item.noMelee = true;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = 36;
		Item.useAnimation = 36;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<QiPalmProjectile>();
		Item.shootSpeed = 9.5f;
		Item.value = Item.buyPrice(silver: 60);
		Item.rare = ItemRarityID.Green;
	}

	public override bool Shoot(
		Player player,
		EntitySource_ItemUse_WithAmmo source,
		Vector2 position,
		Vector2 velocity,
		int type,
		int damage,
		float knockback)
	{
		player.GetModPlayer<CultivationPlayer>().TryCastQiPalm(velocity, source);
		return false;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Book)
			.AddIngredient<SpiritStone>(2)
			.AddIngredient(ItemID.FallenStar, 5)
			.AddTile(TileID.Bookcases)
			.Register();
	}
}
