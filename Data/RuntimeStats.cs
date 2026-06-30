namespace Sandbox.Code.Data;
public class RuntimeStats
{
    public float Health;
    public float Stamina;
    public float Energy;
	public float Stagger;
    public void FillFromDerived( DerivedStats ds )
    {
        Health  = ds.MaxHealth;
        Stamina = ds.MaxStamina;
        Energy  = ds.MaxEnergy;
        Stagger = ds.MaxStagger;
    }

    
}
