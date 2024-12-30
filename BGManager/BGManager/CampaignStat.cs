using System;
using System.Collections.Generic;

namespace BGManager;

public class CampaignStat
{
	public class ConflictSideData
	{
		private Dictionary<Unit.UnitType, Dictionary<Unit.UnitSubType, uint>> UnitNumbers = new Dictionary<Unit.UnitType, Dictionary<Unit.UnitSubType, uint>>();

		private Dictionary<string, uint> BGTypesNumbers = new Dictionary<string, uint>();

		public string country = "";

		public uint bgNumber = 0u;

		public uint bgStratPrice = 0u;

		public uint bgDecksPrice = 0u;

		public ConflictSide side = ConflictSide.Unknown;

		public string ShortDescription => $"{side.ToString()} : {bgNumber} battlegroups, {bgStratPrice} strategic points, {bgDecksPrice} total deck price";

		public string UnitsSummary
		{
			get
			{
				string description = "";
				foreach (KeyValuePair<Unit.UnitType, Dictionary<Unit.UnitSubType, uint>> role in UnitNumbers)
				{
					description += $"{role.Key}:";
					string prefix = "";
					foreach (KeyValuePair<Unit.UnitSubType, uint> roleNumber in role.Value)
					{
						description += $"{prefix} {roleNumber.Value} {Unit.BattleRoleString(roleNumber.Key)}";
						if (prefix.Length < 1)
						{
							prefix = ",";
						}
					}
					description += Environment.NewLine;
				}
				return description;
			}
		}

		public string BGTypesSummary
		{
			get
			{
				string description = "Battle Group types summary:";
				string prefix = "";
				foreach (KeyValuePair<string, uint> t in BGTypesNumbers)
				{
					description += $"{prefix} {t.Value} {t.Key}";
					if (prefix.Length < 1)
					{
						prefix = ",";
					}
				}
				return description;
			}
		}

		public ConflictSideData(ConflictSide side)
		{
			this.side = side;
		}

		public void Include(BG bg)
		{
			bgNumber++;
			bgStratPrice += (uint)bg.Price;
			bgDecksPrice += bg.DeckPrice;
			foreach (DeckUnit du in bg.LinkedDeck)
			{
				if (!UnitNumbers.ContainsKey(du.HostUnit.UType))
				{
					UnitNumbers[du.HostUnit.UType] = new Dictionary<Unit.UnitSubType, uint>();
				}
				if (!UnitNumbers[du.HostUnit.UType].ContainsKey(du.HostUnit.USubType))
				{
					UnitNumbers[du.HostUnit.UType][du.HostUnit.USubType] = 0u;
				}
				UnitNumbers[du.HostUnit.UType][du.HostUnit.USubType] += du.LPU.Count;
			}
			if (!BGTypesNumbers.ContainsKey(bg.MimeticType))
			{
				BGTypesNumbers[bg.MimeticType] = 0u;
			}
			BGTypesNumbers[bg.MimeticType]++;
		}
	}

	private DataBase.CampaignData CD;

	public ConflictSideData Computer = new ConflictSideData(ConflictSide.Computer);

	public ConflictSideData Player = new ConflictSideData(ConflictSide.Player);

	public Dictionary<string, ConflictSide> countryMap = new Dictionary<string, ConflictSide>();

	public List<BG> bgList = new List<BG>();

	public string Ratio
	{
		get
		{
			if (Player.bgDecksPrice != 0 && Computer.bgDecksPrice != 0)
			{
				if (Player.bgDecksPrice < Computer.bgDecksPrice)
				{
					return $"Sides force ratio - {(double)Computer.bgDecksPrice / (double)Player.bgDecksPrice,4:F}:1";
				}
				return $"Sides force ratio - 1:{(double)Player.bgDecksPrice / (double)Computer.bgDecksPrice,4:F}";
			}
			return "Sides force ratio - unknown";
		}
	}

	public CampaignStat(DataBase.CampaignData cd)
	{
		CD = cd;
		foreach (BG bg in CD.BGL)
		{
			if (bg.InitId != 0)
			{
				AddBG(bg);
			}
		}
		ComputeSides();
	}

	public void AddBG(BG bg)
	{
		bgList.Add(bg);
		if (bg.Side != ConflictSide.Unknown && !countryMap.ContainsKey(bg.Country))
		{
			countryMap[bg.Country] = bg.Side;
		}
	}

	public void ComputeSides()
	{
		Computer = new ConflictSideData(ConflictSide.Computer);
		Player = new ConflictSideData(ConflictSide.Player);
		foreach (BG bg in bgList)
		{
			if (countryMap.ContainsKey(bg.Country) && countryMap[bg.Country] == ConflictSide.Computer)
			{
				Computer.Include(bg);
			}
			else
			{
				Player.Include(bg);
			}
		}
	}
}
