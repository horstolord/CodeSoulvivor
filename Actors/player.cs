using Sandbox.Code.Data;
using Sandbox.Code.Systems;
using Sandbox.MovieMaker;

namespace Sandbox.Code.Actors;

public sealed class Player : Actor
{
	[Property] public MovieResource MovieClip { get; set; }
	MoviePlayer moviePlayer; 
	protected override void OnUpdate()
	{
		base.OnUpdate();
		DrawStats();
	}

	

	private void DrawStats()
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


		{
			if ( Input.Keyboard.Pressed( "F" ) )
			{
				var combat = GameObject.Components.Get<CombatComponent>();
				if ( combat == null )
					return;
				var facing = Scene.Camera?.WorldRotation ?? GameObject.WorldRotation;
				facing = Rotation.From( facing.Pitch(), facing.Yaw(), 0f);
				var request = new AttackRequest
				{
					Attacker = GameObject,
					Attack = AttackData.Kick,
					SourceItem = null,
					Origin = GameObject.WorldPosition,
					Facing = facing,
					AimDirection = facing.Forward,
					TargetPoint = null,
					Charge01 = 0f,
					AlternateUse = false,
					TriggerType = AttackTriggerType.PlayerInput
				};
				combat.TryStartAttack( request );
				
			}
			
		}
		if ( Input.Keyboard.Pressed( "mouse1" ) )
		{
			var combat = GameObject.Components.Get<CombatComponent>();
			if ( combat == null )
				return;
			var facing = Scene.Camera?.WorldRotation ?? GameObject.WorldRotation;
			facing = Rotation.From( facing.Pitch(), facing.Yaw(), 0f);
			var request = new AttackRequest
			{
				Attacker = GameObject,
				Attack = AttackData.Punch,
				SourceItem = null,
				Origin = GameObject.WorldPosition,
				Facing = facing,
				AimDirection = facing.Forward,
				TargetPoint = null,
				Charge01 = 0f,
				AlternateUse = false,
				TriggerType = AttackTriggerType.PlayerInput
			};
			moviePlayer.Clip = AttackData.Punch.AttackAnimation
			combat.TryStartAttack( request );
		}
	}
}
