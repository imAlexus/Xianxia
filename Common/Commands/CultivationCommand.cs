using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Common.Commands;

public class CultivationCommand : ModCommand
{
	public override string Command => "cultivation";
	public override CommandType Type => CommandType.Chat;
	public override string Usage => "/cultivation";
	public override string Description => "Shows your current cultivation progress";

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		CultivationPlayer cultivation = caller.Player.GetModPlayer<CultivationPlayer>();
		caller.Reply(Mod.GetLocalization("Cultivation.Status").Format(
			cultivation.GetRealmName(), cultivation.Stage,
			cultivation.Qi, cultivation.MaxQi,
			cultivation.QiExp, cultivation.NextStageThreshold));
		caller.Reply(cultivation.GetRealmBonusSummary());
		caller.Reply(Mod.GetLocalization("Cultivation.MeditationRate").Format(
			cultivation.MeditationQiPerSecond));
		caller.Reply(Mod.GetLocalization("Cultivation.PassiveQiRecovery").Format(
			cultivation.PassiveQiRecoveryPerSecond));
		if (cultivation.HasUnlockedQiProtection)
		{
			caller.Reply(Mod.GetLocalization("Abilities.QiProtectionStatus").Format(
				Mod.GetLocalization(cultivation.QiProtectionEnabled
					? "Abilities.StateEnabled"
					: "Abilities.StateDisabled").Value));
		}
		if (cultivation.HasUnlockedQiSense)
		{
			caller.Reply(Mod.GetLocalization("Abilities.QiSenseStatus").Format(
				Mod.GetLocalization(cultivation.QiSenseEnabled
					? "Abilities.StateEnabled"
					: "Abilities.StateDisabled").Value));
		}
	}
}
