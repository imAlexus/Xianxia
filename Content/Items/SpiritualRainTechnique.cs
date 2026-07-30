using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Abilities;
using Xianxia.Common.Players;
using Xianxia.Content.Projectiles;

namespace Xianxia.Content.Items;

public class SpiritualRainTechnique : ModItem
{
	private const int BaseQiCost = 40;
	private const int MinimumQiCost = 20;
	private const float BaseRadiusInTiles = 12f;
	private const float RadiusPerLevel = 0.25f;
	private const float MaximumCastRange = 30f * 16f;
	private const int CooldownTicks = 15 * 60;

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 30;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 45;
		Item.useAnimation = 45;
		Item.noMelee = true;
		Item.UseSound = SoundID.Item66;
		Item.shoot = ModContent.ProjectileType<SpiritualRainProjectile>();
		Item.shootSpeed = 0f;
		Item.value = Item.buyPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanUseItem(Player player)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		if (player.ownedProjectileCounts[Item.shoot] > 0)
			return false;
		if (cultivation.SpiritualRainCooldown <= 0)
			return true;

		if (player.whoAmI == Main.myPlayer)
		{
			int seconds = (int)MathF.Ceiling(
				cultivation.SpiritualRainCooldown / 60f);
			Main.NewText(Mod.GetLocalization("Abilities.SpiritualRainCooldown")
				.Format(seconds), Color.Orange);
		}
		return false;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
		Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		TryCast(player, source);
		return false;
	}

	public static bool TryCast(Player player, IEntitySource source)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		int projectileType =
			ModContent.ProjectileType<SpiritualRainProjectile>();
		if (player.ownedProjectileCounts[projectileType] > 0)
			return false;
		if (cultivation.SpiritualRainCooldown > 0)
		{
			if (player.whoAmI == Main.myPlayer)
			{
				int seconds = (int)MathF.Ceiling(
					cultivation.SpiritualRainCooldown / 60f);
				Main.NewText(cultivation.Mod.GetLocalization(
					"Abilities.SpiritualRainCooldown").Format(seconds),
					Color.Orange);
			}
			return false;
		}
		if (cultivation.RealmIndex < 1)
		{
			Main.NewText(cultivation.Mod.GetLocalization(
				"Abilities.RequiresRealm").Format(
					cultivation.Mod.GetLocalization(
						"Cultivation.Realms.QiCondensation").Value),
				Color.OrangeRed);
			return false;
		}

		int abilityLevel = cultivation.GetAbilityLevel(CultivationAbility.SpiritualRain);
		int baseQiCost = Math.Max(MinimumQiCost, BaseQiCost - abilityLevel + 1);
		int qiCost = cultivation.GetAbilityQiCost(
			baseQiCost, CultivationAbility.SpiritualRain);
		if (!cultivation.SpendAbilityQi(baseQiCost, CultivationAbility.SpiritualRain))
		{
			Main.NewText(cultivation.Mod.GetLocalization(
				"Abilities.NotEnoughQi").Format(qiCost), Color.OrangeRed);
			return false;
		}

		Vector2 target = Main.MouseWorld;
		Vector2 offset = target - player.Center;
		if (offset.LengthSquared() > MaximumCastRange * MaximumCastRange)
			target = player.Center + offset.SafeNormalize(Vector2.UnitY) * MaximumCastRange;

		float radiusInTiles = (BaseRadiusInTiles + (abilityLevel - 1) * RadiusPerLevel)
			* cultivation.GetAbilityPowerMultiplier(
				CultivationAbility.SpiritualRain, 0f);
		Projectile.NewProjectile(source, target, Vector2.Zero,
			projectileType, 0, 0f,
			player.whoAmI, radiusInTiles);
		cultivation.StartSpiritualRainCooldown(CooldownTicks);
		cultivation.AddAbilityExperience(CultivationAbility.SpiritualRain, 5);
		return true;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Book)
			.AddIngredient(ItemID.Waterleaf, 3)
			.AddIngredient(ItemID.Moonglow, 3)
			.AddIngredient<SpiritStone>(2)
			.AddTile(TileID.Bookcases)
			.Register();
	}
}
