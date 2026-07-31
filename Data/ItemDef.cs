namespace Sandbox.Code.Data;

public sealed class ItemDef
{
	public string Id { get; init; }
	public string Name { get; init; }
	public string Description { get; init; }
	public ItemCategory Category { get; init; }
	public ItemRarity Rarity { get; init; }
	public bool Stackable { get; init; }
	public int MaxStack { get; init; } = 1;
	public List<string> Tags { get; init; } = new();
	public List<ModData> Mods { get; init; } = new();
	public EquipmentData Equipment { get; init; }
	public ConsumableData Consumable { get; init; }
	public CraftingData Crafting { get; init; }
	public bool IsEquippable => Equipment != null;
	public bool IsConsumable => Consumable != null;
	public bool IsCraftingMaterial => Crafting != null;
}
public sealed class EquipmentData
{
	public EquipmentSlot Slot { get; init; }
	public EquipmentStatBlock Stats { get; init; } = new();
}
public sealed class EquipmentStatBlock
{
	public float BaseDamage { get; init; }
	public float BaseAttackSpeed { get; init; }
	public float GuardValue { get; init; }
	public float PoiseDamage { get; init; }
	
	public float BaseDefense {get; init; }
	
	//public List<MaterialData> Material { get; init; }
	public float Quality { get; init; }
	public float Weight { get; init; }
	
}
public sealed class ConsumableData
{
	public int Charges { get; init; } = 1;
	public string UseEffectId { get; init; }
}
public sealed class CraftingData
{
	public int MaterialValue { get; init; } = 1;
	public List<string> MaterialTags { get; init; } = new();
}
public sealed class ItemInstance
{
	public string InstanceId { get; init; }
	public ItemDef Definition { get; init; }
	public int StackCount { get; set; } = 1;
	public int RemainingCharges { get; set; }
}
