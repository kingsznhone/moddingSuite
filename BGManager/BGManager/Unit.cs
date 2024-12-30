using System;
using System.Collections.Generic;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types.AllTypes;

namespace BGManager;

public class Unit
{
	public enum TransportRole
	{
		NotTransportable,
		Transport,
		Vehicle,
		Barque,
		FootCrew
	}

	public enum UnitSubType
	{
		FOB,
		CV,
		Foot_CV,
		Helo_CV,
		Supply_truck,
		Supply_helo,
		Infantry,
		MANPAD,
		ATGM,
		Flamethrowers,
		ASF,
		Napalm_bomber,
		Cluster_bomber,
		Iron_bomber,
		AGM_plane,
		Ground_attack,
		SEAD,
		Tank,
		Recon,
		Foot_Recon,
		Helo_Recon,
		Gunship,
		Transport_Helo,
		Naval_CV,
		Escort_Ship,
		Supply_Ship,
		Naval_ASF,
		AShM,
		AShM_truck,
		Rad_AA,
		IR_SAM,
		AA,
		SPAAG,
		Mortar,
		Artillery,
		MLRS,
		Vechicle,
		Transport_Truck,
		Transport_IFV,
		Tank_Destroyer,
		Napalm_Vehicle,
		IFV,
		APC,
		Error
	}

	public enum MovingType
	{
		Foot = 1,
		Wheeled = 2,
		Off_Road_Wheels = 3,
		Caterpillar = 5,
		Flying = 6,
		Wheels_Swim = 7,
		Caterpillar_Swim = 8,
		Naval = 9
	}

	public enum UnitType
	{
		Logistics = 3,
		Infantry = 6,
		Air = 7,
		Vechicle = 8,
		Tank = 9,
		Recon = 10,
		Helo = 11,
		Navy = 12,
		Support = 13,
		Error = 0
	}

	private string _name = "";

	private TransportRole _trnasportRole = TransportRole.NotTransportable;

	private NdfObject _unitRef = null;

	private NdfCollection _modules = null;

