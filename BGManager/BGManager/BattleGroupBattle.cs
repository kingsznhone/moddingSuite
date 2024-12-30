using moddingSuite.Model.Ndfbin;

namespace BGManager;

public class BattleGroupBattle
{
	public uint id { get; set; }

	public int Moral { get; set; }

	public int MaxMoral { get; set; }

	public int Cohesion { get; set; }

	public int MaxCohesion { get; set; }

	public int FightingType { get; set; }

	public int MinPoints { get; set; }

	public bool EngageBattle { get; set; }

	public bool AvoidBattle { get; set; }

	public bool RetreatBattle { get; set; }

	public bool SuriveWithoutCV { get; set; }

	public bool CanConflict { get; set; }

	public bool TakeAlamo { get; set; }

	public string view { get; set; }

	public BattleGroupBattle(NdfObject bgb)
	{
		Moral = NDFWrappers.ndfGetIntByKey("MoralInitial", bgb);
		MaxMoral = NDFWrappers.ndfGetIntByKey("MoralMax", bgb);
		Cohesion = NDFWrappers.ndfGetIntByKey("BaseCohesion", bgb);
		MaxCohesion = NDFWrappers.ndfGetIntByKey("CohesionMax", bgb);
		FightingType = NDFWrappers.ndfGetIntByKey("FightingType", bgb);
		MinPoints = NDFWrappers.ndfGetIntByKey("MinimalUnitPointsForDestruction", bgb);
		EngageBattle = NDFWrappers.ndfGetBoolIsTrueByKey("EngageBattle", bgb);
		AvoidBattle = NDFWrappers.ndfGetBoolIsTrueByKey("AvoidBattleAvailable", bgb);
		RetreatBattle = NDFWrappers.ndfGetBoolIsTrueByKey("RetreatBattleAvailable", bgb);
		SuriveWithoutCV = NDFWrappers.ndfGetBoolIsTrueByKey("CanSurviveWithoutUC", bgb);
		CanConflict = NDFWrappers.ndfGetBoolIsTrueByKey("CanConflict", bgb);
		TakeAlamo = NDFWrappers.ndfGetBoolIsTrueByKey("TakeAlamo", bgb);
		id = bgb.Id;
		view = string.Format("{4,-5}:{0,2}-{1,2} {2,2}-{3}", Moral, MaxMoral, Cohesion, MaxCohesion, id);
	}

	public bool Compare(BattleGroupBattle bgb)
	{
		return Moral == bgb.Moral && Cohesion == bgb.Cohesion && MaxCohesion == bgb.MaxCohesion && FightingType == bgb.FightingType && MinPoints == bgb.MinPoints && EngageBattle == bgb.EngageBattle && AvoidBattle == bgb.AvoidBattle && RetreatBattle == bgb.RetreatBattle && SuriveWithoutCV == bgb.SuriveWithoutCV && CanConflict == bgb.CanConflict && TakeAlamo == bgb.TakeAlamo;
	}
}
