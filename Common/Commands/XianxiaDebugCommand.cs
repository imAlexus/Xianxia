using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Abilities;
using Xianxia.Common.Config;
using Xianxia.Common.Players;
using Xianxia.Content.NPCs.SpiritBeasts;

namespace Xianxia.Common.Commands;

public class XianxiaDebugCommand : ModCommand
{
	public override string Command => "xiadebug";
	public override CommandType Type => CommandType.Chat;
	public override string Usage => "/xiadebug help";
	public override string Description => "Xianxia progression and ability testing tools";

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient
			&& !CultivationServerConfig.Instance.EnableDebugCommandsInMultiplayer)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.MultiplayerDisabled").Value);
			return;
		}

		CultivationPlayer cultivation = caller.Player.GetModPlayer<CultivationPlayer>();
		string action = args.Length == 0 ? "help" : args[0].ToLowerInvariant();
		switch (action)
		{
			case "help":
			case "?":
				ShowHelp(caller);
				break;
			case "status":
				ShowStatus(caller, cultivation);
				break;
			case "set":
				SetProgression(caller, cultivation, args);
				break;
			case "qi":
				SetQi(caller, cultivation, args);
				break;
			case "advance":
				if (!cultivation.DebugAdvanceStage())
				{
					caller.Reply(Mod.GetLocalization("DebugCommands.CannotAdvance").Value);
				}
				else
				{
					ShowStatus(caller, cultivation);
				}
				break;
			case "tribulation":
				HandleTribulation(caller, cultivation, args);
				break;
			case "ability":
				SetAbilityLevel(caller, cultivation, args);
				break;
			case "effect":
				PlayEffect(caller, cultivation, args);
				break;
			case "beast":
				SpawnSpiritBeast(caller, args);
				break;
			case "spawncheck":
				ShowSpiritBeastSpawnCheck(caller, cultivation);
				break;
			case "alchemy":
				SetAlchemyDebug(caller, args);
				break;
			case "forging":
				SetForgingDebug(caller, args);
				break;
			case "sect":
			case "sectrank":
				SetSectDebug(caller, args);
				break;
			case "reset":
				cultivation.DebugResetProgression();
				caller.Reply(Mod.GetLocalization("DebugCommands.Reset").Value);
				break;
			default:
				caller.Reply(Mod.GetLocalization("DebugCommands.Unknown").Format(args[0]));
				ShowHelp(caller);
				break;
		}
	}

	private void ShowHelp(CommandCaller caller)
	{
		caller.Reply(Mod.GetLocalization("DebugCommands.HelpTitle").Value);
		for (int i = 1; i <= 13; i++)
		{
			caller.Reply(Mod.GetLocalization($"DebugCommands.Help{i}").Value);
		}
	}

	private void SetSectDebug(CommandCaller caller, string[] args)
	{
		if (args.Length < 2 || !args[1].Equals("max", StringComparison.OrdinalIgnoreCase))
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.SectUsage").Value);
			return;
		}

		SectPlayer sect = caller.Player.GetModPlayer<SectPlayer>();
		sect.DebugMaxRank();
		caller.Reply(Mod.GetLocalization("DebugCommands.SectRankMaxed").Format(
			sect.GetRankName(), sect.LifetimeContribution));
	}

	private void SetAlchemyDebug(CommandCaller caller, string[] args)
	{
		AlchemyPlayer alchemy = caller.Player.GetModPlayer<AlchemyPlayer>();
		if (args.Length == 3 && args[1].Equals("saturation", StringComparison.OrdinalIgnoreCase)
			&& float.TryParse(args[2], out float saturation)
			&& saturation is >= 0f and <= AlchemyPlayer.MaximumSaturation)
		{
			alchemy.DebugSetSaturation(saturation);
			caller.Reply(Mod.GetLocalization("DebugCommands.AlchemySaturationSet").Format((int)saturation));
			return;
		}

		if (args.Length == 3 && int.TryParse(args[1], out int tier)
			&& tier is >= 0 and <= AlchemyPlayer.MaxTier
			&& TryParseAlchemyStage(args[2], out int stage))
		{
			alchemy.DebugSetRank(tier, stage);
			caller.Reply(Mod.GetLocalization("DebugCommands.AlchemyRankSet").Format(
				tier, alchemy.TierRealmName, alchemy.StageName));
			return;
		}

		caller.Reply(Mod.GetLocalization("DebugCommands.AlchemyUsage").Value);
	}

	private void SetForgingDebug(CommandCaller caller, string[] args)
	{
		ArtifactForgingPlayer forging =
			caller.Player.GetModPlayer<ArtifactForgingPlayer>();
		if (args.Length == 3 && int.TryParse(args[1], out int tier)
			&& tier is >= 0 and <= ArtifactForgingPlayer.MaxTier
			&& TryParseAlchemyStage(args[2], out int stage))
		{
			forging.DebugSetRank(tier, stage);
			caller.Reply(Mod.GetLocalization("DebugCommands.ForgingRankSet").Format(
				tier, forging.TierRealmName, forging.StageName));
			return;
		}

		caller.Reply(Mod.GetLocalization("DebugCommands.ForgingUsage").Value);
	}

	private static bool TryParseAlchemyStage(string value, out int stage)
	{
		stage = value.ToLowerInvariant() switch
		{
			"0" or "low" or "basso" or "bassa" or "l" => 0,
			"1" or "middle" or "mid" or "medio" or "media" or "m" => 1,
			"2" or "high" or "alto" or "alta" or "h" => 2,
			_ => -1
		};
		return stage >= 0;
	}

	private void ShowSpiritBeastSpawnCheck(CommandCaller caller, CultivationPlayer cultivation)
	{
		Player player = caller.Player;
		CultivationServerConfig config = CultivationServerConfig.Instance;
		float distance = Math.Abs(player.Center.X / 16f - Main.spawnTileX);
		bool safeZone = player.townNPCs > 2f || player.ZonePeaceCandle;
		bool invasion = Main.invasionType > 0;
		string biome = GetCurrentBiome(player);
		string time = Mod.GetLocalization(Main.dayTime
			? "DebugCommands.SpawnCheckDay" : "DebugCommands.SpawnCheckNight").Value;

		caller.Reply(Mod.GetLocalization("DebugCommands.SpawnCheckTitle").Value, new Color(90, 235, 220));
		caller.Reply(Mod.GetLocalization("DebugCommands.SpawnCheckEnvironment").Format(
			biome, time, (int)distance, cultivation.GetRealmName(), cultivation.Stage));
		caller.Reply(Mod.GetLocalization("DebugCommands.SpawnCheckSettings").Format(
			config.EnableSpiritBeasts, config.EnableSpiritBeastDistanceScaling,
			config.SpiritBeastSpawnRatePercent, safeZone, invasion));

		string globalReason = null;
		if (!config.EnableSpiritBeasts)
			globalReason = Mod.GetLocalization("DebugCommands.SpawnCheckReasonDisabled").Value;
		else if (safeZone)
			globalReason = Mod.GetLocalization("DebugCommands.SpawnCheckReasonSafe").Value;
		else if (invasion)
			globalReason = Mod.GetLocalization("DebugCommands.SpawnCheckReasonInvasion").Value;

		bool naturalSurface = player.ZoneOverworldHeight && !player.ZoneDungeon && !player.ZoneDesert;
		CheckSpiritBeast(caller, ModContent.NPCType<SpiritHare>(), globalReason,
			naturalSurface && Main.dayTime, 0, 0, distance, config);
		CheckSpiritBeast(caller, ModContent.NPCType<JadeHornDeer>(), globalReason,
			naturalSurface && Main.dayTime, 0, 200, distance, config);
		CheckSpiritBeast(caller, ModContent.NPCType<FlameTailedFox>(), globalReason,
			naturalSurface && !Main.dayTime, 1, 450, distance, config);
		CheckSpiritBeast(caller, ModContent.NPCType<ThunderclawTiger>(), globalReason,
			player.ZoneOverworldHeight && player.ZoneJungle && !Main.dayTime, 2, 700,
			distance, config);
		caller.Reply(Mod.GetLocalization("DebugCommands.SpawnCheckCandidateNote").Value,
			new Color(180, 190, 205));
	}

	private void CheckSpiritBeast(CommandCaller caller, int npcType, string globalReason,
		bool habitatValid, int requiredRealm, int minimumDistance, float currentDistance,
		CultivationServerConfig config)
	{
		string name = Lang.GetNPCNameValue(npcType);
		string reason = globalReason;
		if (reason is null && caller.Player.GetModPlayer<CultivationPlayer>().RealmIndex < requiredRealm)
			reason = Mod.GetLocalization("DebugCommands.SpawnCheckReasonRealm").Format(
				Mod.GetLocalization($"Cultivation.Realms.{GetRealmKey(requiredRealm)}").Value);
		if (reason is null && config.EnableSpiritBeastDistanceScaling && currentDistance < minimumDistance)
			reason = Mod.GetLocalization("DebugCommands.SpawnCheckReasonDistance").Format(
				minimumDistance - (int)currentDistance, minimumDistance);
		if (reason is null && !habitatValid)
			reason = Mod.GetLocalization("DebugCommands.SpawnCheckReasonHabitat").Value;

		if (reason is null)
			caller.Reply(Mod.GetLocalization("DebugCommands.SpawnCheckEligible").Format(name),
				new Color(105, 235, 125));
		else
			caller.Reply(Mod.GetLocalization("DebugCommands.SpawnCheckBlocked").Format(name, reason),
				new Color(245, 125, 105));
	}

	private string GetCurrentBiome(Player player)
	{
		string key = player.ZoneDungeon ? "Dungeon"
			: player.ZoneDesert ? "Desert"
			: player.ZoneSnow ? "Snow"
			: player.ZoneJungle ? "Jungle"
			: player.ZoneBeach ? "Beach"
			: player.ZoneOverworldHeight ? "Forest"
			: "Underground";
		return Mod.GetLocalization($"DebugCommands.SpawnCheckBiome{key}").Value;
	}

	private static string GetRealmKey(int realm) => realm switch
	{
		0 => "Mortal",
		1 => "QiGathering",
		2 => "FoundationEstablishment",
		3 => "CoreFormation",
		_ => "NascentSoul"
	};

	private void ShowStatus(CommandCaller caller, CultivationPlayer cultivation)
	{
		caller.Reply(Mod.GetLocalization("Cultivation.Status").Format(
			cultivation.GetRealmName(), cultivation.Stage,
			cultivation.Qi, cultivation.MaxQi,
			cultivation.QiExp, cultivation.NextStageThreshold));
	}

	private void SetProgression(CommandCaller caller, CultivationPlayer cultivation, string[] args)
	{
		if (args.Length < 3 || !TryParseRealm(args[1], out int realm)
			|| !int.TryParse(args[2], out int stage) || stage is < 1 or > 9)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.SetUsage").Value);
			return;
		}

		cultivation.DebugSetProgression(realm, stage);
		caller.Reply(Mod.GetLocalization("DebugCommands.ProgressionSet").Format(
			cultivation.GetRealmName(), cultivation.Stage));
	}

	private void SetQi(CommandCaller caller, CultivationPlayer cultivation, string[] args)
	{
		if (args.Length < 2)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.QiUsage").Value);
			return;
		}

		int amount;
		switch (args[1].ToLowerInvariant())
		{
			case "fill":
			case "max":
				amount = cultivation.MaxQi;
				break;
			case "empty":
			case "zero":
				amount = 0;
				break;
			default:
				if (!int.TryParse(args[1], out amount))
				{
					caller.Reply(Mod.GetLocalization("DebugCommands.QiUsage").Value);
					return;
				}
				break;
		}

		cultivation.DebugSetQi(amount);
		caller.Reply(Mod.GetLocalization("DebugCommands.QiSet").Format(
			cultivation.Qi, cultivation.MaxQi));
	}

	private void HandleTribulation(CommandCaller caller, CultivationPlayer cultivation, string[] args)
	{
		if (args.Length < 2)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.TribulationUsage").Value);
			return;
		}

		string option = args[1].ToLowerInvariant();
		if (option is "win" or "success")
		{
			if (!cultivation.DebugResolveTribulation(success: true))
				caller.Reply(Mod.GetLocalization("DebugCommands.NoTribulation").Value);
			return;
		}
		if (option is "fail" or "lose")
		{
			if (!cultivation.DebugResolveTribulation(success: false))
				caller.Reply(Mod.GetLocalization("DebugCommands.NoTribulation").Value);
			return;
		}

		if (!TryParseRealm(option, out int targetRealm) || targetRealm < 3
			|| !cultivation.DebugPrepareTribulation(targetRealm))
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.TribulationUsage").Value);
		}
	}

	private void SetAbilityLevel(CommandCaller caller, CultivationPlayer cultivation, string[] args)
	{
		if (args.Length < 3 || !int.TryParse(args[^1], out int level)
			|| level is < 1 or > CultivationAbilityInfo.MaxLevel)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.AbilityUsage").Value);
			return;
		}

		string abilityName = string.Concat(args.Skip(1).Take(args.Length - 2));
		if (abilityName.Equals("all", StringComparison.OrdinalIgnoreCase))
		{
			cultivation.DebugSetAllAbilityLevels(level);
			caller.Reply(Mod.GetLocalization("DebugCommands.AllAbilitiesSet").Format(level));
			return;
		}

		if (!Enum.TryParse(abilityName, ignoreCase: true, out CultivationAbility ability)
			|| ability == CultivationAbility.Count)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.AbilityUnknown").Format(abilityName));
			caller.Reply(string.Join(", ", Enum.GetNames<CultivationAbility>().Where(name => name != "Count")));
			return;
		}

		cultivation.DebugSetAbilityLevel(ability, level);
		caller.Reply(Mod.GetLocalization("DebugCommands.AbilitySet").Format(ability, level));
	}

	private void PlayEffect(CommandCaller caller, CultivationPlayer cultivation, string[] args)
	{
		if (args.Length < 2 || args[1].ToLowerInvariant() is not ("stage" or "realm"))
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.EffectUsage").Value);
			return;
		}

		cultivation.DebugPlayBreakthroughEffect(args[1].Equals("realm", StringComparison.OrdinalIgnoreCase));
		caller.Reply(Mod.GetLocalization("DebugCommands.EffectPlayed").Format(args[1]));
	}

	private void SpawnSpiritBeast(CommandCaller caller, string[] args)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.BeastSinglePlayer").Value);
			return;
		}

		if (args.Length < 2)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.BeastUsage").Value);
			return;
		}

		int type = args[1].ToLowerInvariant() switch
		{
			"hare" or "rabbit" => ModContent.NPCType<SpiritHare>(),
			"deer" or "jade" => ModContent.NPCType<JadeHornDeer>(),
			"fox" or "flame" => ModContent.NPCType<FlameTailedFox>(),
			"tiger" or "thunder" => ModContent.NPCType<ThunderclawTiger>(),
			_ => -1
		};
		if (type < 0)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.BeastUsage").Value);
			return;
		}

		int npcIndex = NPC.NewNPC(
			caller.Player.GetSource_Misc("XianxiaDebugBeast"),
			(int)caller.Player.Center.X + 100,
			(int)caller.Player.Center.Y - 30,
			type);
		if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
		{
			caller.Reply(Mod.GetLocalization("DebugCommands.BeastSpawned")
				.Format(Main.npc[npcIndex].FullName));
		}
	}

	private static bool TryParseRealm(string value, out int realm)
	{
		string normalized = value.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
		if (int.TryParse(normalized, out int number) && number is >= 1 and <= 5)
		{
			realm = number - 1;
			return true;
		}

		realm = normalized switch
		{
			"mortal" => 0,
			"qi" or "qigathering" or "qicondensation" => 1,
			"foundation" or "foundationestablishment" => 2,
			"core" or "coreformation" => 3,
			"nascent" or "nascentsoul" => 4,
			_ => -1
		};
		return realm >= 0;
	}
}
