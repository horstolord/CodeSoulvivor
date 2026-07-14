using Sandbox.Code.Actors;

namespace Sandbox.Code.Systems;


public class gizmoscomp : Component

{ 
	public  Enemy Local { get; private set; }

	public StatSheet StatSheet { get; private set; }
	

	protected override void OnStart()
	{
		base.OnStart();
		Local = Components.Get<Enemy>(  );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		StatSheet = Local.StatSheet;
		DrawDebugStats();
	}

	public void DrawDebugStats()
	{
			Gizmo.Draw.ScreenText(
				$"Health: {StatSheet.CurrentHealth:F1}/{StatSheet.MaxHealth.Value:F1}",
				new Vector2( 100, 10 )
			);
			Gizmo.Draw.ScreenText(
				$"Energy: {StatSheet.CurrentEnergy:F1}/{StatSheet.MaxEnergy.Value:F1}",
				new Vector2( 1000, 30 )
			);
			Gizmo.Draw.ScreenText(
				$"Stamina: {StatSheet.CurrentStamina:F1}/{StatSheet.MaxStamina.Value:F1}",
				new Vector2( 0, 50 )
			);
			Gizmo.Draw.ScreenText(
				$"Stagger: {StatSheet.CurrentStagger:F1}/{StatSheet.MaxStagger.Value:F1}",
				new Vector2( 100, 70 )
			);
			Gizmo.Draw.ScreenText(
				$"StaminaRegen: {StatSheet.StaminaRegen.Value:F2}/s (Agility: {StatSheet.Agility.Value:F1})",
				new Vector2( 100, 90 )
			);
	}
}

