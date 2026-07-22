using System;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia.Common.Commands;

public class QiProtectionCommand : ModCommand
{
	public override string Command => "qiprotection";
	public override CommandType Type => CommandType.Chat;
	public override string Usage => "/qiprotection [on|off]";
	public override string Description => "Toggles the Qi Protection passive";

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		CultivationPlayer cultivation = caller.Player.GetModPlayer<CultivationPlayer>();
		if (!cultivation.HasUnlockedQiProtection)
		{
			caller.Reply(Mod.GetLocalization("Abilities.QiProtectionRequiresFoundation").Value);
			return;
		}

		bool enabled;
		if (args.Length == 0)
		{
			enabled = !cultivation.QiProtectionEnabled;
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
					caller.Reply(Mod.GetLocalization("Abilities.QiProtectionUsage").Value);
					return;
			}
		}

		cultivation.SetQiProtectionEnabled(enabled);
		caller.Reply(Mod.GetLocalization(enabled
			? "Abilities.QiProtectionEnabled"
			: "Abilities.QiProtectionDisabled").Value);
	}
}
