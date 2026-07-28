using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Items;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Projectiles;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Artifacts;

public interface ISpiritualArtifact
{
	int RequiredForgingTier { get; }
	int RequiredForgingStage { get; }
	int ForgingExperience { get; }
}

public abstract class SpiritualArtifactItem : ModItem, ISpiritualArtifact
{
	public abstract int RequiredForgingTier { get; }
	public abstract int RequiredForgingStage { get; }
	public abstract int ForgingExperience { get; }

	protected int AdjustedQiCost(Player player, int baseCost)
	{
		float multiplier = Item.GetGlobalItem<ArtifactGlobalItem>().QiCostMultiplier;
		return Math.Max(1, (int)MathF.Ceiling(baseCost * multiplier));
	}

	protected bool TrySpendQi(Player player, int baseCost) =>
		player.GetModPlayer<CultivationPlayer>().SpendQi(
			AdjustedQiCost(player, baseCost));

	protected Condition ForgingRequirement() => new(
		Mod.GetLocalization("Forging.RecipeRequirement").WithFormatArgs(
			RequiredForgingTier,
			Mod.GetLocalization($"Alchemy.Stages.{
				AlchemyPlayer.GetStageKey(RequiredForgingStage)}").Value),
		() => !Main.gameMenu
			&& Main.LocalPlayer is { active: true }
			&& Main.LocalPlayer.GetModPlayer<ArtifactForgingPlayer>()
				.MeetsRequirement(RequiredForgingTier, RequiredForgingStage));
}

public class VerdantAntlerStaff : SpiritualArtifactItem
{
	public override int RequiredForgingTier => 0;
	public override int RequiredForgingStage => 0;
	public override int ForgingExperience => 18;

	public override void SetDefaults()
	{
		Item.width = 46;
		Item.height = 46;
		Item.damage = 28;
		Item.DamageType = DamageClass.Magic;
		Item.knockBack = 4f;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = 24;
		Item.useAnimation = 24;
		Item.noMelee = true;
		Item.autoReuse = true;
		Item.UseSound = SoundID.Item8;
		Item.shoot = ModContent.ProjectileType<VerdantAntlerBoltProjectile>();
		Item.shootSpeed = 12.5f;
		Item.value = Item.buyPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
		Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (!TrySpendQi(player, 5))
			return false;
		Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
			player.whoAmI);
		return false;
	}

	public override void AddRecipes() =>
		CreateRecipe()
			.AddIngredient<JadeAntler>(2)
			.AddIngredient<SpiritJadeBar>(4)
			.AddIngredient<MortalBeastCore>()
			.AddTile<ArtifactForgeTile>()
			.AddCondition(ForgingRequirement())
			.Register();
}

public class JadeAntlerTalisman : SpiritualArtifactItem
{
	public override int RequiredForgingTier => 1;
	public override int RequiredForgingStage => 0;
	public override int ForgingExperience => 28;

	public override void SetDefaults()
	{
		Item.width = 38;
		Item.height = 42;
		Item.accessory = true;
		Item.value = Item.buyPrice(gold: 3);
		Item.rare = ItemRarityID.Orange;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		float power = Item.GetGlobalItem<ArtifactGlobalItem>().PowerMultiplier;
		player.statDefense += (int)MathF.Round(5f * power);
		player.lifeRegen += (int)MathF.Round(2f * power);
		player.GetDamage(DamageClass.Magic) += 0.06f * power;
	}

	public override void AddRecipes() =>
		CreateRecipe()
			.AddIngredient<JadeAntler>(3)
			.AddIngredient<SpiritJadeBar>(8)
			.AddIngredient<QiGatheringBeastCore>(2)
			.AddTile<SpiritJadeArtifactForgeTile>()
			.AddCondition(ForgingRequirement())
			.Register();
}

public class FlameSpiritFan : SpiritualArtifactItem
{
	public override int RequiredForgingTier => 2;
	public override int RequiredForgingStage => 0;
	public override int ForgingExperience => 42;

	public override void SetDefaults()
	{
		Item.width = 46;
		Item.height = 38;
		Item.damage = 58;
		Item.DamageType = DamageClass.Magic;
		Item.knockBack = 5f;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = 31;
		Item.useAnimation = 31;
		Item.noMelee = true;
		Item.autoReuse = true;
		Item.UseSound = SoundID.Item34;
		Item.shoot = ModContent.ProjectileType<FlameSpiritFanProjectile>();
		Item.shootSpeed = 10f;
		Item.value = Item.buyPrice(gold: 6);
		Item.rare = ItemRarityID.LightRed;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
		Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (!TrySpendQi(player, 15))
			return false;
		for (int i = -1; i <= 1; i++)
			Projectile.NewProjectile(source, position,
				velocity.RotatedBy(MathHelper.ToRadians(i * 9f)), type,
				damage, knockback, player.whoAmI);
		return false;
	}

