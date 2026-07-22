using Terraria;
using Terraria.ModLoader;

namespace Xianxia.Content.Items.Alchemy;

public interface IAlchemyPill
{
	int RequiredAlchemyTier { get; }
	int RequiredAlchemyStage { get; }
	int AlchemyExperience { get; }
	int SaturationCost { get; }
	int BaseBuffDuration { get; }
}

public static class AlchemyRecipeHelper
{
	public static Recipe RequireAlchemyRank(this Recipe recipe, int tier, int stage)
	{
		if (tier <= 0 && stage <= 0)
			return recipe;

		Xianxia mod = ModContent.GetInstance<Xianxia>();
		string realm = mod.GetLocalization(
			$"Cultivation.Realms.{Common.Players.AlchemyPlayer.GetTierRealmKey(tier)}").Value;
		string stageName = mod.GetLocalization(
			$"Alchemy.Stages.{Common.Players.AlchemyPlayer.GetStageKey(stage)}").Value;
		return recipe.AddCondition(
			mod.GetLocalization("Alchemy.RequiresRank").WithFormatArgs(tier, realm, stageName),
			() => Main.LocalPlayer.GetModPlayer<Common.Players.AlchemyPlayer>()
				.MeetsRequirement(tier, stage));
	}
}
