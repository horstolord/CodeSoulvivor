using System.Collections.Generic;
using System.Linq;
using Sandbox.Code.Data;

namespace Sandbox.Code.Presentation;

/// <summary>
/// Temporary UI-facing inventory state until the gameplay inventory system owns this data.
/// </summary>
public sealed class InventoryPanel
{
    public const int Columns = 9;
    public const int Rows = 4;

    public List<InventorySlot> Slots { get; private set; } = new();
    public Dictionary<EquipmentSlot, InventoryItem> EquippedSlots { get; private set; } = new();

    public InventoryItem HoveredItem { get; private set; }
    public int? SelectedSlotId { get; private set; }
    private int? draggingSlotId;

    public int UsedSlots => Slots.Count( s => s.Item != null );
    public int TotalSlots => Slots.Count;
    public int Revision { get; private set; }
    public int TotalAttack => EquippedSlots.Values.Sum( item => (int)(item.Definition?.Equipment?.Stats?.BaseDamage ?? item.Stats.Attack) );
    public int TotalDefense => EquippedSlots.Values.Sum( item => (int)(item.Definition?.Equipment?.Stats?.Armor ?? item.Stats.Defense) );
    public int TotalSpeed => EquippedSlots.Values.Sum( item => item.Stats.Speed );

    public InventoryPanel()
    {
        InitializeGrid();
        SeedStarterItems();
    }

    public InventoryItem GetEquippedItem( EquipmentSlot slot )
    {
        EquippedSlots.TryGetValue( NormalizeSlot( slot ), out var item );
        return item;
    }

    public void OnSlotMouseDown( InventorySlot slot )
    {
        if ( slot.IsLocked ) return;
        SelectedSlotId = slot.Id;
        draggingSlotId = slot.Id;
    }

    public void OnSlotMouseUp( InventorySlot targetSlot )
    {
        if ( draggingSlotId.HasValue && draggingSlotId.Value != targetSlot.Id )
        {
            SwapSlots( draggingSlotId.Value, targetSlot.Id );
        }

        draggingSlotId = null;
    }

    public void SetHoveredItem( InventoryItem item )
    {
        HoveredItem = item;
        Revision++;
    }

    public void SwapSlots( int fromId, int toId )
    {
        var fromSlot = Slots.FirstOrDefault( s => s.Id == fromId );
        var toSlot = Slots.FirstOrDefault( s => s.Id == toId );

        if ( fromSlot == null || toSlot == null || toSlot.IsLocked ) return;

        (fromSlot.Item, toSlot.Item) = (toSlot.Item, fromSlot.Item);
        Revision++;
    }

    public void EquipItem( InventorySlot slot )
    {
        if ( slot?.Item?.Definition?.IsEquippable != true ) return;

        var equipmentSlot = NormalizeSlot( slot.Item.Definition.Equipment.Slot );
        EquipItemInSlot( slot, equipmentSlot );
    }

    public void EquipSelectedItemTo( EquipmentSlot targetSlot )
    {
        if ( !SelectedSlotId.HasValue ) return;

        var slot = Slots.FirstOrDefault( s => s.Id == SelectedSlotId.Value );
        if ( slot?.Item?.Definition?.IsEquippable != true ) return;

        var normalizedTarget = NormalizeSlot( targetSlot );
        var itemSlot = NormalizeSlot( slot.Item.Definition.Equipment.Slot );
        if ( normalizedTarget != itemSlot ) return;

        EquipItemInSlot( slot, normalizedTarget );
    }

    public void Unequip( EquipmentSlot equipmentSlot )
    {
        var normalizedSlot = NormalizeSlot( equipmentSlot );
        if ( !EquippedSlots.TryGetValue( normalizedSlot, out var item ) ) return;

        var emptySlot = Slots.FirstOrDefault( s => s.Item == null && !s.IsLocked );
        if ( emptySlot == null ) return;

        emptySlot.Item = item;
        EquippedSlots.Remove( normalizedSlot );

        // Strip stat modifiers from the gameplay layer
        Sandbox.Code.Actors.Player.Local?.Equipment?.Unequip( normalizedSlot );

        Revision++;
    }

