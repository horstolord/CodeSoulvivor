using System.Collections.Generic;

namespace Sandbox.Code.Data;

public class LootDropEntry
{
	public string ItemId;
	public float Weight = 1f;
	public int MinLevel = 1;
}

public static class LootDropRegistry
{
	public static List<LootDropEntry> All { get; } = new()
	{
		new LootDropEntry { ItemId = "shortsword", Weight = 5f, MinLevel = 1 },
		new LootDropEntry { ItemId = "leather_cap", Weight = 5f, MinLevel = 1 },
	};

	public static ItemDef Resolve( string itemId ) => itemId switch
	{
		"shortsword" => ItemData.shortsword,
		"leather_cap" => ItemData.leatherCap,
		_ => null
	};
}
