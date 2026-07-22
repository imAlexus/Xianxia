using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.Projectiles;

namespace Xianxia.Content.Items;

public class FireballTechnique : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.damage = 35;
		Item.DamageType = DamageClass.Magic;
		Item.noMelee = true;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<QiFireballProjectile>();
		Item.shootSpeed = 10f;
		Item.value = Item.buyPrice(silver: 40);
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
		player.GetModPlayer<CultivationPlayer>().TryCastFireball(velocity, source);
		return false;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Book)
			.AddIngredient<SpiritStone>()
			.AddIngredient(ItemID.FallenStar, 5)
			.AddTile(TileID.Bookcases)
			.Register();
	}
}
