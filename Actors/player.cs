using Sandbox.Code.Data;
using Sandbox.Code.Systems;
using Sandbox.MovieMaker;
namespace Sandbox.Code.Actors;
public sealed class Player : Actor
{
	[Property] public MoviePlayer MoviePlayer { get; set; }
	private CombatComponent _combat;
	protected override void OnStart()
	{
		base.OnStart();
		_combat = GameObject.Components.Get<CombatComponent>();
		if (MoviePlayer == null )
			MoviePlayer = GameObject.AddComponent<MoviePlayer>();
	}
	protected override void OnUpdate()
	{
		base.OnUpdate();
		HandleCombatInput();
		DrawDebugStats();
	}
	private void HandleCombatInput()
	{
		if ( _combat == null )
			return;
		if ( Input.Keyboard.Pressed( "attack1" ) || Input.Keyboard.Pressed( "mouse1" ) )
		{
			TryPerformAttack( AttackData.Punch );
		}
		if ( Input.Keyboard.Pressed( "F" ) )
		{
			TryPerformAttack( AttackData.Kick );
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
		PlayAttackAnimation( attack );
		_combat.TryStartAttack( request );
	}
	private void PlayAttackAnimation( AttackDef attack )
	{
		if ( MoviePlayer == null )
		{
			Log.Warning( "Player has no MoviePlayer assigned." );
			return;
		}
		if ( attack == null )
		{
			Log.Warning( "Tried to play animation for null attack." );
			return;
		}
		if ( attack.AttackAnimation == null )
		{
			Log.Warning( $"Attack {attack.Id} has no AttackAnimation assigned." );
			return;
		}
		
		MoviePlayer.Play(attack.AttackAnimation);
	}
	private void DrawDebugStats()
	{
		Gizmo.Draw.ScreenText(
			$"Health: {Runtime.Health}",
			new Vector2( 10, 10 )
		);
		Gizmo.Draw.ScreenText(
			$"Energy: {Runtime.Energy}",
			new Vector2( 10, 30 )
		);
		Gizmo.Draw.ScreenText(
			$"Stamina: {Runtime.Stamina}",
			new Vector2( 10, 50 )
		);
		Gizmo.Draw.ScreenText(
			$"Stagger: {Runtime.Stagger}",
			new Vector2( 10, 70 )
		);
	}
}
	
	
	

