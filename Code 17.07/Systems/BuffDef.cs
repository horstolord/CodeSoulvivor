namespace Sandbox.Code.Systems;

public class BuffDef
{
	public string Id { get; set; }
	public string DisplayName { get; set; }
	public float Duration { get; set; } // In seconds
	public List<BuffModifier> Modifiers { get; set; } = new();

	public BuffDef( string id, string displayName, float duration )
	{
		Id = id;
		DisplayName = displayName;
		Duration = duration;
	}
}
