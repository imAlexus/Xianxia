using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Projectiles;

namespace Xianxia.Content.Items.Weapons;

public class FlyingSword : ModItem
{
	private const int BaseQiCost = 6;

	public override void SetDefaults()
	{
		Item.width = 54;
		Item.height = 54;
		Item.damage = 42;
		Item.DamageType = DamageClass.Magic;
		Item.knockBack = 4.5f;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = 25;
		Item.useAnimation = 25;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.autoReuse = true;
		Item.UseSound = SoundID.Item8;
		Item.shoot = ModContent.ProjectileType<FlyingSwordProjectile>();
		Item.shootSpeed = 13f;
		Item.value = Item.buyPrice(gold: 2);
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanUseItem(Player player)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		return cultivation.RealmIndex >= 1
			&& cultivation.Qi >= GetQiCost(player)
			&& player.ownedProjectileCounts[Item.shoot] < 2;
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
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		int qiCost = GetQiCost(player);
		if (!cultivation.SpendQi(qiCost))
		{
			return false;
		}

		Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
		return false;
	}

	private int GetQiCost(Player player)
	{
		int damage = (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage);
		return System.Math.Max(BaseQiCost, (int)System.Math.Ceiling(damage / 10f));
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient<SpiritJadeBar>(12)
			.AddIngredient<SpiritStone>(3)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