    private void EquipItemInSlot( InventorySlot inventorySlot, EquipmentSlot equipmentSlot )
    {
        var itemToEquip = inventorySlot.Item;
        if ( itemToEquip == null ) return;

        if ( EquippedSlots.TryGetValue( equipmentSlot, out var equippedItem ) )
        {
            inventorySlot.Item = equippedItem;
        }
        else
        {
            inventorySlot.Item = null;
        }

        EquippedSlots[equipmentSlot] = itemToEquip;

        // Forward to the gameplay EquipmentControl so stat modifiers are actually applied
        var def = itemToEquip.Definition;
        Log.Info( $"[Inventory] Equipping '{def?.Name}' — Definition.Stats.Armor={def?.Equipment?.Stats?.Armor:F1}, Mods={def?.Mods?.Count ?? 0}" );
        Actors.Player.Local?.Equipment?.Equip( def );

        SelectedSlotId = null;
        draggingSlotId = null;
        Revision++;
    }

    private void InitializeGrid()
    {
        Slots.Clear();

        int count = Columns * Rows;
        for ( int i = 0; i < count; i++ )
        {
            Slots.Add( new InventorySlot { Id = i, IsLocked = false } );
        }
    }

    private void SeedStarterItems()
    {
        SetSlot( 0, ItemData.rustySword );
        SetSlot( 1, ItemData.tatteredHood );
        SetSlot( 2, ItemData.healingFlask, 3 );
        SetSlot( 3, ItemData.ironOre, 12 );
    }

    private void SetSlot( int index, ItemDef definition, int quantity = 1 )
    {
        if ( index < 0 || index >= Slots.Count || definition == null ) return;

        Slots[index].Item = InventoryItem.FromDefinition( definition, quantity );
    }

    private static EquipmentSlot NormalizeSlot( EquipmentSlot slot )
    {
        return slot switch
        {
            EquipmentSlot.MainHand2 or EquipmentSlot.MainHand3 => EquipmentSlot.MainHand1,
            EquipmentSlot.OffHand2 or EquipmentSlot.OffHand3 => EquipmentSlot.OffHand1,
            _ => slot
        };
    }
}

public sealed class InventorySlot
{
    public int Id { get; init; }
    public bool IsLocked { get; init; }
    public InventoryItem Item { get; set; }
}

public sealed class InventoryItem
{
    public ItemDef Definition { get; init; }
    public string Name => Definition?.Name ?? "Unknown Item";
    public string Description => Definition?.Description ?? "";
    public ItemRarity Rarity => Definition?.Rarity ?? ItemRarity.Common;
    public string RarityClass => Rarity.ToString().ToLower();
    public int Quantity { get; init; } = 1;
    public InventoryItemStats Stats { get; init; } = new();
    public string IconGlyph { get; init; } = "?";

    public static InventoryItem FromDefinition( ItemDef definition, int quantity = 1 )
    {
        var stats = definition?.Equipment?.Stats;

        return new InventoryItem
        {
            Definition = definition,
            Quantity = quantity,
            IconGlyph = GetIconGlyph( definition ),
            Stats = new InventoryItemStats
            {
                Attack = (int)(stats?.BaseDamage ?? 0f),
                Defense = (int)(stats?.Armor ?? 0f),
                Speed = (int)(definition?.Mods?.Where( mod => mod.StatName == "Swiftness" ).Sum( mod => mod.Value ) ?? 0f)
            }
        };
    }

    private static string GetIconGlyph( ItemDef definition )
    {
        if ( definition == null ) return "?";

        if ( definition.Tags.Contains( "sword" ) ) return "SW";
        if ( definition.Tags.Contains( "armor" ) || definition.Equipment?.Slot == EquipmentSlot.Head ) return "HD";
        if ( definition.Tags.Contains( "healing" ) ) return "HP";
        if ( definition.Tags.Contains( "ore" ) ) return "OR";

        return definition.Category switch
        {
            ItemCategory.Equipment => "EQ",
            ItemCategory.Consumable => "CO",
            ItemCategory.Material => "MA",
            _ => "IT"
        };
    }
}

public sealed class InventoryItemStats
{
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int Speed { get; init; }
}
