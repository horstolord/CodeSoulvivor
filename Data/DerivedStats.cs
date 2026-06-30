namespace Sandbox.Code.Data;
public class DerivedStats
{
	public float MaxHealth;
	public float MaxStamina;
	public float MaxEnergy;
	public float HealthRegen;
	public float StaminaRegen;
	public float EnergyRegen;
	public float MaxStagger;
	
	
	public void Recalculate( AttributeSet a )
	{
		MaxHealth    = a.Vitality * 10f;
		HealthRegen  = a.Vitality * 0.1f;
		MaxStamina   = a.Agility * 10f;
		StaminaRegen = a.Agility * 5f;
		MaxEnergy    = a.Wisdom * 10f;
		EnergyRegen  = a.Acuity * 0.2f;
		MaxStagger   = a.Might * 10f;
		
	}
}
