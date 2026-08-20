using System;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Code.Actors;
using Sandbox.Code.Data;

namespace Sandbox.Code.Systems;

public sealed class SlideControl : Component
{
	[Property] public float SlideDuration { get; set; } = 0.45f;
	[Property] public float SpeedBoost { get; set; } = 350f;
	[Property] public float MinSpeedThreshold { get; set; } = 180f;
	[Property] public float Cooldown { get; set; } = 0.6f;
	[Property] public float StaminaCost { get; set; } = 0f;
	[Property] public float PreventGroundingDuration { get; set; } = 0f;

	[Property] public bool IsSliding { get; private set; } = false;

	public TimeSince TimeSinceSlide { get; private set; } = 999f;
	public TimeUntil SlideEndTime { get; private set; }

	private PlayerController _playerController;
	private StatSheet _statSheet;
	private BuffComponent _buffs;
	private SkinnedModelRenderer _bodyRenderer;
	private CitizenAnimationHelper _animHelper;

	protected override void OnStart()
	{
		base.OnStart();
		CacheComponents();
	}

	private void CacheComponents()
	{
		_playerController ??= Components.GetInAncestorsOrSelf<PlayerController>() ?? Components.Get<PlayerController>();
		_statSheet ??= Components.GetInAncestorsOrSelf<StatSheet>() ?? Components.Get<StatSheet>();
		_buffs ??= Components.GetInAncestorsOrSelf<BuffComponent>() ?? Components.Get<BuffComponent>();
		_animHelper ??= Components.GetInChildren<CitizenAnimationHelper>() ?? Components.Get<CitizenAnimationHelper>();
		_bodyRenderer ??= Components.GetInChildren<SkinnedModelRenderer>() ?? Components.Get<SkinnedModelRenderer>();
	}

	protected override void OnUpdate()
	{
		if ( _playerController == null )
		{
			CacheComponents();
			if ( _playerController == null ) return;
		}

		if ( IsSliding )
		{
			// Maintain boosted duck speed during slide
			if ( _statSheet != null )
			{
				_playerController.DuckedSpeed = _statSheet.MoveSpeed.Value + SpeedBoost;
			}

			if ( SlideEndTime )
			{
				EndSlide();
			}
			return;
		}

		if ( CanSlide() && (Input.Pressed( "duck" ) || Input.Pressed( "crouch" )) )
		{
			StartSlide();
		}
	}

	public bool CanSlide()
	{
		if ( IsSliding ) return false;
		if ( TimeSinceSlide < Cooldown ) return false;
		if ( _playerController == null ) return false;
		if ( !_playerController.IsOnGround ) return false;

		var horizontalSpeed = _playerController.Velocity.WithZ( 0 ).Length;
		if ( horizontalSpeed < MinSpeedThreshold ) return false;

		if ( StaminaCost > 0f && _statSheet != null && _statSheet.CurrentStamina < StaminaCost )
			return false;

		return true;
	}

	public void StartSlide()
	{
		IsSliding = true;
		TimeSinceSlide = 0f;
		SlideEndTime = SlideDuration;

		// Deduct optional stamina cost
		if ( StaminaCost > 0f && _statSheet != null )
		{
			_statSheet.CurrentStamina = MathF.Max( 0f, _statSheet.CurrentStamina - StaminaCost );
		}

		// Apply flat MoveSpeed buff via BuffComponent
		if ( _buffs != null && _statSheet != null )
		{
			var slideBuff = new BuffDef( "slide_speed", "Slide", SlideDuration )
			{
				Modifiers = new List<BuffModifier>
				{
					new BuffModifier( "MoveSpeed", SpeedBoost, ModifierType.Flat )
				}
			};
			_buffs.ApplyBuff( slideBuff, _statSheet );
		}

		// Update PlayerController ducked speed to match slide momentum
		if ( _statSheet != null )
		{
			_playerController.DuckedSpeed = _statSheet.MoveSpeed.Value + SpeedBoost;
		}
		_playerController.IsDucking = true;

		// Optional prevent grounding (e.g. for slopes or frictionless sliding)
		if ( PreventGroundingDuration > 0f )
		{
			_playerController.PreventGrounding( PreventGroundingDuration );
		}

		// Animate slide
		if ( _animHelper.IsValid() )
		{
			_animHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.Slide;
			_animHelper.DuckLevel = 1f;
		}
		else if ( _bodyRenderer.IsValid() )
		{
			_bodyRenderer.Set( "special_movement_states", (int)CitizenAnimationHelper.SpecialMoveStyle.Slide );
			_bodyRenderer.Set( "duck", 1f );
		}

		Log.Info( $"[SlideControl] Slide initiated! Duration: {SlideDuration}s, SpeedBoost: +{SpeedBoost}" );
	}

	public void EndSlide()
	{
		IsSliding = false;

		// Restore normal ducked speed
		if ( _statSheet != null && _playerController != null )
		{
			_playerController.DuckedSpeed = (_statSheet.MoveSpeed.Value + 100f) * 0.5f;
		}

		// Reset animation state
		if ( _animHelper.IsValid() )
		{
			_animHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.None;
		}
		else if ( _bodyRenderer.IsValid() )
		{
			_bodyRenderer.Set( "special_movement_states", 0 );
		}

		Log.Info( "[SlideControl] Slide ended." );
	}
}
