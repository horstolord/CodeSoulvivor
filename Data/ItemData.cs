namespace Sandbox.Code.Data;

public enum ItemCategory
{
	Equipment,
	Consumable,
	Material,
	KeyItem
}
public enum ItemRarity
{
	Common,
	Magic,
	Rare,
	Epic,
	Legendary,
	Mythical,
	Divine
}
public enum EquipmentSlot
{
	None,
	Head,
	Chest,
	Hands,
	Belt,
	Feet,
	MainHand1,
	OffHand1,
	MainHand2,
	OffHand2,
	MainHand3,
	OffHand3,
	Ring1,
	Ring2,
	Amulet
}

public static class ItemData
{
	public static ItemDef rustySword = new ItemDef
	{
		Id = "rusty_sword",
		Name = "Rusty Sword",
		Description = "An old blade, worn but still sharp.",
		Category = ItemCategory.Equipment,
		Rarity = ItemRarity.Common,
		Stackable = false,
		Tags = new() { "weapon", "sword", "metal", "starter-gear", "tier1" },
		Equipment = new EquipmentData
		{
			Slot = EquipmentSlot.MainHand1,
			Stats = new EquipmentStatBlock
			{
				BaseDamage = 14,
				BaseAttackSpeed = 2.0f,
				PoiseDamage = 6,
				Weight = 4
			}
		},
		Mods = new()
		{
			new ModData("Might", 1, ModifierType.Flat)
		}
	};
	public static ItemDef tatteredHood = new ItemDef
	{
		Id = "tattered_hood",
		Name = "Tattered Hood",
		Description = "A frayed hood for concealing one's misery.",
		Category = ItemCategory.Equipment,
		Rarity = ItemRarity.Common,
		Stackable = false,
		Tags = new() { "armor", "cloth", "light-armor", "starter-gear", "tier1" },
		Equipment = new EquipmentData
		{
			Slot = EquipmentSlot.Head,
			Stats = new EquipmentStatBlock
			{
				Weight = 1,
				Armor = 75f
			}
		},
		Mods = new()
		{
			new ModData("Will", 1, ModifierType.Flat),
			new ModData("MaxEnergy", 10, ModifierType.Flat)
		}
	};
	public static ItemDef healingFlask = new ItemDef
	{
		Id = "minor_healing_flask",
		Name = "Minor Healing Flask",
		Description = "Restores a small amount of health.",
		Category = ItemCategory.Consumable,
		Rarity = ItemRarity.Common,
		Stackable = true,
		MaxStack = 5,
		Tags = new() { "consumable", "healing", "flask", "tier1" },
		Consumable = new ConsumableData
		{
			Charges = 1,
			UseEffectId = "heal_small"
		}
	};
	public static ItemDef ironOre = new ItemDef
	{
		Id = "iron_ore",
		Name = "Iron Ore",
		Description = "Raw ore used in forging.",
		Category = ItemCategory.Material,
		Rarity = ItemRarity.Common,
		Stackable = true,
		MaxStack = 99,
		Tags = new() { "material", "ore", "metal" },
		Crafting = new CraftingData
		{
			MaterialValue = 1,
			MaterialTags = new() { "metal", "ore", "tier1" }
		}
	};
}
