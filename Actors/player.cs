using Sandbox.Citizen;
using Sandbox.Code.Data;
using Sandbox.Code.Systems;
namespace Sandbox.Code.Actors;
public sealed class Player : Actor
{
	
	public static Player Local { get; private set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	private Vector3 _knockbackVelocity;

	// Load the "player" / hero stat preset from MobRegistry
	protected override string GetMobPresetId() => "player";
	
	protected override void OnStart()
	{
		base.OnStart();
		MobRegistry.Initialize();
		Local = this;
		Combat = GameObject.Components.Get<CombatComponent>();
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
		if ( Combat.CanAttack )
		{
			HandleCombatInput();
		}
	}
	
	private void HandleCombatInput()
	{
		if ( Combat == null )
			return;
		if ( Input.Keyboard.Pressed( "attack1" ) || Input.Keyboard.Pressed( "mouse1" ) )
		{
			TryPerformAttack( AttackData.Punch );
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
	
	
	

