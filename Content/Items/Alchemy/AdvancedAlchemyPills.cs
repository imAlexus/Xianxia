using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Common.Items;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Alchemy;

public class MeridianCleansingPill : ModItem, IAlchemyPill
{
	public int RequiredAlchemyTier => 1;
	public int RequiredAlchemyStage => 1;
	public int AlchemyExperience => 34;
	public int SaturationCost => 0;
	public int BaseBuffDuration => 0;

	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.useStyle = ItemUseStyleID.EatFood;
		Item.useTime = 20;
		Item.useAnimation = 20;
		Item.UseSound = SoundID.Item3;
		Item.value = Item.buyPrice(silver: 90);
		Item.rare = ItemRarityID.Orange;
	}

	public override bool CanUseItem(Player player) =>
		player.GetModPlayer<AlchemyPlayer>().Saturation > 0f;

	public override bool? UseItem(Player player)
	{
		player.GetModPlayer<AlchemyPlayer>().ReduceSaturation(
			40f * AlchemyGlobalItem.GetCombinedEffectiveness(Item, player));
		return true;
	}

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<MoonDewFlower>(3)
		.AddIngredient<SpiritGrass>(3)
		.AddIngredient<PillDregs>(2)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class GreaterQiRecoveryPill : ModItem, IAlchemyPill
{
	public int RequiredAlchemyTier => 2;
	public int RequiredAlchemyStage => 1;
	public int AlchemyExperience => 46;
	public int SaturationCost => 30;
	public int BaseBuffDuration => 0;

	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.useStyle = ItemUseStyleID.EatFood;
		Item.useTime = 20;
		Item.useAnimation = 20;
		Item.UseSound = SoundID.Item3;
		Item.value = Item.buyPrice(gold: 1);
		Item.rare = ItemRarityID.LightRed;
	}

	public override bool CanUseItem(Player player)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		return cultivation.Qi < cultivation.MaxQi;
	}

	public override bool? UseItem(Player player)
	{
		CultivationPlayer cultivation = player.GetModPlayer<CultivationPlayer>();
		float effectiveness = AlchemyGlobalItem.GetCombinedEffectiveness(Item, player);
		int restored = Math.Max(500, cultivation.QiExp / 10);
		cultivation.RestoreQi((int)(restored * effectiveness));
		return true;
	}

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<SpiritGrass>(5)
		.AddIngredient<MoonDewFlower>(3)
		.AddIngredient<SpiritStone>(3)
		.AddIngredient<QiGatheringBeastCore>()
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}
