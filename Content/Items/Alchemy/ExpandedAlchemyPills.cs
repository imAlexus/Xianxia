using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Content.Buffs;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.Items.Alchemy;

public abstract class ExpandedAlchemyPill : ModItem, IAlchemyPill
{
	protected abstract int BuffType { get; }
	protected abstract int DurationSeconds { get; }
	public abstract int RequiredAlchemyTier { get; }
	public abstract int RequiredAlchemyStage { get; }
	public abstract int AlchemyExperience { get; }
	public abstract int SaturationCost { get; }
	public int BaseBuffDuration => DurationSeconds * 60;
	protected virtual int Rarity => ItemRarityID.Pink;
	protected virtual int Value => Item.buyPrice(gold: 1);

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.useStyle = ItemUseStyleID.EatFood;
		Item.useTime = 20;
		Item.useAnimation = 20;
		Item.UseSound = SoundID.Item3;
		Item.buffType = BuffType;
		Item.buffTime = BaseBuffDuration;
		Item.value = Value;
		Item.rare = Rarity;
	}
}

public class FoundationStabilizationPill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 1;
	public override int RequiredAlchemyStage => 2;
	public override int AlchemyExperience => 42;
	public override int SaturationCost => 30;
	protected override int BuffType => ModContent.BuffType<FoundationStabilizationBuff>();
	protected override int DurationSeconds => 300;
	protected override int Rarity => ItemRarityID.Orange;

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<Ironroot>(3)
		.AddIngredient<SpiritJadeBar>(2)
		.AddIngredient<QiGatheringBeastCore>()
		.AddIngredient<SpiritStone>(4)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class GoldenCoreTemperingPill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 2;
	public override int RequiredAlchemyStage => 2;
	public override int AlchemyExperience => 58;
	public override int SaturationCost => 35;
	protected override int BuffType => ModContent.BuffType<GoldenCoreTemperingBuff>();
	protected override int DurationSeconds => 240;
	protected override int Rarity => ItemRarityID.LightRed;

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<FoundationBeastCore>()
		.AddIngredient<FireLotus>(3)
		.AddIngredient<SpiritJadeBar>(3)
		.AddIngredient<SpiritStone>(6)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class NascentSoulAwakeningPill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 3;
	public override int RequiredAlchemyStage => 2;
	public override int AlchemyExperience => 78;
	public override int SaturationCost => 40;
	protected override int BuffType => ModContent.BuffType<NascentSoulAwakeningBuff>();
	protected override int DurationSeconds => 240;
	protected override int Rarity => ItemRarityID.Purple;
	protected override int Value => Item.buyPrice(gold: 2);

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<CoreFormationBeastCore>()
		.AddIngredient<ThunderEssence>(2)
		.AddIngredient<MoonDewFlower>(3)
		.AddIngredient<ProfoundIronBar>(2)
		.AddIngredient<SpiritStone>(10)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class SoulNourishingPill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 4;
	public override int RequiredAlchemyStage => 0;
	public override int AlchemyExperience => 90;
	public override int SaturationCost => 30;
	protected override int BuffType => ModContent.BuffType<SoulNourishingBuff>();
	protected override int DurationSeconds => 60;
	protected override int Rarity => ItemRarityID.Purple;
	protected override int Value => Item.buyPrice(gold: 2);

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<CoreFormationBeastCore>()
		.AddIngredient<MoonDewFlower>(4)
		.AddIngredient<SpiritBeastBlood>(5)
		.AddIngredient<SpiritStone>(10)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class VoidInsightPill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 4;
	public override int RequiredAlchemyStage => 1;
	public override int AlchemyExperience => 110;
	public override int SaturationCost => 40;
	protected override int BuffType => ModContent.BuffType<VoidInsightBuff>();
	protected override int DurationSeconds => 300;
	protected override int Rarity => ItemRarityID.Purple;
	protected override int Value => Item.buyPrice(gold: 3);

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<ThunderEssence>(3)
		.AddIngredient<ProfoundIronBar>(3)
		.AddIngredient<MoonDewFlower>(3)
		.AddIngredient<SpiritStone>(14)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class HeavenlyRebirthPill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 4;
	public override int RequiredAlchemyStage => 2;
	public override int AlchemyExperience => 150;
	public override int SaturationCost => 60;
	protected override int BuffType => ModContent.BuffType<HeavenlyRebirthBuff>();
	protected override int DurationSeconds => 600;
	protected override int Rarity => ItemRarityID.Red;
	protected override int Value => Item.buyPrice(gold: 5);

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<CoreFormationBeastCore>(2)
		.AddIngredient<FlameEssence>(3)
		.AddIngredient<ThunderEssence>(3)
		.AddIngredient<SpiritBeastBlood>(8)
		.AddIngredient<SpiritStone>(20)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class TribulationWardPill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 2;
	public override int RequiredAlchemyStage => 2;
	public override int AlchemyExperience => 60;
	public override int SaturationCost => 35;
	protected override int BuffType => ModContent.BuffType<TribulationWardBuff>();
	protected override int DurationSeconds => 600;
	protected override int Rarity => ItemRarityID.LightRed;

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<ThunderEssence>(2)
		.AddIngredient<Ironroot>(3)
		.AddIngredient<FoundationBeastCore>()
		.AddIngredient<SpiritStone>(8)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class SpiritBeastLurePill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 1;
	public override int RequiredAlchemyStage => 0;
	public override int AlchemyExperience => 26;
	public override int SaturationCost => 20;
	protected override int BuffType => ModContent.BuffType<SpiritBeastLureBuff>();
	protected override int DurationSeconds => 300;
	protected override int Rarity => ItemRarityID.Orange;
	protected override int Value => Item.buyPrice(silver: 80);

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<SpiritBeastBlood>(3)
		.AddIngredient<SpiritFur>(2)
		.AddIngredient<SpiritGrass>(2)
		.AddIngredient<MortalBeastCore>()
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}

public class ConcealmentPill : ExpandedAlchemyPill
{
	public override int RequiredAlchemyTier => 1;
	public override int RequiredAlchemyStage => 2;
	public override int AlchemyExperience => 38;
	public override int SaturationCost => 20;
	protected override int BuffType => ModContent.BuffType<ConcealmentBuff>();
	protected override int DurationSeconds => 300;
	protected override int Rarity => ItemRarityID.Orange;
	protected override int Value => Item.buyPrice(silver: 90);

	public override void AddRecipes() => CreateRecipe(2)
		.AddIngredient(ItemID.BottledWater)
		.AddIngredient<MoonDewFlower>(2)
		.AddIngredient<Ironroot>(2)
		.AddIngredient<SpiritFur>(3)
		.AddIngredient<SpiritStone>(3)
		.AddTile<AlchemyCauldronTile>()
		.RequireAlchemyRank(RequiredAlchemyTier, RequiredAlchemyStage)
		.Register();
}
