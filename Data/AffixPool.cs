using System.Collections.Generic;

namespace Sandbox.Code.Data;

public static class AffixPool
{
	public static List<AffixDef> All { get; } = new()
	{
		// Might — weapon-only, scales melee damage via AttackData's MightToHealthDamage scaling
		new AffixDef { Id = "might_t1", GroupId = "might_damage", StatName = "Might", Tier = 1, MinLevel = 1,  MinValue = 1f, MaxValue = 2f, Weight = 10f, RequiredTags = { "weapon" } },
		new AffixDef { Id = "might_t2", GroupId = "might_damage", StatName = "Might", Tier = 2, MinLevel = 15, MinValue = 3f, MaxValue = 5f, Weight = 6f,  RequiredTags = { "weapon" } },
		new AffixDef { Id = "might_t3", GroupId = "might_damage", StatName = "Might", Tier = 3, MinLevel = 50, MinValue = 6f, MaxValue = 9f, Weight = 2f,  RequiredTags = { "weapon" } },

		// Crit chance — weapon-only
		new AffixDef { Id = "crit_t1", GroupId = "crit_chance", StatName = "CritChance", Tier = 1, MinLevel = 1,  MinValue = 2f, MaxValue = 4f,  Weight = 10f, RequiredTags = { "weapon" } },
		new AffixDef { Id = "crit_t2", GroupId = "crit_chance", StatName = "CritChance", Tier = 2, MinLevel = 20, MinValue = 5f, MaxValue = 8f,  Weight = 6f,  RequiredTags = { "weapon" } },
		new AffixDef { Id = "crit_t3", GroupId = "crit_chance", StatName = "CritChance", Tier = 3, MinLevel = 45, MinValue = 9f, MaxValue = 14f, Weight = 2f,  RequiredTags = { "weapon" } },

		// Armor — armor-only
		new AffixDef { Id = "armor_t1", GroupId = "armor_flat", StatName = "Armor", Tier = 1, MinLevel = 1,  MinValue = 3f,  MaxValue = 6f,  Weight = 10f, RequiredTags = { "armor" } },
		new AffixDef { Id = "armor_t2", GroupId = "armor_flat", StatName = "Armor", Tier = 2, MinLevel = 15, MinValue = 7f,  MaxValue = 12f, Weight = 6f,  RequiredTags = { "armor" } },
		new AffixDef { Id = "armor_t3", GroupId = "armor_flat", StatName = "Armor", Tier = 3, MinLevel = 35, MinValue = 13f, MaxValue = 20f, Weight = 2f,  RequiredTags = { "armor" } },

		// Endurance (vitality) — universal
		new AffixDef { Id = "vit_t1", GroupId = "vitality", StatName = "Endurance", Tier = 1, MinLevel = 1,  MinValue = 1f, MaxValue = 2f, Weight = 10f },
		new AffixDef { Id = "vit_t2", GroupId = "vitality", StatName = "Endurance", Tier = 2, MinLevel = 20, MinValue = 3f, MaxValue = 5f, Weight = 6f },
		new AffixDef { Id = "vit_t3", GroupId = "vitality", StatName = "Endurance", Tier = 3, MinLevel = 40, MinValue = 6f, MaxValue = 8f, Weight = 2f },

		// Move speed — universal
		new AffixDef { Id = "movespeed_t1", GroupId = "move_speed", StatName = "MoveSpeed", Tier = 1, MinLevel = 1,  MinValue = 5f,  MaxValue = 10f, Weight = 8f },
		new AffixDef { Id = "movespeed_t2", GroupId = "move_speed", StatName = "MoveSpeed", Tier = 2, MinLevel = 25, MinValue = 11f, MaxValue = 18f, Weight = 4f },
	};
}
