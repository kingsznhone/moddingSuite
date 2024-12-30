using moddingSuite.Model.Ndfbin;

namespace BGManager;

public class PawnModule
{
	public int Health;

	public int MaxHealth;

	public int ActionPoints;

	public int MaxActionPoints;

	public uint id;

	public string view;

	public PawnModule(NdfObject pm)
	{
		Health = NDFWrappers.ndfGetIntByKey("InitialHealthPoints", pm);
		MaxHealth = NDFWrappers.ndfGetIntByKey("MaxHealthPoints", pm);
		ActionPoints = NDFWrappers.ndfGetIntByKey("InitialActionPoints", pm);
		MaxActionPoints = NDFWrappers.ndfGetIntByKey("MaxActionPoints", pm);
		id = pm.Id;
		view = $"{Health,2}-{MaxHealth,2} {ActionPoints,2}-{MaxActionPoints}";
	}
}
