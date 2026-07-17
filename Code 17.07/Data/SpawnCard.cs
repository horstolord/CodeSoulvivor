namespace Sandbox.Code.Data;

/// <summary>
/// Defines a spawnable mob type and its cost to the SpawnDirector.
/// </summary>
public class SpawnCard
{
	/// <summary>Key into MobRegistry — also set as Enemy.PresetOverride on spawn.</summary>
	public string MobPresetId   { get; set; }
	/// <summary>Director credit cost to spawn one of these.</summary>
	public float  Cost          { get; set; }
	/// <summary>Relative probability weight vs. other affordable cards.</summary>
	public float  Weight        { get; set; } = 1f;
	/// <summary>Minimum difficulty value before this card becomes available.</summary>
	public float  MinDifficulty { get; set; } = 0f;
	public string DisplayName   { get; set; }
}

/// <summary>
/// All cards available to the SpawnDirector, defined in code.
/// Add new mob types here as they are created.
/// </summary>
public static class SpawnCardRegistry
{
	public static List<SpawnCard> All { get; } = new()
	{
		new SpawnCard
		{
			MobPresetId   = "goblin",
			DisplayName   = "Goblin Scout",
			Cost          = 10f,
			Weight        = 3f,
			MinDifficulty = 0f,
		},
		new SpawnCard
		{
			MobPresetId   = "orc",
			DisplayName   = "Orc Berserker",
			Cost          = 10f,
			Weight        = 3f,
			MinDifficulty = 0f,  // unlocks ~5 minutes in
		},
	};
}