	public override void AddRecipes() =>
		CreateRecipe()
			.AddIngredient<FlameEssence>(3)
			.AddIngredient<FoundationBeastCore>(2)
			.AddIngredient<SpiritJadeBar>(10)
			.AddTile<SpiritJadeArtifactForgeTile>()
			.AddCondition(ForgingRequirement())
			.Register();
}

public class ThunderclapSeal : SpiritualArtifactItem
{
	public override int RequiredForgingTier => 3;
	public override int RequiredForgingStage => 0;
	public override int ForgingExperience => 62;

	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 46;
		Item.damage = 92;
		Item.DamageType = DamageClass.Magic;
		Item.knockBack = 7f;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = 38;
		Item.useAnimation = 38;
		Item.noMelee = true;
		Item.autoReuse = true;
		Item.UseSound = SoundID.Item122;
		Item.shoot = ModContent.ProjectileType<ThunderclapOrbProjectile>();
		Item.shootSpeed = 18f;
		Item.value = Item.buyPrice(gold: 12);
		Item.rare = ItemRarityID.Cyan;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
		Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (!TrySpendQi(player, 28))
			return false;
		Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
			player.whoAmI);
		return false;
	}

	public override void AddRecipes() =>
		CreateRecipe()
			.AddIngredient<ThunderEssence>(4)
			.AddIngredient<CoreFormationBeastCore>(2)
			.AddIngredient<ProfoundIronBar>(12)
			.AddTile<ProfoundArtifactForgeTile>()
			.AddCondition(ForgingRequirement())
			.Register();
}

public class BeastSoulBanner : SpiritualArtifactItem
{
	public override int RequiredForgingTier => 4;
	public override int RequiredForgingStage => 0;
	public override int ForgingExperience => 85;

	public override void SetDefaults()
	{
		Item.width = 44;
		Item.height = 48;
		Item.accessory = true;
		Item.value = Item.buyPrice(gold: 20);
		Item.rare = ItemRarityID.Red;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		float power = Item.GetGlobalItem<ArtifactGlobalItem>().PowerMultiplier;
		player.GetDamage(DamageClass.Summon) += 0.16f * power;
		player.GetDamage(DamageClass.Magic) += 0.10f * power;
		player.maxMinions += 1;
		player.statDefense += (int)MathF.Round(8f * power);

		int guardianType = ModContent.ProjectileType<BeastSoulGuardianProjectile>();
		for (int i = 0; i < Main.maxProjectiles; i++)
		{
			Projectile projectile = Main.projectile[i];
			if (projectile.active && projectile.owner == player.whoAmI
				&& projectile.type == guardianType)
			{
				projectile.timeLeft = BeastSoulGuardianProjectile.AccessoryRefreshTime;
				if (player.whoAmI == Main.myPlayer
					&& Vector2.DistanceSquared(projectile.Center, player.Center)
						> BeastSoulGuardianProjectile.MaxOwnerDistance
						* BeastSoulGuardianProjectile.MaxOwnerDistance)
				{
					projectile.Center = player.Center;
					projectile.velocity = Vector2.Zero;
					projectile.netUpdate = true;
				}
			}
		}
		if (player.whoAmI == Main.myPlayer
			&& player.ownedProjectileCounts[guardianType] == 0)
		{
			int damage = (int)MathF.Round(72f * power
				* player.GetTotalDamage(DamageClass.Summon).Additive);
			Projectile.NewProjectile(player.GetSource_Accessory(Item),
				player.Center, Vector2.Zero, guardianType, Math.Max(1, damage),
				4f, player.whoAmI);
		}
	}

	public override void AddRecipes() =>
		CreateRecipe()
			.AddIngredient<CoreFormationBeastCore>(4)
			.AddIngredient<ThunderEssence>(2)
			.AddIngredient<FlameEssence>(2)
			.AddIngredient<ProfoundIronBar>(15)
			.AddTile<ProfoundArtifactForgeTile>()
			.AddCondition(ForgingRequirement())
			.Register();
}
