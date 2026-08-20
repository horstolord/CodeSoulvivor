
using Sandbox.Citizen;
using Sandbox.Code.Data;
using Sandbox.Code.Systems;
using HudPanel = Sandbox.Code.Presentation.UI;
namespace Sandbox.Code.Actors;
public sealed class Player : Actor
{
	
	public static Player Local { get; private set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	public SlideControl Slide { get; private set; }
	private PlayerController _playerController;
	
	// Load the "player" / hero stat preset from MobRegistry
	protected override string GetMobPresetId() => "player";
	
	protected override void OnStart()
	{
		base.OnStart();
		MobRegistry.Initialize();
		Local = this;
		Combat = GameObject.Components.Get<CombatComponent>();
		Slide = Components.GetOrCreate<SlideControl>();
		_playerController = GameObject.Components.GetInAncestorsOrSelf<PlayerController>(  );
		BodyRenderer ??= Components.GetInChildren<SkinnedModelRenderer>();
		if ( BodyRenderer is null )
			Log.Warning( $"No BodyRenderer found on {GameObject.Name}" );
		foreach ( var r in Components.GetAll<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
		{
			Log.Info( $"Found renderer: {r.GameObject.Name}" );
		}
		Log.Info( $"Assigned renderer: {BodyRenderer?.GameObject?.Name ?? "NULL"}" );
		
	}
	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdatePlayerMovementStats();
		HandleFlaskHotkeys();
		if ( Combat.CanAttack )
		{
			HandleCombatInput();
		}
	}
	
	private void HandleFlaskHotkeys()
	{
		if ( Input.Keyboard.Pressed( "1" ) )
		{
			Presentation.UI.LocalInventory?.UseFlask( EquipmentSlot.Flask1 );
		}
		if ( Input.Keyboard.Pressed( "2" ) )
		{
			Presentation.UI.LocalInventory?.UseFlask( EquipmentSlot.Flask2 );
		}
	}

	public void OnEnemyKilled( Actor enemy )
	{
		Log.Info( $"[Player] Enemy slain: {enemy.GameObject.Name}. Refilling flasks!" );
		Presentation.UI.LocalInventory?.RefillFlasks( 1 );
	}

	public void ApplyFlaskEffect( ItemDef flaskDef )
	{
		if ( flaskDef?.Consumable == null || StatSheet == null ) return;

		var consumable = flaskDef.Consumable;
		if ( consumable.RestoreHealth > 0f )
		{
			float maxHp = StatSheet.MaxHealth?.Value ?? 100f;
			StatSheet.CurrentHealth = System.MathF.Min( maxHp, StatSheet.CurrentHealth + consumable.RestoreHealth );
			Log.Info( $"[Flask] Restored {consumable.RestoreHealth} HP -> {StatSheet.CurrentHealth}/{maxHp}" );
		}

		if ( consumable.RestoreEnergy > 0f )
		{
			float maxEnergy = StatSheet.MaxEnergy?.Value ?? 100f;
			StatSheet.CurrentEnergy = System.MathF.Min( maxEnergy, StatSheet.CurrentEnergy + consumable.RestoreEnergy );
			Log.Info( $"[Flask] Restored {consumable.RestoreEnergy} Mana -> {StatSheet.CurrentEnergy}/{maxEnergy}" );
		}

		if ( consumable.BuffEffect != null && Buffs != null )
		{
			Buffs.ApplyBuff( consumable.BuffEffect, StatSheet );
		}
	}

	public bool UsePotion( Presentation.InventoryItem item )
	{
		if ( item?.Definition?.Consumable == null || StatSheet == null ) return false;

		var consumable = item.Definition.Consumable;

		if ( consumable.BuffEffect != null && Buffs != null )
		{
			Buffs.ApplyBuff( consumable.BuffEffect, StatSheet );
			Log.Info( $"[Potion] Consumed '{item.Name}' — applied buff '{consumable.BuffEffect.DisplayName}'" );
		}

		if ( consumable.RestoreHealth > 0f )
		{
			float maxHp = StatSheet.MaxHealth?.Value ?? 100f;
			StatSheet.CurrentHealth = System.MathF.Min( maxHp, StatSheet.CurrentHealth + consumable.RestoreHealth );
		}

		if ( consumable.RestoreEnergy > 0f )
		{
			float maxEnergy = StatSheet.MaxEnergy?.Value ?? 100f;
			StatSheet.CurrentEnergy = System.MathF.Min( maxEnergy, StatSheet.CurrentEnergy + consumable.RestoreEnergy );
		}

		return true;
	}
	

	private void UpdatePlayerMovementStats()
	{
		if ( _playerController == null )
		{
			_playerController = Components.GetInAncestorsOrSelf<PlayerController>() ?? Components.Get<PlayerController>();
		}
		if ( _playerController != null && StatSheet != null )
		{
			float baseSpeed = StatSheet.MoveSpeed.Value + 100f;
			_playerController.WalkSpeed = baseSpeed;
			_playerController.RunSpeed = baseSpeed * 2;
			_playerController.JumpSpeed = StatSheet.JumpPower.Value * 3;
			if ( Slide == null || !Slide.IsSliding )
			{
				_playerController.DuckedSpeed = baseSpeed * 0.5f;
			}
		}
	}
	private void HandleCombatInput()
	{
		if ( Combat == null )
			return;
		if ( Input.Keyboard.Pressed( "attack1" ) || Input.Keyboard.Pressed( "mouse1" ) )
		{
			// Use equipped weapon attack; fall back to unarmed punch
			var attack = Equipment?.GetWeaponAttackDef() ?? AttackData.Punch;
			TryPerformAttack( attack );
			BodyRenderer.Set( "holdtype", 5 );
			BodyRenderer.Set( "b_attack", true );
		}
		if ( Input.Keyboard.Pressed( "F" ) )
		{
			TryPerformAttack( AttackData.Kick );
		}
		if ( Input.Keyboard.Pressed( "R" ) )
		{
			TryPerformAttack( AttackData.Shoot );
		}
	}
	private void TryPerformAttack( AttackDef attack )
	{
		var facing = Scene.Camera?.WorldRotation ?? GameObject.WorldRotation;
		facing = Rotation.From( facing.Pitch(), facing.Yaw(), 0f );
		var request = new AttackRequest
		{
			Attacker = GameObject,
			Attack = attack,
			SourceItem = null,
			Origin = GameObject.WorldPosition,
			Facing = facing,
			AimDirection = facing.Forward,
			TargetPoint = null,
			Charge01 = 0f,
			AlternateUse = false,
			TriggerType = AttackTriggerType.PlayerInput
		};
		DebugAttackAnimation( attack );
		Combat.TryStartAttack( request );
		

	}
	private void DebugAttackAnimation( AttackDef attack )
	{
		if ( BodyRenderer == null )
		{
			Log.Warning( "Actor has no Renderer assigned." );
			return;
		}
		if ( attack == null )
		{
			Log.Warning( "Tried to play animation for null attack." );
			return;
		}
		if ( attack.AnimationName == null )
		{
			Log.Warning( $"Attack {attack.Id} has no AttackAnimation assigned." );
			return;
		}
		
		
	}
	private void DrawDebugStats()
	{
		Gizmo.Draw.ScreenText(
			$"Health: {StatSheet.CurrentHealth:F1}/{StatSheet.MaxHealth.Value:F1}",
			new Vector2( 10, 10 )
		);
		Gizmo.Draw.ScreenText(
			$"Energy: {StatSheet.CurrentEnergy:F1}/{StatSheet.MaxEnergy.Value:F1}",
			new Vector2( 10, 30 )
		);
		Gizmo.Draw.ScreenText(
			$"Stamina: {StatSheet.CurrentStamina:F1}/{StatSheet.MaxStamina.Value:F1}",
			new Vector2( 10, 50 )
		);
		Gizmo.Draw.ScreenText(
			$"Stagger: {StatSheet.CurrentStagger:F1}/{StatSheet.MaxStagger.Value:F1}",
			new Vector2( 10, 70 )
		);
		Gizmo.Draw.ScreenText(
			$"StaminaRegen: {StatSheet.StaminaRegen.Value:F2}/s (Swft: {StatSheet.Swiftness.Value:F1})",
			new Vector2( 10, 90 )
		);
	}
}
	
	
	
