using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Xianxia.Common.Elements;

namespace Xianxia.Common.Players;

public enum SpiritualRootQuality : byte
{
	Mixed,
	True,
	Heavenly,
	Mutated,
	Primordial
}

/// <summary>
/// Permanent character-level Spiritual Root data. Roots are generated while
/// loading characters that do not have saved Root data, including characters
/// created before the system existed. Appraisal and multiplayer sync are
/// intentionally handled by later system layers.
/// </summary>
public class SpiritualRootPlayer : ModPlayer
{
	private const int CurrentDataVersion = 1;
	private const int AffinitySlots = SpiritualElementInfo.ElementCount;

	private static readonly SpiritualElement[] BasicElements =
	[
		SpiritualElement.Wood,
		SpiritualElement.Fire,
		SpiritualElement.Earth,
		SpiritualElement.Metal,
		SpiritualElement.Water
	];

	private static readonly SpiritualElement[] GeneratedMutatedElements =
	[
		SpiritualElement.Lightning,
		SpiritualElement.Ice,
		SpiritualElement.Wind
	];

	private readonly byte[] affinities = new byte[AffinitySlots];

	public bool HasSpiritualRoot { get; private set; }
	public bool IsRevealed { get; private set; }
	public SpiritualRootQuality Quality { get; private set; }
	public SpiritualElement Elements { get; private set; }
	public SpiritualElement PrimaryElement { get; private set; }
	public int Purity { get; private set; }
	public float CultivationGainBonusPercent => Quality switch
	{
		SpiritualRootQuality.Mixed => 5f + Purity * 0.03f,
		SpiritualRootQuality.True => ScalePurityBonus(70, 84, 10f, 14f),
		SpiritualRootQuality.Heavenly => ScalePurityBonus(85, 100, 18f, 25f),
		SpiritualRootQuality.Mutated => ScalePurityBonus(80, 96, 15f, 22f),
		SpiritualRootQuality.Primordial => 25f,
		_ => 0f
	};
	public float CultivationGainMultiplier =>
		1f + CultivationGainBonusPercent / 100f;
	public float BiomeMeditationBonusPercent =>
		TryGetBiomeMeditationResonance(out _, out float bonus, out _)
			? bonus : 0f;
	public float BiomeMeditationMultiplier =>
		1f + BiomeMeditationBonusPercent / 100f;
	public float BreakthroughChanceModifier => Quality switch
	{
		SpiritualRootQuality.Mixed => -8f + Purity * 0.05f,
		SpiritualRootQuality.True => ScalePurityBonus(70, 84, 0f, 4f),
		SpiritualRootQuality.Heavenly => ScalePurityBonus(85, 100, 10f, 18f),
		SpiritualRootQuality.Mutated => ScalePurityBonus(80, 96, 6f, 14f),
		SpiritualRootQuality.Primordial => 20f,
		_ => 0f
	};

	public override void Initialize()
	{
		ClearRoot();
	}

	public override void SaveData(TagCompound tag)
	{
		if (!HasSpiritualRoot)
			GenerateRoot(Main.rand);

		tag["spiritualRootVersion"] = CurrentDataVersion;
		tag["spiritualRootQuality"] = (byte)Quality;
		tag["spiritualRootElements"] = (int)Elements;
		tag["spiritualRootPrimary"] = (int)PrimaryElement;
		tag["spiritualRootPurity"] = Purity;
		tag["spiritualRootRevealed"] = IsRevealed;

		List<int> savedAffinities = new(AffinitySlots);
		for (int i = 0; i < AffinitySlots; i++)
			savedAffinities.Add(affinities[i]);
		tag["spiritualRootAffinities"] = savedAffinities;
	}

	public override void LoadData(TagCompound tag)
	{
		if (!tag.ContainsKey("spiritualRootVersion"))
		{
			GenerateRoot(Main.rand);
			return;
		}

		Quality = (SpiritualRootQuality)Math.Clamp(
			tag.GetByte("spiritualRootQuality"),
			(byte)SpiritualRootQuality.Mixed,
			(byte)SpiritualRootQuality.Primordial);
		Elements = (SpiritualElement)tag.GetInt("spiritualRootElements")
			& SpiritualElementInfo.AllElementMask;
		PrimaryElement = (SpiritualElement)tag.GetInt("spiritualRootPrimary");
		Purity = Math.Clamp(tag.GetInt("spiritualRootPurity"), 1, 100);
		IsRevealed = tag.GetBool("spiritualRootRevealed");

		Array.Clear(affinities);
		IList<int> savedAffinities = tag.GetList<int>("spiritualRootAffinities");
		for (int i = 0; i < AffinitySlots && i < savedAffinities.Count; i++)
			affinities[i] = (byte)Math.Clamp(savedAffinities[i], 0, 100);

		HasSpiritualRoot = IsValidSavedRoot();
		if (!HasSpiritualRoot)
			GenerateRoot(Main.rand);
	}