	protected NdfObject UnitRef
	{
		get
		{
			return _unitRef;
		}
		set
		{
			_unitRef = value;
			ShortDatabaseName = NDFWrappers.ndfGetStringByKey("_ShortDatabaseName", _unitRef);
			DebugName = NDFWrappers.ndfGetStringByKey("ClassNameForDebug", _unitRef);
			AliasName = NDFWrappers.ndfGetStringByKey("AliasName", _unitRef);
			Country = NDFWrappers.ndfGetStringByKey("MotherCountry", _unitRef);
			Name = NDFWrappers.ndfGetHashStringByKey("NameInMenuToken", _unitRef, DB.UnitHash);
			int _factory = NDFWrappers.ndfGetIntByKey("Factory", _unitRef);
			UnitAllegiance = ((NDFWrappers.ndfGetIntByKey("Nationalite", _unitRef) == 1) ? Allegiance.PACT : Allegiance.NATO);
			IsPrototype = NDFWrappers.ndfGetBoolIsTrueByKey("IsPrototype", _unitRef);
			MoveType = (MovingType)NDFWrappers.ndfGetIntByKey("UnitMovingType", _unitRef);
			ECM = (uint)((double)NDFWrappers.ndfGetFloatByKey("HitRollECMModifier", _unitRef) * -100.0);
			UType = (UnitType)_factory;
			List<int> priceList = NDFWrappers.ndfGetIntListByKey("ProductionPrice", _unitRef);
			NdfValueWrapper v = NDFWrappers.ndfGetValueByChain(_unitRef, new List<string> { "Modules", "Transporter", "Default", "Categories", "0" });
			TransporterType = ((v == null) ? "" : NDFWrappers.ndfGetString(v));
			if (priceList != null && priceList.Count > 0)
			{
				Price = (uint)priceList[0];
			}
			NdfCollection c = NDFWrappers.ndfGetCollectionByKey("UnitTypeTokens", _unitRef.PropertyValues);
			if (c != null)
			{
				DeckTypes = NDFWrappers.ndfGetHashStringList(c, DB.InterfaceHash);
				if (DeckTypes != null && DeckTypes.Count > 0)
				{
					for (int i = 0; i < DeckTypes.Count; i++)
					{
						string dt = DeckTypes[i];
						Gauges.removedPrefixWord("#", " ", ref dt);
						DeckTypes[i] = dt;
					}
				}
			}
			Year = NDFWrappers.ndfGetUIntByKey("ProductionYear", _unitRef);
			_modules = NDFWrappers.ndfGetCollectionByKey("Modules", _unitRef.PropertyValues);
			if (NDFWrappers.ndfCheckChain(_unitRef, new List<string>(new string[7] { "Modules", "Damage", "Default", "CommonDamageDescriptor", "BlindageProperties", "ArmorDescriptorFront", "BaseBlindage" })))
			{
				v = NDFWrappers.ndfGetValueByChain(_unitRef, new List<string>(new string[4] { "Modules", "Damage", "Default", "MaxDamages" }));
				uint md = (uint)NDFWrappers.ndfGetNumber(v);
				v = NDFWrappers.ndfGetValueByChain(_unitRef, new List<string>(new string[7] { "Modules", "Damage", "Default", "CommonDamageDescriptor", "BlindageProperties", "ArmorDescriptorFront", "BaseBlindage" }));
				int fa = NDFWrappers.ndfGetInt(v);
				v = NDFWrappers.ndfGetValueByChain(_unitRef, new List<string>(new string[7] { "Modules", "Damage", "Default", "CommonDamageDescriptor", "BlindageProperties", "ArmorDescriptorSides", "BaseBlindage" }));
				int sa = NDFWrappers.ndfGetInt(v);
				v = NDFWrappers.ndfGetValueByChain(_unitRef, new List<string>(new string[7] { "Modules", "Damage", "Default", "CommonDamageDescriptor", "BlindageProperties", "ArmorDescriptorRear", "BaseBlindage" }));
				int ra = NDFWrappers.ndfGetInt(v);
				v = NDFWrappers.ndfGetValueByChain(_unitRef, new List<string>(new string[7] { "Modules", "Damage", "Default", "CommonDamageDescriptor", "BlindageProperties", "ArmorDescriptorTop", "BaseBlindage" }));
				int ta = NDFWrappers.ndfGetInt(v);
				uint armorModifier = ((fa < 4 || sa < 4 || ra < 4 || ta < 4) ? 3u : 4u);
				FrontArmor = (uint)(fa - armorModifier);
				SideArmor = (uint)(sa - armorModifier);
				RearArmor = (uint)(ra - armorModifier);
				TopArmor = (uint)(ta - armorModifier);
			}
			Turrets = new List<Turret>();
			if (_modules == null)
			{
				return;
			}
			NdfObjectReference damage = (NdfObjectReference)NDFWrappers.ndfGetValueFromMap("Damage", _modules);
			if (damage != null)
			{
				NdfObjectReference damageRef = (NdfObjectReference)NDFWrappers.ndfGetValueByKey("Default", damage.Instance.PropertyValues);
				if (damageRef != null)
				{
					Damage = (uint)NDFWrappers.ndfGetFloatByKey("MaxDamages", damageRef.Instance);
					NdfObjectReference damageDescr = (NdfObjectReference)NDFWrappers.ndfGetValueByKey("CommonDamageDescriptor", damageRef.Instance.PropertyValues);
					if (damageDescr == null)
					{
					}
				}
			}
			NdfObjectReference weapon = (NdfObjectReference)NDFWrappers.ndfGetValueFromMap("WeaponManager", _modules);
			if (weapon == null)
			{
				return;
			}
			NdfObjectReference wmod = (NdfObjectReference)NDFWrappers.ndfGetValueByKey("Default", weapon.Instance.PropertyValues);
			if (wmod == null)
			{
				return;
			}
			NdfCollection turretList = (NdfCollection)NDFWrappers.ndfGetValueByKey("TurretDescriptorList", wmod.Instance.PropertyValues);
			if (turretList == null)
			{
				return;
			}
			foreach (CollectionItemValueHolder turret in turretList)
			{
				NdfObjectReference tr = (NdfObjectReference)turret.Value;
				if (tr != null)
				{
					Turrets.Add(new Turret(DB, tr));
				}
			}
		}
	}

