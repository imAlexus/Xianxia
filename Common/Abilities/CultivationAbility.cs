using Xianxia.Common.Elements;

namespace Xianxia.Common.Abilities;

public enum CultivationAbility
{
	Meditation,
	QiSense,
	QiResistance,
	Fireball,
	QiPalm,
	QiProtection,
	FlameStep,
	QiFlight,
	NascentTeleport,
	SpiritualPressure,
	SpiritBreathing,
	GoldenCoreCirculation,
	NascentSoulRegeneration,
	// Keep new abilities at the end so existing saved ability EXP remains mapped
	// to the same enum values.
	NightVision,
	SwordIntent,
	SpiritSwordRain,
	SectProtectionFormation,
	SpiritualRain,
	QiBurning,
	Count
}

public static class CultivationAbilityInfo
{
	public const int MaxLevel = 20;

	public static bool IsTechniqueLoadoutAbility(
		CultivationAbility ability) => ability is
		CultivationAbility.QiResistance
		or CultivationAbility.Fireball
		or CultivationAbility.QiPalm
		or CultivationAbility.SpiritualRain
		or CultivationAbility.FlameStep
		or CultivationAbility.SpiritSwordRain
		or CultivationAbility.NascentTeleport;

	public static bool IsToggleTechnique(
		CultivationAbility ability) => ability is
		CultivationAbility.QiSense
		or CultivationAbility.QiProtection
		or CultivationAbility.QiBurning
		or CultivationAbility.NightVision
		or CultivationAbility.QiFlight
		or CultivationAbility.SectProtectionFormation
		or CultivationAbility.SpiritualPressure;

	public static int RequiredRealm(CultivationAbility ability) => ability switch
	{
		CultivationAbility.Meditation or CultivationAbility.SpiritBreathing => 0,
		CultivationAbility.QiSense or CultivationAbility.QiResistance
			or CultivationAbility.Fireball or CultivationAbility.QiPalm => 1,
		CultivationAbility.QiProtection or CultivationAbility.FlameStep
			or CultivationAbility.NightVision or CultivationAbility.QiBurning => 2,
		CultivationAbility.QiFlight or CultivationAbility.GoldenCoreCirculation => 3,
		CultivationAbility.NascentTeleport or CultivationAbility.SpiritualPressure
			or CultivationAbility.NascentSoulRegeneration => 4,
		CultivationAbility.SwordIntent => 1,
		CultivationAbility.SpiritSwordRain => 2,
		CultivationAbility.SectProtectionFormation => 3,
		CultivationAbility.SpiritualRain => 1,
		_ => 99
	};

	public static int ExperienceForNextLevel(int level) => 50 * level * level;

	public static SpiritualElement GetSpiritualElements(CultivationAbility ability) =>
		ability switch
		{
			CultivationAbility.Fireball => SpiritualElement.Fire,
			CultivationAbility.SpiritualRain =>
				SpiritualElement.Water | SpiritualElement.Wood,
			CultivationAbility.SpiritSwordRain => SpiritualElement.Metal,
			CultivationAbility.QiProtection => SpiritualElement.Earth,
			CultivationAbility.FlameStep =>
				SpiritualElement.Fire | SpiritualElement.Wind,
			CultivationAbility.QiFlight => SpiritualElement.Wind,
			CultivationAbility.NascentTeleport => SpiritualElement.Void,
			CultivationAbility.QiSense => SpiritualElement.Water,
			CultivationAbility.QiBurning =>
				SpiritualElement.Fire | SpiritualElement.Void,
			_ => SpiritualElement.None
		};
}
