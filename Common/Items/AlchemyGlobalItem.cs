using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Content.Items.Alchemy;

namespace Xianxia.Common.Items;

public class AlchemyGlobalItem : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (item.ModItem is not IAlchemyPill pill)
			return;

		AlchemyPlayer alchemy = Main.LocalPlayer.GetModPlayer<AlchemyPlayer>();
		tooltips.Add(new TooltipLine(Mod, "AlchemyMastery",
			Mod.GetLocalization("Alchemy.PillMasteryTooltip").Format(
				pill.RequiredAlchemyTier, alchemy.GetTierRealmName(pill.RequiredAlchemyTier),
				alchemy.GetStageName(pill.RequiredAlchemyStage), pill.AlchemyExperience)));
		tooltips.Add(new TooltipLine(Mod, "PillSaturation",
			Mod.GetLocalization("Alchemy.PillSaturationTooltip").Format(pill.SaturationCost))
		{
			OverrideColor = new Microsoft.Xna.Framework.Color(210, 145, 225)
		});
	}

	public override bool CanUseItem(Item item, Player player)
	{
		if (item.ModItem is not IAlchemyPill pill)
			return true;

		AlchemyPlayer alchemy = player.GetModPlayer<AlchemyPlayer>();
		if (!alchemy.CanConsumePill(pill.SaturationCost))
		{
			if (player.whoAmI == Main.myPlayer)
				Main.NewText(Mod.GetLocalization("Alchemy.TooSaturated").Value, 235, 105, 130);
			return false;
		}

		if (pill.BaseBuffDuration > 0)
			item.buffTime = (int)(pill.BaseBuffDuration * alchemy.PillEffectiveness);
		return true;
	}

	public override void OnConsumeItem(Item item, Player player)
	{
		if (item.ModItem is IAlchemyPill pill)
			player.GetModPlayer<AlchemyPlayer>().AddSaturation(pill.SaturationCost);
	}

	public override void OnCreated(Item item, ItemCreationContext context)
	{
		if (context is not RecipeItemCreationContext || item.ModItem is not IAlchemyPill pill
			|| Main.gameMenu || Main.LocalPlayer is not { active: true })
			return;

		Main.LocalPlayer.GetModPlayer<AlchemyPlayer>()
			.HandleCraftedPill(item, pill.AlchemyExperience);
	}
}