	public override void PostUpdateEquips()
	{
		if (!HasSpiritualRoot)
			return;

		ElementalCultivationPlayer elemental =
			Player.GetModPlayer<ElementalCultivationPlayer>();
		foreach (SpiritualElement element in Elements.Enumerate())
			elemental.SetAffinity(element, GetAffinity(element));

		(float power, float resistance, float qiCost, float mastery) = Quality switch
		{
			SpiritualRootQuality.Mixed => (3f, 2f, 0f, 3f),
			SpiritualRootQuality.True => (6f, 4f, 3f, 6f),
			SpiritualRootQuality.Heavenly => (10f, 6f, 6f, 10f),
			SpiritualRootQuality.Mutated => (9f, 5f, 5f, 8f),
			SpiritualRootQuality.Primordial => (8f, 8f, 5f, 8f),
			_ => (0f, 0f, 0f, 0f)
		};
		elemental.AddPower(Elements, power);
		elemental.AddResistance(Elements, resistance);
		elemental.AddQiCostReduction(Elements, qiCost);
		elemental.AddMasteryGain(Elements, mastery);

		float primaryAffinity = GetAffinity(PrimaryElement) / 100f;
		if (Elements.HasElement(SpiritualElement.Wood))
			Player.lifeRegen += Math.Max(1, (int)MathF.Round(2f * primaryAffinity));
		if (Elements.HasElement(SpiritualElement.Fire))
			Player.GetDamage(DamageClass.Generic) += 0.03f * primaryAffinity;
		if (Elements.HasElement(SpiritualElement.Earth))
			Player.statDefense += Math.Max(1, (int)MathF.Round(4f * primaryAffinity));
		if (Elements.HasElement(SpiritualElement.Metal))
			Player.GetCritChance(DamageClass.Generic) += 3f * primaryAffinity;
		if (Elements.HasElement(SpiritualElement.Water))
			Player.GetModPlayer<CultivationPlayer>().EquipmentMeditationQiBonus +=
				Math.Max(1, (int)MathF.Round(2f * primaryAffinity));
		if (Elements.HasElement(SpiritualElement.Lightning))
			Player.GetAttackSpeed(DamageClass.Generic) += 0.04f * primaryAffinity;
		if (Elements.HasElement(SpiritualElement.Ice))
			Player.endurance += 0.03f * primaryAffinity;
		if (Elements.HasElement(SpiritualElement.Wind))
			Player.moveSpeed += 0.06f * primaryAffinity;
	}