	public DataBase DB { get; protected set; }

	public uint Id => UnitRef.Id;

	public string Name
	{
		get
		{
			return _name;
		}
		protected set
		{
			string s = ((value != "") ? value : ((AliasName != "") ? AliasName : ((ShortDatabaseName != "") ? ShortDatabaseName : DebugName)));
			Gauges.removePrefix("Descriptor_", ref s);
			Gauges.removePrefix("Building_", ref s);
			Gauges.removePrefix("Unit_", ref s);
			_name = s;
		}
	}

	public uint Price { get; protected set; }

	public UnitType UType { get; protected set; }

	public UnitSubType USubType { get; protected set; }

	public Allegiance UnitAllegiance { get; protected set; }

	public string Country { get; protected set; }

	public uint Year { get; protected set; }

	public TransportRole Role
	{
		get
		{
			return _trnasportRole;
		}
		protected set
		{
			if (value == TransportRole.Transport)
			{
				switch (USubType)
				{
				case UnitSubType.IFV:
					USubType = UnitSubType.Transport_IFV;
					break;
				case UnitSubType.Gunship:
					USubType = UnitSubType.Transport_Helo;
					break;
				case UnitSubType.Vechicle:
					if (Turrets.Count == 0)
					{
						USubType = UnitSubType.Transport_Truck;
					}
					break;
				}
			}
			DB.RegisterUnitRole(this, value);
			_trnasportRole = value;
		}
	}

	public string BattleRole => BattleRoleString(USubType);

	public string Description => $"{Id}:{Name},{Price} pts,Coutry:{Country},Role:{BattleRole},Type:{UType},Algnc:{UnitAllegiance},Moving type:{MoveType}";

	public string MovementString => (MoveType == MovingType.Flying) ? $"speed: {MaxSpeed} km/h{Environment.NewLine}" : ((MoveType == MovingType.Foot) ? $"speed: {Speed} km/h{Environment.NewLine}" : ((MoveType == MovingType.Naval) ? $"speed: {Math.Round((double)Speed / 18.52)} knots{Environment.NewLine}" : $"speed: {Speed} km/h road speed: {RoadSpeed} km/h{Environment.NewLine}"));

	public string ArmorString => (MoveType == MovingType.Foot) ? "" : $"Armor front {FrontArmor} side {SideArmor} rear {RearArmor} top {TopArmor}{Environment.NewLine}";

	public string RoleString
	{
		get
		{
			if (Role == TransportRole.NotTransportable || Role == TransportRole.Vehicle)
			{
				return BattleRoleString(USubType) + Environment.NewLine;
			}
			return $"{BattleRoleString(USubType)}, {Role} {TransporterType}{Environment.NewLine}";
		}
	}

	public float SpeedBonusOnRoad { get; protected set; }

	public uint MaxSpeed { get; protected set; }

	public uint RoadSpeed { get; protected set; }

	public uint Speed { get; protected set; }

	public uint Damage { get; protected set; }

	public string DebugName { get; protected set; }

	public string AliasName { get; set; }

	public string ShortDatabaseName { get; protected set; }

	public string TransporterType { get; protected set; }

	public List<string> DeckTypes { get; protected set; }

	public string DeckTypesString
	{
		get
		{
			if (DeckTypes != null && DeckTypes.Count > 0)
			{
				return string.Join(",", DeckTypes);
			}
			return "no deck types found";
		}
	}

	public List<Turret> Turrets { get; protected set; }

