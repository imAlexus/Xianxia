using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;
using Xianxia.Common.Systems;
using Xianxia.Content.TileEntities;

namespace Xianxia;

internal enum XianxiaMessageType : byte
{
	MeditationState,
	BreakthroughEffect,
	AlchemyState,
	SectState,
	FormationContribution,
	PermanentFormationAction,
	PermanentFormationTribulationHit,
	FormationPathState,
	PermanentFormationRelayAction
}

public class Xianxia : Mod
{
	internal static ModKeybind MeditateKeybind { get; private set; }
	internal static ModKeybind QiResistanceKeybind { get; private set; }
	internal static ModKeybind QiFlightKeybind { get; private set; }
	internal static ModKeybind FireballKeybind { get; private set; }
	internal static ModKeybind AbilityWheelKeybind { get; private set; }
	internal static ModKeybind QiPalmKeybind { get; private set; }
	internal static ModKeybind FlameStepKeybind { get; private set; }
	internal static ModKeybind NascentTeleportKeybind { get; private set; }
	internal static ModKeybind SpiritualPressureKeybind { get; private set; }
	internal static ModKeybind NightVisionKeybind { get; private set; }
	internal static ModKeybind AbilityTreeKeybind { get; private set; }
	internal static ModKeybind SpiritSwordRainKeybind { get; private set; }
	internal static ModKeybind SectFormationKeybind { get; private set; }

	public override void Load()
	{
		// Custom currencies must be registered from Mod.Load so the shop and
		// CustomCurrencyManager share the same live ID after every reload.
		SectCurrencySystem.Register();

		MeditateKeybind = KeybindLoader.RegisterKeybind(this, "Meditate", "LeftControl");
		QiResistanceKeybind = KeybindLoader.RegisterKeybind(this, "QiResistance", "Z");
		QiFlightKeybind = KeybindLoader.RegisterKeybind(this, "QiFlight", "V");
		FireballKeybind = KeybindLoader.RegisterKeybind(this, "Fireball", "X");
		AbilityWheelKeybind = KeybindLoader.RegisterKeybind(this, "AbilityWheel", "G");
		QiPalmKeybind = KeybindLoader.RegisterKeybind(this, "QiPalm", "C");
		FlameStepKeybind = KeybindLoader.RegisterKeybind(this, "FlameStep", "F");
		NascentTeleportKeybind = KeybindLoader.RegisterKeybind(this, "NascentTeleport", "N");
		SpiritualPressureKeybind = KeybindLoader.RegisterKeybind(this, "SpiritualPressure", "P");
		NightVisionKeybind = KeybindLoader.RegisterKeybind(this, "NightVision", "K");
		AbilityTreeKeybind = KeybindLoader.RegisterKeybind(this, "AbilityTree", "J");
		SpiritSwordRainKeybind = KeybindLoader.RegisterKeybind(this, "SpiritSwordRain", "R");
		SectFormationKeybind = KeybindLoader.RegisterKeybind(this, "SectFormation", "B");
	}

	public override void Unload()
	{
		SectCurrencySystem.Reset();

		MeditateKeybind = null;
		QiResistanceKeybind = null;
		QiFlightKeybind = null;
		FireballKeybind = null;
		AbilityWheelKeybind = null;
		QiPalmKeybind = null;
		FlameStepKeybind = null;
		NascentTeleportKeybind = null;
		SpiritualPressureKeybind = null;
		NightVisionKeybind = null;
		AbilityTreeKeybind = null;
		SpiritSwordRainKeybind = null;
		SectFormationKeybind = null;
	}

	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		XianxiaMessageType messageType = (XianxiaMessageType)reader.ReadByte();
		if (messageType == XianxiaMessageType.MeditationState)
		{
			HandleMeditationState(reader, whoAmI);
			return;
		}

		if (messageType == XianxiaMessageType.BreakthroughEffect)
		{
			HandleBreakthroughEffect(reader, whoAmI);
			return;
		}

		if (messageType == XianxiaMessageType.AlchemyState)
		{
			HandleAlchemyState(reader, whoAmI);
			return;
		}

		if (messageType == XianxiaMessageType.SectState)
		{
			HandleSectState(reader, whoAmI);
			return;
		}

		if (messageType == XianxiaMessageType.FormationContribution)
		{
			HandleFormationContribution(reader, whoAmI);
			return;
		}

