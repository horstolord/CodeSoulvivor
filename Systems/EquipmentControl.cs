using System.Collections.Generic;
using Sandbox.Code.Actors;
using Sandbox.Code.Data;

namespace Sandbox.Code.Systems;

/// <summary>
/// Manages an actor's equipped items.
/// Applying an item adds its Mods to the StatSheet as tracked StatModifiers.
/// Removing it strips those same modifiers back off cleanly.
/// </summary>
public class EquipmentControl : Component
{
	// Key = normalized equipment slot, Value = equipped item + its applied modifiers
	private readonly Dictionary<EquipmentSlot, EquippedEntry> _slots = new();

	private sealed class EquippedEntry
	{
		public ItemDef Item;
		public List<(Stat Stat, StatModifier Modifier)> AppliedModifiers = new();
	}

	// ============ PUBLIC API ============

	public ItemDef GetEquippedItem( EquipmentSlot slot ) =>
		_slots.TryGetValue( NormalizeSlot( slot ), out var entry ) ? entry.Item : null;

	public ItemDef GetEquippedWeapon() => GetEquippedItem( EquipmentSlot.MainHand1 );

	/// <summary>
	/// Builds a live AttackDef from the currently equipped main-hand weapon.
	/// Returns null if nothing is equipped (caller should fall back to unarmed).
	/// </summary>
	public AttackDef GetWeaponAttackDef()
	{
		var weapon = GetEquippedWeapon();
		if ( weapon?.Equipment?.Stats == null )
			return null;

		return AttackData.BuildWeaponAttack( weapon );
	}

	/// <summary>
	/// Equip an item. Automatically unequips whatever was in that slot first.
	/// </summary>
	public void Equip( ItemDef item )
	{
		if ( item?.Equipment == null ) return;

		var statSheet = GameObject.Components.Get<StatSheet>( FindMode.EverythingInSelfAndAncestors );
		if ( statSheet == null )
		{
			Log.Warning( $"[EquipmentControl] No StatSheet found on {GameObject.Name}" );
			return;
		}

		var slot = NormalizeSlot( item.Equipment.Slot );

		// Remove previous item in this slot first
		if ( _slots.ContainsKey( slot ) )
			UnequipInternal( slot, statSheet );

		// Apply all Mods from the new item
		var entry = new EquippedEntry { Item = item };

		foreach ( var mod in item.Mods )
		{
			var stat = statSheet.GetStat( mod.StatName );
			if ( stat == null )
			{
				Log.Warning( $"[EquipmentControl] Item '{item.Name}' references unknown stat '{mod.StatName}'" );
				continue;
			}
			var modifier = new StatModifier( mod.Value, mod.Type, source: entry );
			stat.AddModifier( modifier );
			entry.AppliedModifiers.Add( (stat, modifier) );
		}

		// Apply weapon AttackSpeed to the AttackSpeed stat
		// BaseAttackSpeed 1.0 = neutral, 1.2 = 20% faster → +20 flat to AttackSpeed stat
		if ( item.Equipment.Stats.BaseAttackSpeed != 0f && item.Equipment.Stats.BaseAttackSpeed != 1f )
		{
			var attackSpeedStat = statSheet.GetStat( "AttackSpeed" );
			if ( attackSpeedStat != null )
			{
				float delta = (item.Equipment.Stats.BaseAttackSpeed - 1f) * 100f;
				var modifier = new StatModifier( delta, ModifierType.Flat, source: entry );
				attackSpeedStat.AddModifier( modifier );
				entry.AppliedModifiers.Add( (attackSpeedStat, modifier) );
			}
		}

		_slots[slot] = entry;

		// Recalculate derived stats if any attributes were modified
		bool touchedAttribute = item.Mods.Any( m => IsAttributeStat( m.StatName ) );
		if ( touchedAttribute )
			statSheet.RecalculateDerivedStats();

		Log.Info( $"[EquipmentControl] Equipped '{item.Name}' in slot {slot}. Applied {entry.AppliedModifiers.Count} modifier(s)." );
	}

	/// <summary>
	/// Unequip the item currently in the given slot, stripping its modifiers.
	/// </summary>
	public void Unequip( EquipmentSlot slot )
	{
		var normalized = NormalizeSlot( slot );
		if ( !_slots.ContainsKey( normalized ) ) return;

		var statSheet = GameObject.Components.Get<StatSheet>( FindMode.EverythingInSelfAndAncestors );
		UnequipInternal( normalized, statSheet );

		if ( statSheet != null )
			statSheet.RecalculateDerivedStats();
	}

	// ============ INTERNALS ============

	private void UnequipInternal( EquipmentSlot slot, StatSheet statSheet )
	{
		if ( !_slots.TryGetValue( slot, out var entry ) ) return;

		foreach ( var (stat, modifier) in entry.AppliedModifiers )
			stat.RemoveModifier( modifier );

		_slots.Remove( slot );
		Log.Info( $"[EquipmentControl] Unequipped '{entry.Item.Name}' from slot {slot}." );
	}

	private static bool IsAttributeStat( string name ) =>
		name is "Might" or "Swiftness" or "Endurance" or "Will" or "Acuity" or "Wisdom";

	private static EquipmentSlot NormalizeSlot( EquipmentSlot slot ) => slot switch
	{
		EquipmentSlot.MainHand2 or EquipmentSlot.MainHand3 => EquipmentSlot.MainHand1,
		EquipmentSlot.OffHand2  or EquipmentSlot.OffHand3  => EquipmentSlot.OffHand1,
		_ => slot
	};
}
