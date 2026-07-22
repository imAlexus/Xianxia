using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Xianxia.Common.Players;

namespace Xianxia;

internal enum XianxiaMessageType : byte
{
	MeditationState,
	BreakthroughEffect,
	AlchemyState
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

	public override void Load()
	{
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
	}

	public override void Unload()
	{
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
}