	public override void OnEnterWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
			SyncState();
	}

	public override void CopyClientState(ModPlayer targetCopy)
	{
		SpiritualRootPlayer clone = (SpiritualRootPlayer)targetCopy;
		clone.CopyFrom(this);
	}

	public override void SendClientChanges(ModPlayer clientPlayer)
	{
		if (!StateEquals((SpiritualRootPlayer)clientPlayer))
			SyncState();
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) =>
		global::Xianxia.Xianxia.SendSpiritualRootState(Player.whoAmI, this, toWho, fromWho);

	public bool RevealRoot()
	{
		if (!HasSpiritualRoot || IsRevealed)
			return false;
		IsRevealed = true;
		SyncState();
		return true;
	}

	internal byte[] GetAffinitySnapshot() => (byte[])affinities.Clone();

	internal void SetStateFromNetwork(SpiritualRootQuality quality,
		SpiritualElement elements, SpiritualElement primary, int purity,
		bool revealed, byte[] networkAffinities)
	{
		Quality = (SpiritualRootQuality)Math.Clamp((byte)quality,
			(byte)SpiritualRootQuality.Mixed, (byte)SpiritualRootQuality.Primordial);
		Elements = elements & SpiritualElementInfo.AllElementMask;
		PrimaryElement = primary;
		Purity = Math.Clamp(purity, 1, 100);
		IsRevealed = revealed;
		Array.Clear(affinities);
		for (int i = 0; i < affinities.Length && i < networkAffinities.Length; i++)
			affinities[i] = Math.Min(networkAffinities[i], (byte)100);
		HasSpiritualRoot = IsValidSavedRoot();
		if (!HasSpiritualRoot)
			GenerateRoot(Main.rand);
	}

	public void DebugRegenerate(bool reveal)
	{
		GenerateRoot(Main.rand);
		IsRevealed = reveal;
		SyncState();
	}

	public int GetAffinity(SpiritualElement element)
	{
		int index = SpiritualElementInfo.GetIndex(element);
		return index >= 0 ? affinities[index] : 0;
	}

	public float GetAverageAffinity(SpiritualElement elements)
	{
		float total = 0f;
		int count = 0;
		foreach (SpiritualElement element in elements.Enumerate())
		{
			total += GetAffinity(element);
			count++;
		}
		return count > 0 ? total / count : 0f;
	}

	public bool TryGetBiomeMeditationResonance(
		out SpiritualElement resonantElement,
		out float bonusPercent,
		out string biomeLocalizationKey)
	{
		resonantElement = SpiritualElement.None;
		bonusPercent = 0f;
		biomeLocalizationKey = string.Empty;
		if (!HasSpiritualRoot)
			return false;

		foreach (SpiritualElement element in Elements.Enumerate())
		{
			if (!TryGetElementBiome(element, out float maximumBonus,
				out string locationKey))
				continue;
			float scaledBonus = maximumBonus * GetAffinity(element) / 100f;
			if (scaledBonus <= bonusPercent)
				continue;
			resonantElement = element;
			bonusPercent = scaledBonus;
			biomeLocalizationKey = locationKey;
		}
		return resonantElement != SpiritualElement.None && bonusPercent > 0f;
	}

	private bool TryGetElementBiome(SpiritualElement element,
		out float maximumBonus, out string localizationKey)
	{
		maximumBonus = 15f;
		localizationKey = element switch
		{
			SpiritualElement.Wood when Player.ZoneJungle => "Jungle",
			SpiritualElement.Fire when Player.ZoneUnderworldHeight => "Underworld",
			SpiritualElement.Earth when Player.ZoneDirtLayerHeight
				|| Player.ZoneRockLayerHeight => "Underground",
			SpiritualElement.Metal when Player.ZoneDungeon => "Dungeon",
			SpiritualElement.Water when Player.ZoneBeach => "Ocean",
			SpiritualElement.Lightning when Main.raining => "Storm",
			SpiritualElement.Ice when Player.ZoneSnow => "Snow",
			SpiritualElement.Wind when Player.ZoneOverworldHeight
				&& Player.Center.Y / 16f < Main.worldSurface * 0.75f => "Sky",
			SpiritualElement.Void when Player.ZoneSkyHeight => "Space",
			_ => string.Empty
		};
		if (element == SpiritualElement.Earth)
			maximumBonus = 12f;
		return !string.IsNullOrEmpty(localizationKey);
	}

	public string GetQualityLocalizationKey() => Quality switch
	{
		SpiritualRootQuality.Mixed => "Mixed",
		SpiritualRootQuality.True => "True",
		SpiritualRootQuality.Heavenly => "Heavenly",
		SpiritualRootQuality.Mutated => "Mutated",
		SpiritualRootQuality.Primordial => "Primordial",
		_ => "Mixed"
	};

	private void GenerateRoot(UnifiedRandom random)
	{
		ClearRoot();
		int qualityRoll = random.Next(10_000);
		Quality = qualityRoll switch
		{
			< 4_500 => SpiritualRootQuality.Mixed,
			< 7_700 => SpiritualRootQuality.True,
			< 9_200 => SpiritualRootQuality.Heavenly,
			< 9_900 => SpiritualRootQuality.Mutated,
			_ => SpiritualRootQuality.Primordial
		};

		switch (Quality)
		{
			case SpiritualRootQuality.Mixed:
				GenerateMixedRoot(random);
				break;
			case SpiritualRootQuality.True:
				GenerateTrueRoot(random);
				break;
			case SpiritualRootQuality.Heavenly:
				GenerateSingleRoot(random, BasicElements, 85, 101);
				break;
			case SpiritualRootQuality.Mutated:
				GenerateSingleRoot(random, GeneratedMutatedElements, 80, 97);
				break;
			case SpiritualRootQuality.Primordial:
				GeneratePrimordialRoot(random);
				break;
		}

		HasSpiritualRoot = true;
		IsRevealed = false;
	}

	private void GenerateMixedRoot(UnifiedRandom random)
	{
		int elementCount = random.NextBool(2) ? 2 : 3;
		SpiritualElement[] selected = SelectDistinctElements(
			random, BasicElements, elementCount);
		int[] weights = new int[elementCount];
		int totalWeight = 0;
		for (int i = 0; i < elementCount; i++)
		{
			weights[i] = random.Next(22, 51);
			totalWeight += weights[i];
		}

		int assigned = 0;
		for (int i = 0; i < elementCount; i++)
		{
			int value = i == elementCount - 1
				? 100 - assigned
				: Math.Max(1, (int)MathF.Round(weights[i] * 100f / totalWeight));
			assigned += value;
			SetGeneratedAffinity(selected[i], value);
		}
		FinalizeGeneratedRoot();
	}

	private void GenerateTrueRoot(UnifiedRandom random)
	{
		SpiritualElement[] selected = SelectDistinctElements(random, BasicElements, 2);
		int primaryAffinity = random.Next(70, 85);
		SetGeneratedAffinity(selected[0], primaryAffinity);
		SetGeneratedAffinity(selected[1], 100 - primaryAffinity);
		FinalizeGeneratedRoot();
	}

	private void GenerateSingleRoot(
		UnifiedRandom random,
		SpiritualElement[] pool,
		int minimumAffinity,
		int maximumAffinityExclusive)
	{
		SpiritualElement element = pool[random.Next(pool.Length)];
		SetGeneratedAffinity(element,
			random.Next(minimumAffinity, maximumAffinityExclusive));
		FinalizeGeneratedRoot();
	}

	private void GeneratePrimordialRoot(UnifiedRandom random)
	{
		foreach (SpiritualElement element in BasicElements)
			SetGeneratedAffinity(element, 100);
		PrimaryElement = BasicElements[random.Next(BasicElements.Length)];
		Elements = SpiritualElementInfo.BasicElementMask;
		Purity = 100;
	}

	private void FinalizeGeneratedRoot()
	{
		Elements = SpiritualElement.None;
		PrimaryElement = SpiritualElement.None;
		Purity = 0;
		for (int i = 0; i < AffinitySlots; i++)
		{
			if (affinities[i] <= 0)
				continue;

			SpiritualElement element = SpiritualElementInfo.Elements[i];
			Elements |= element;
			if (affinities[i] > Purity)
			{
				Purity = affinities[i];
				PrimaryElement = element;
			}
		}
	}

	private void SetGeneratedAffinity(SpiritualElement element, int value)
	{
		int index = SpiritualElementInfo.GetIndex(element);
		if (index >= 0)
			affinities[index] = (byte)Math.Clamp(value, 0, 100);
	}

	private bool IsValidSavedRoot()
	{
		if (Elements == SpiritualElement.None
			|| !PrimaryElement.IsSingleElement()
			|| !Elements.HasElement(PrimaryElement)
			|| Purity is < 1 or > 100)
			return false;

		int highestAffinity = 0;
		foreach (SpiritualElement element in Elements.Enumerate())
			highestAffinity = Math.Max(highestAffinity, GetAffinity(element));
		return highestAffinity > 0 && GetAffinity(PrimaryElement) == highestAffinity;
	}

	private static SpiritualElement[] SelectDistinctElements(
		UnifiedRandom random,
		SpiritualElement[] pool,
		int count)
	{
		SpiritualElement[] shuffled = (SpiritualElement[])pool.Clone();
		for (int i = shuffled.Length - 1; i > 0; i--)
		{
			int swapIndex = random.Next(i + 1);
			(shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
		}

		SpiritualElement[] result = new SpiritualElement[count];
		Array.Copy(shuffled, result, count);
		return result;
	}

	private void ClearRoot()
	{
		HasSpiritualRoot = false;
		IsRevealed = false;
		Quality = SpiritualRootQuality.Mixed;
		Elements = SpiritualElement.None;
		PrimaryElement = SpiritualElement.None;
		Purity = 0;
		Array.Clear(affinities);
	}

	private float ScalePurityBonus(int minimumPurity, int maximumPurity,
		float minimumBonus, float maximumBonus)
	{
		float progress = Math.Clamp(
			(Purity - minimumPurity) / (float)(maximumPurity - minimumPurity),
			0f, 1f);
		return minimumBonus + (maximumBonus - minimumBonus) * progress;
	}

	private void SyncState()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			global::Xianxia.Xianxia.SendSpiritualRootState(Player.whoAmI, this);
	}

	private void CopyFrom(SpiritualRootPlayer source)
	{
		HasSpiritualRoot = source.HasSpiritualRoot;
		IsRevealed = source.IsRevealed;
		Quality = source.Quality;
		Elements = source.Elements;
		PrimaryElement = source.PrimaryElement;
		Purity = source.Purity;
		Array.Copy(source.affinities, affinities, affinities.Length);
	}

	private bool StateEquals(SpiritualRootPlayer other)
	{
		if (HasSpiritualRoot != other.HasSpiritualRoot
			|| IsRevealed != other.IsRevealed
			|| Quality != other.Quality
			|| Elements != other.Elements
			|| PrimaryElement != other.PrimaryElement
			|| Purity != other.Purity)
			return false;
		for (int i = 0; i < affinities.Length; i++)
			if (affinities[i] != other.affinities[i])
				return false;
		return true;
	}
}
