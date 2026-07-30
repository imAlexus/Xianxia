using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Items;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Alchemy;

public sealed class MeridianMendingPill : ModItem, IAlchemyPill
{
	public override string Texture =>
		"Xianxia/Content/Items/Alchemy/MeridianCleansingPill";

	public int RequiredAlchemyTier => 1;
	public int RequiredAlchemyStage => 1;
	public int AlchemyExperience => 48;
	public int SaturationCost => 35;
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
		Item.value = Item.buyPrice(gold: 1, silver: 50);
		Item.rare = ItemRarityID.LightRed;
	}

	public override bool CanUseItem(Player player) =>
		player.GetModPlayer<CultivationPlayer>().HasBurnedQi;

	public override bool? UseItem(Player player)
	{
		float effectiveness =
			AlchemyGlobalItem.GetCombinedEffectiveness(Item, player);
		int repaired = Math.Max(1,
			(int)MathF.Round(1000f * effectiveness));
		player.GetModPlayer<CultivationPlayer>()
			.RepairBurnedQiCapacity(repaired, showMessage: true);
		return true;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<Ironroot>(3)
		.AddIngredient<SpiritJadeBar>(2)
		.AddIngredient<SpiritBeastBlood>(3)
		.AddIngredient<FoundationBeastCore>()
		.AddIngredient<SpiritStone>(5)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}