	public MovingType MoveType { get; protected set; }

	public bool IsPrototype { get; protected set; }

	public uint FrontArmor { get; protected set; }

	public uint SideArmor { get; protected set; }

	public uint RearArmor { get; protected set; }

	public uint TopArmor { get; protected set; }

	public uint ECM { get; protected set; }

	public static string BattleRoleString(UnitSubType role)
	{
		return role.ToString().Replace("_", " ");
	}

	public static string FormatMovingType(MovingType mt)
	{
		return mt switch
		{
			MovingType.Caterpillar_Swim => "Caterpillar, amphibious", 
			MovingType.Off_Road_Wheels => "Wheeled, off-road", 
			MovingType.Wheels_Swim => "Wheeled, amphibious", 
			_ => mt.ToString(), 
		};
	}

	private void DetermineUnitRole()
	{
		Role = TransportRole.NotTransportable;
		USubType = UnitSubType.Error;
		switch (UType)
		{
		case UnitType.Logistics:
			if (Name.Contains("FOB"))
			{
				USubType = UnitSubType.FOB;
			}
			else if (Name.Contains("#command"))
			{
				if (MoveType == MovingType.Foot)
				{
					USubType = UnitSubType.Foot_CV;
				}
				else if (MoveType == MovingType.Flying)
				{
					USubType = UnitSubType.Helo_CV;
				}
				else
				{
					USubType = UnitSubType.CV;
				}
			}
			else if (MoveType == MovingType.Flying)
			{
				USubType = UnitSubType.Supply_helo;
			}
			else
			{
				USubType = UnitSubType.Supply_truck;
			}
			break;
		case UnitType.Infantry:
			Role = TransportRole.FootCrew;
			USubType = UnitSubType.Infantry;
			if (TurretDescriptionContains("SAM") && Damage < 10)
			{
				USubType = UnitSubType.MANPAD;
			}
			else if (TurretDescriptionContains("ATGM") && Damage < 10)
			{
				USubType = UnitSubType.ATGM;
			}
			else if (TurretDescriptionContains("Napalm") && Damage < 10)
			{
				USubType = UnitSubType.Flamethrowers;
			}
			break;
		case UnitType.Recon:
			Role = TransportRole.Vehicle;
			switch (MoveType)
			{
			case MovingType.Foot:
				USubType = UnitSubType.Foot_Recon;
				Role = TransportRole.FootCrew;
				break;
			case MovingType.Flying:
				USubType = UnitSubType.Helo_Recon;
				break;
			default:
				USubType = UnitSubType.Recon;
				break;
			}
			break;
		case UnitType.Support:
			Role = TransportRole.Vehicle;
			USubType = UnitSubType.Artillery;
			foreach (Turret trt in Turrets)
			{
				if (trt.Description.Contains("Mortar"))
				{
					USubType = UnitSubType.Mortar;
					break;
				}
				if (trt.Description.Contains("Autocannon"))
				{
					USubType = UnitSubType.SPAAG;
					break;
				}
				if (trt.Description.Contains("Radar"))
				{
					USubType = UnitSubType.Rad_AA;
					break;
				}
				if (trt.Description.Contains("MLRS"))
				{
					USubType = UnitSubType.MLRS;
					break;
				}
				if (trt.Description.Contains("Infrared"))
				{
					USubType = UnitSubType.IR_SAM;
					break;
				}
				if (trt.Description.Contains("SAM"))
				{
					USubType = UnitSubType.AA;
					break;
				}
			}
			break;
		case UnitType.Tank:
			Role = TransportRole.Vehicle;
			USubType = UnitSubType.Tank;
			break;
		case UnitType.Vechicle:
			if (TurretDescriptionContains("Napalm"))
			{
				USubType = UnitSubType.Napalm_Vehicle;
			}
			else if (TurretDescriptionContains("Autocannon"))
			{
				USubType = UnitSubType.IFV;
			}
			else if (TurretDescriptionContains("ATGM"))
			{
				USubType = UnitSubType.Tank_Destroyer;
			}
			else
			{
				USubType = UnitSubType.Vechicle;
			}
			break;
		case UnitType.Air:
			USubType = UnitSubType.Ground_attack;
			if (TurretDescriptionContains("Antiradar"))
			{
				USubType = UnitSubType.SEAD;
			}
			else if (TurretDescriptionContains("Napalm"))
			{
				USubType = UnitSubType.Napalm_bomber;
			}
			else if (TurretDescriptionContains("Cluster"))
			{
				USubType = UnitSubType.Cluster_bomber;
			}
			else if (TurretDescriptionContains("Bomb"))
			{
				USubType = UnitSubType.Iron_bomber;
			}
			else if (TurretDescriptionContains("AGM") || TurretDescriptionContains("ATGM"))
			{
				USubType = UnitSubType.AGM_plane;
			}
			else if (TurretDescriptionContains("AAM"))
			{
				USubType = UnitSubType.ASF;
			}
			break;
		case UnitType.Navy:
			if (Name.Contains("#command"))
			{
				USubType = UnitSubType.Naval_CV;
			}
			else if (MoveType == MovingType.Naval)
			{
				if (Turrets.Count > 0)
				{
					USubType = UnitSubType.Escort_Ship;
				}
				else
				{
					USubType = UnitSubType.Supply_Ship;
				}
			}
			else if (MoveType == MovingType.Flying)
			{
				USubType = UnitSubType.Naval_ASF;
				foreach (Turret trt in Turrets)
				{
					if (trt.Description.Contains("SSM"))
					{
						USubType = UnitSubType.AShM;
						break;
					}
				}
			}
			else
			{
				USubType = UnitSubType.AShM_truck;
			}
			break;
		case UnitType.Helo:
			USubType = UnitSubType.Gunship;
			break;
		}
		if (TransporterType != "")
		{
			if (TransporterType == "infanterie")
			{
				Role = TransportRole.Transport;
			}
			else
			{
				Role = TransportRole.Barque;
			}
		}
	}