		if (messageType == XianxiaMessageType.PermanentFormationAction)
		{
			HandlePermanentFormationAction(reader, whoAmI);
			return;
		}

		if (messageType == XianxiaMessageType.PermanentFormationTribulationHit)
		{
			HandlePermanentFormationTribulationHit(reader, whoAmI);
			return;
		}

		if (messageType == XianxiaMessageType.FormationPathState)
		{
			HandleFormationPathState(reader, whoAmI);
			return;
		}

		if (messageType == XianxiaMessageType.PermanentFormationRelayAction)
		{
			HandlePermanentFormationRelayAction(reader, whoAmI);
			return;
		}

		Logger.Warn($"Unknown packet type: {messageType}");
	}

	private static void HandleMeditationState(BinaryReader reader, int whoAmI)
	{

		int playerIndex = reader.ReadByte();
		bool isMeditating = reader.ReadBoolean();

		if (Main.netMode == NetmodeID.Server)
		{
			// Never trust a player index supplied by a client.
			playerIndex = whoAmI;
		}

		if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active)
		{
			return;
		}

		Main.player[playerIndex]
			.GetModPlayer<CultivationPlayer>()
			.SetMeditatingFromNetwork(isMeditating);

		if (Main.netMode == NetmodeID.Server)
		{
			SendMeditationState(playerIndex, isMeditating, ignoreClient: whoAmI);
		}
	}

	private static void HandleBreakthroughEffect(BinaryReader reader, int whoAmI)
	{
		int playerIndex = reader.ReadByte();
		int realmIndex = reader.ReadByte();
		bool isRealmBreakthrough = reader.ReadBoolean();

		if (Main.netMode == NetmodeID.Server)
		{
			playerIndex = whoAmI;
		}

		if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active)
		{
			return;
		}

		Main.player[playerIndex]
			.GetModPlayer<CultivationPlayer>()
			.SetBreakthroughEffectFromNetwork(realmIndex, isRealmBreakthrough);

		if (Main.netMode == NetmodeID.Server)
		{
			SendBreakthroughEffect(playerIndex, realmIndex, isRealmBreakthrough, ignoreClient: whoAmI);
		}
	}

	private static void HandleAlchemyState(BinaryReader reader, int whoAmI)
	{
		int playerIndex = reader.ReadByte();
		int tier = reader.ReadByte();
		int stage = reader.ReadByte();
		int experience = reader.ReadInt32();
		float saturation = reader.ReadSingle();
		if (Main.netMode == NetmodeID.Server)
			playerIndex = whoAmI;
		if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active)
			return;

		Main.player[playerIndex].GetModPlayer<AlchemyPlayer>()
			.SetAlchemyStateFromNetwork(tier, stage, experience, saturation);
		if (Main.netMode == NetmodeID.Server)
			SendAlchemyState(playerIndex, tier, stage, experience, saturation,
				ignoreClient: whoAmI);
	}

	private static void HandleSectState(BinaryReader reader, int whoAmI)
	{
		int playerIndex = reader.ReadByte();
		bool joined = reader.ReadBoolean();
		int lifetimeContribution = reader.ReadInt32();
		SectMissionType missionType = (SectMissionType)reader.ReadByte();
		int missionTarget = reader.ReadInt32();
		int missionProgress = reader.ReadInt32();
		bool swordIntent = reader.ReadBoolean();
		bool swordRain = reader.ReadBoolean();
		bool formation = reader.ReadBoolean();
		if (Main.netMode == NetmodeID.Server)
			playerIndex = whoAmI;
		if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active)
			return;

		Main.player[playerIndex].GetModPlayer<SectPlayer>().SetSectStateFromNetwork(
			joined, lifetimeContribution, missionType, missionTarget, missionProgress,
			swordIntent, swordRain, formation);
		if (Main.netMode == NetmodeID.Server)
			SendSectState(playerIndex, joined, lifetimeContribution, missionType,
				missionTarget, missionProgress, swordIntent, swordRain, formation,
				ignoreClient: whoAmI);
	}

	private static void HandleFormationContribution(BinaryReader reader, int whoAmI)
	{
		int contributorIndex = reader.ReadByte();
		int ownerIndex = reader.ReadByte();
		int qiAmount = reader.ReadByte();
		if (Main.netMode == NetmodeID.Server)
			contributorIndex = whoAmI;
		if (contributorIndex < 0 || contributorIndex >= Main.maxPlayers
			|| ownerIndex < 0 || ownerIndex >= Main.maxPlayers
			|| !Main.player[contributorIndex].active
			|| !Main.player[ownerIndex].active
			|| qiAmount <= 0 || qiAmount > 20)
			return;

		Player contributor = Main.player[contributorIndex];
		Player owner = Main.player[ownerIndex];
		if (!owner.HasBuff<Content.Buffs.SectProtectionFormationBuff>()
			|| Vector2.DistanceSquared(contributor.Center, owner.Center) > 360f * 360f)
			return;

		if (Main.netMode == NetmodeID.Server)
		{
			owner.GetModPlayer<SectPlayer>()
				.ReceiveFormationContribution(contributorIndex, qiAmount);
			SendFormationContribution(contributorIndex, ownerIndex, qiAmount,
				toClient: ownerIndex);
			return;
		}

		owner.GetModPlayer<SectPlayer>()
			.ReceiveFormationContribution(contributorIndex, qiAmount);
	}

	private static void HandlePermanentFormationAction(BinaryReader reader, int whoAmI)
	{
		short x = reader.ReadInt16();
		short y = reader.ReadInt16();
		bool deposit = reader.ReadBoolean();
		bool toggle = reader.ReadBoolean();
		bool cycle = reader.ReadBoolean();
		bool toggleMode = reader.ReadBoolean();
		bool upgrade = reader.ReadBoolean();
		bool cycleAccess = reader.ReadBoolean();
		byte requestedKind = reader.ReadByte();
		if (Main.netMode != NetmodeID.Server
			|| whoAmI < 0 || whoAmI >= Main.maxPlayers
			|| !Main.player[whoAmI].active)
			return;
		if (TileEntity.ByPosition.TryGetValue(new Point16(x, y), out TileEntity entity)
			&& entity is PermanentFormationCoreEntity core)
			core.HandleInteraction(Main.player[whoAmI], deposit, toggle,
				cycle, toggleMode, upgrade, cycleAccess, requestedKind);
	}

	private static void HandlePermanentFormationTribulationHit(
		BinaryReader reader, int whoAmI)
	{
		int entityId = reader.ReadInt32();
		int incomingDamage = reader.ReadInt32();
		int realmOffset = reader.ReadByte();
		if (Main.netMode != NetmodeID.Server
			|| whoAmI < 0 || whoAmI >= Main.maxPlayers
			|| !Main.player[whoAmI].active
			|| incomingDamage < 1 || incomingDamage > 5000
			|| realmOffset < 0 || realmOffset > 4)
			return;
		if (TileEntity.ByID.TryGetValue(entityId, out TileEntity entity)
			&& entity is PermanentFormationCoreEntity core)
			core.AbsorbTribulationStrike(Main.player[whoAmI],
				incomingDamage, realmOffset, showEffect: false);
	}

	private static void HandleFormationPathState(BinaryReader reader, int whoAmI)
	{
		int playerIndex = reader.ReadByte();
		int tier = reader.ReadByte();
		int stage = reader.ReadByte();
		int experience = reader.ReadInt32();
		int defenseCycles = reader.ReadInt32();
		int damageHandled = reader.ReadInt32();
		int tribulationStrikes = reader.ReadInt32();
		int veinsLinked = reader.ReadInt32();
		if (Main.netMode == NetmodeID.Server)
			playerIndex = whoAmI;
		if (playerIndex < 0 || playerIndex >= Main.maxPlayers
			|| !Main.player[playerIndex].active)
			return;
		Main.player[playerIndex].GetModPlayer<FormationPathPlayer>()
			.SetStateFromNetwork(tier, stage, experience, defenseCycles,
				damageHandled, tribulationStrikes, veinsLinked);
		if (Main.netMode == NetmodeID.Server)
			SendFormationPathState(playerIndex, tier, stage, experience,
				defenseCycles, damageHandled, tribulationStrikes, veinsLinked,
				ignoreClient: whoAmI);
	}

	private static void HandlePermanentFormationRelayAction(
		BinaryReader reader, int whoAmI)
	{
		short x = reader.ReadInt16();
		short y = reader.ReadInt16();
		byte mode = reader.ReadByte();
		if (Main.netMode != NetmodeID.Server
			|| whoAmI < 0 || whoAmI >= Main.maxPlayers
			|| !Main.player[whoAmI].active)
			return;
		if (TileEntity.ByPosition.TryGetValue(new Point16(x, y),
			out TileEntity entity)
			&& entity is FormationRelayFlagEntity relay)
			relay.TrySetSpecializedMode(Main.player[whoAmI], mode);
	}

	internal static void SendMeditationState(int playerIndex, bool isMeditating, int toClient = -1, int ignoreClient = -1)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.MeditationState);
		packet.Write((byte)playerIndex);
		packet.Write(isMeditating);
		packet.Send(toClient, ignoreClient);
	}

	internal static void SendBreakthroughEffect(
		int playerIndex,
		int realmIndex,
		bool isRealmBreakthrough,
		int toClient = -1,
		int ignoreClient = -1)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.BreakthroughEffect);
		packet.Write((byte)playerIndex);
		packet.Write((byte)realmIndex);
		packet.Write(isRealmBreakthrough);
		packet.Send(toClient, ignoreClient);
	}

	internal static void SendAlchemyState(int playerIndex, int tier, int stage, int experience,
		float saturation, int toClient = -1, int ignoreClient = -1)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.AlchemyState);
		packet.Write((byte)playerIndex);
		packet.Write((byte)tier);
		packet.Write((byte)stage);
		packet.Write(experience);
		packet.Write(saturation);
		packet.Send(toClient, ignoreClient);
	}

	internal static void SendSectState(int playerIndex, bool joined, int lifetimeContribution,
		SectMissionType missionType, int missionTarget, int missionProgress,
		bool swordIntent, bool swordRain, bool formation,
		int toClient = -1, int ignoreClient = -1)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.SectState);
		packet.Write((byte)playerIndex);
		packet.Write(joined);
		packet.Write(lifetimeContribution);
		packet.Write((byte)missionType);
		packet.Write(missionTarget);
		packet.Write(missionProgress);
		packet.Write(swordIntent);
		packet.Write(swordRain);
		packet.Write(formation);
		packet.Send(toClient, ignoreClient);
	}

	internal static void SendFormationContribution(int contributorIndex, int ownerIndex,
		int qiAmount, int toClient = -1, int ignoreClient = -1)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.FormationContribution);
		packet.Write((byte)contributorIndex);
		packet.Write((byte)ownerIndex);
		packet.Write((byte)Math.Clamp(qiAmount, 0, 20));
		packet.Send(toClient, ignoreClient);
	}

	internal static void SendPermanentFormationAction(int x, int y,
		bool deposit, bool toggle, bool cycle, bool toggleMode,
		bool upgrade = false, bool cycleAccess = false,
		byte requestedKind = byte.MaxValue)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.PermanentFormationAction);
		packet.Write((short)x);
		packet.Write((short)y);
		packet.Write(deposit);
		packet.Write(toggle);
		packet.Write(cycle);
		packet.Write(toggleMode);
		packet.Write(upgrade);
		packet.Write(cycleAccess);
		packet.Write(requestedKind);
		packet.Send();
	}

	internal static void SendPermanentFormationTribulationHit(
		int entityId, int incomingDamage, int realmOffset)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.PermanentFormationTribulationHit);
		packet.Write(entityId);
		packet.Write(incomingDamage);
		packet.Write((byte)Math.Clamp(realmOffset, 0, 4));
		packet.Send();
	}

	internal static void SendFormationPathState(int playerIndex, int tier,
		int stage, int experience, int defenseCycles, int damageHandled,
		int tribulationStrikes, int veinsLinked,
		int toClient = -1, int ignoreClient = -1)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.FormationPathState);
		packet.Write((byte)playerIndex);
		packet.Write((byte)Math.Clamp(tier, 0, FormationPathPlayer.MaxTier));
		packet.Write((byte)Math.Clamp(stage, 0, FormationPathPlayer.MaxStage));
		packet.Write(Math.Max(0, experience));
		packet.Write(Math.Max(0, defenseCycles));
		packet.Write(Math.Max(0, damageHandled));
		packet.Write(Math.Max(0, tribulationStrikes));
		packet.Write(Math.Max(0, veinsLinked));
		packet.Send(toClient, ignoreClient);
	}

	internal static void SendPermanentFormationRelayAction(int x, int y, int mode)
	{
		ModPacket packet = ModContent.GetInstance<Xianxia>().GetPacket();
		packet.Write((byte)XianxiaMessageType.PermanentFormationRelayAction);
		packet.Write((short)x);
		packet.Write((short)y);
		packet.Write((byte)Math.Clamp(mode, 0, 3));
		packet.Send();
	}
}
