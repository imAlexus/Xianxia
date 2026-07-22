using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Common.Commands;

public class QiSenseCommand : ModCommand
{
	public override string Command => "qisense";
	public override CommandType Type => CommandType.Chat;
	public override string Usage => "/qisense [on|off]";
	public override string Description => "Toggles the Qi Sense passive";

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		CultivationPlayer cultivation = caller.Player.GetModPlayer<CultivationPlayer>();
		if (!cultivation.HasUnlockedQiSense)
		{
			caller.Reply(Mod.GetLocalization("Abilities.QiSenseRequiresGathering").Value);
			return;
		}

		bool enabled;
		if (args.Length == 0)
		{
			enabled = !cultivation.QiSenseEnabled;
		}
		else
		{
			switch (args[0].ToLowerInvariant())
			{
				case "on":
				case "enable":
				case "1":
					enabled = true;
					break;
				case "off":
				case "disable":
				case "0":
					enabled = false;
					break;
				default:
					caller.Reply(Mod.GetLocalization("Abilities.QiSenseUsage").Value);
					return;
			}
		}

		if (!cultivation.SetQiSenseEnabled(enabled))
		{
			caller.Reply(Mod.GetLocalization("Abilities.NotEnoughQi").Format(1));
			return;
		}

		caller.Reply(Mod.GetLocalization(enabled
			? "Abilities.QiSenseEnabled"
			: "Abilities.QiSenseDisabled").Value);
	}
}