	public Unit(DataBase db, NdfObject unitRef)
	{
		DB = db;
		UnitRef = unitRef;
		DetermineUnitRole();
		Speed = (uint)(NDFWrappers.ndfGetFloatByKey("VitesseCombat", _unitRef) / 52f);
		if (NDFWrappers.ndfCheckChain(_unitRef, new List<string>(new string[4] { "Modules", "MouvementHandler", "Default", "Maxspeed" })))
		{
			NdfValueWrapper v = NDFWrappers.ndfGetValueByChain(_unitRef, new List<string>(new string[4] { "Modules", "MouvementHandler", "Default", "Maxspeed" }));
			MaxSpeed = (uint)(NDFWrappers.ndfGetFloat(v) / 52f);
		}
		if (NDFWrappers.ndfCheckChain(_unitRef, new List<string>(new string[4] { "Modules", "MouvementHandler", "Default", "SpeedBonusOnRoad" })))
		{
			NdfValueWrapper v = NDFWrappers.ndfGetValueByChain(_unitRef, new List<string>(new string[4] { "Modules", "MouvementHandler", "Default", "SpeedBonusOnRoad" }));
			SpeedBonusOnRoad = NDFWrappers.ndfGetFloat(v);
			if (SpeedBonusOnRoad > 0f)
			{
				RoadSpeed = (uint)Math.Round(((double)SpeedBonusOnRoad + 1.0) * (double)Speed);
			}
		}
		if (MoveType == MovingType.Foot)
		{
			Speed = (RoadSpeed = (MaxSpeed = (uint)(NDFWrappers.ndfGetFloatByKey("VitesseCombat", _unitRef) / 78f)));
		}
	}

	internal bool TurretDescriptionContains(string keyword)
	{
		foreach (Turret trt in Turrets)
		{
			if (trt.Description.Contains(keyword))
			{
				return true;
			}
		}
		return false;
	}
}
