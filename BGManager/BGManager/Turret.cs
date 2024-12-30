using System;
using System.Collections.Generic;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types.AllTypes;

namespace BGManager;

public class Turret
{
	public class TurretMount
	{
		private NdfObjectReference MountedWeaponDescriptor { get; set; }

		public string Name { get; protected set; }

		public string TypeName { get; protected set; }

		public string TypeArme { get; protected set; }

		public string Caliber { get; protected set; }

		public float BurstTime { get; protected set; }

		public float SalvoTime { get; protected set; }

		public float NbTirParSalves { get; protected set; }

		public uint Salvo { get; protected set; }

		public uint Suppression { get; protected set; }

		public uint HitProbabilty { get; protected set; }

		public uint HitStabilization { get; protected set; }

		public uint RPM { get; protected set; }

		public Dictionary<string, UIntPair> Range { get; protected set; }

		public string Description
		{
			get
			{
				string descr = $"{Name} {TypeArme} ({Caliber}) {RPM} rpm{Environment.NewLine}Range ";
				foreach (KeyValuePair<string, UIntPair> range in Range)
				{
					descr = ((range.Value.B != 0) ? (descr + $"[{range.Key}: {range.Value.B}-{range.Value.A}m]") : (descr + $"[{range.Key}: {range.Value.A}m]"));
				}
				return descr + string.Format("{3}Suppression {0}, Accuracy {1}%, Stabilisation {2}%", Suppression, HitProbabilty, HitStabilization, Environment.NewLine);
			}
		}

		public TurretMount(DataBase db, NdfObjectReference mwd)
		{
			MountedWeaponDescriptor = mwd;
			NdfObjectReference aor = (NdfObjectReference)NDFWrappers.ndfGetValueByKey("Ammunition", mwd.Instance.PropertyValues);
			Name = NDFWrappers.ndfGetHashStringByKey("Name", aor.Instance, db.UnitHash);
			TypeName = NDFWrappers.ndfGetHashStringByKey("TypeName", aor.Instance, db.UnitHash);
			TypeArme = NDFWrappers.ndfGetHashStringByKey("TypeArme", aor.Instance, db.UnitHash);
			Caliber = NDFWrappers.ndfGetHashStringByKey("Caliber", aor.Instance, db.UnitHash);
			Range = new Dictionary<string, UIntPair>();
			BurstTime = NDFWrappers.ndfGetFloatByKey("TempsEntreDeuxTirs", aor.Instance);
			SalvoTime = NDFWrappers.ndfGetFloatByKey("TempsEntreDeuxSalves", aor.Instance);
			Salvo = NDFWrappers.ndfGetUIntByKey("AffichageMunitionParSalve", aor.Instance);
			NbTirParSalves = NDFWrappers.ndfGetIntByKey("NbTirParSalves", aor.Instance);
			RPM = (uint)((float)(60 * Salvo) / (BurstTime * (NbTirParSalves - 1f) + SalvoTime));
			Suppression = (uint)NDFWrappers.ndfGetFloatByKey("SuppressDamages", aor.Instance);
			UIntPair range = GetRange("PorteeMaximale", "PorteeMinimale", aor.Instance);
			NdfValueWrapper hp = NDFWrappers.ndfGetValueByChain(aor.Instance, new List<string> { "HitRollRule", "HitProbability" });
			HitProbabilty = (uint)Math.Round(NDFWrappers.ndfGetFloat(hp) * 100f);
			hp = NDFWrappers.ndfGetValueByChain(aor.Instance, new List<string> { "HitRollRule", "HitProbabilityWhileMoving" });
			HitStabilization = (uint)Math.Round(NDFWrappers.ndfGetFloat(hp) * 100f);
			if (range.A != 0 || range.B != 0)
			{
				Range.Add("Ground", range);
			}
			range = GetRange("PorteeMaximaleBateaux", "PorteeMinimaleBateaux", aor.Instance);
			if (range.A != 0 || range.B != 0)
			{
				Range.Add("Sea", range);
			}
			range = GetRange("PorteeMaximaleTBA", "PorteeMinimaleTBA", aor.Instance);
			if (range.A != 0 || range.B != 0)
			{
				Range.Add("Helo", range);
			}
			range = GetRange("PorteeMaximaleHA", "PorteeMinimaleHA", aor.Instance);
			if (range.A != 0 || range.B != 0)
			{
				Range.Add("Air", range);
			}
		}

		private UIntPair GetRange(string keyA, string keyB, NdfObject ndf)
		{
			UIntPair range = default(UIntPair);
			range.A = (uint)((double)NDFWrappers.ndfGetFloatByKey(keyA, ndf) / 74.285);
			range.B = (uint)((double)NDFWrappers.ndfGetFloatByKey(keyB, ndf) / 74.285);
			return range;
		}
	}

	private List<TurretMount> _mounts = new List<TurretMount>();

	public NdfObjectReference TurretRef { get; protected set; }

	public NdfCollection MountedList { get; protected set; }

	public uint RotationSpeed { get; protected set; }

	public uint RotationRange { get; protected set; }

	public uint RotationIdle { get; protected set; }

	public uint MinPitch { get; protected set; }

	public uint MaxPitch { get; protected set; }

	public uint IdlePitch { get; protected set; }

	public List<TurretMount> Mounts
	{
		get
		{
			return _mounts;
		}
		protected set
		{
			_mounts = value;
		}
	}

	public string Description
	{
		get
		{
			string descr = " * Turret" + Environment.NewLine;
			foreach (TurretMount tm in Mounts)
			{
				descr = descr + tm.Description + Environment.NewLine;
			}
			return descr + $"Turret rotation: Hor [{RotationRange}, {RotationSpeed} deg/sec, zero {RotationIdle}], Ver [{MinPitch}-{MaxPitch}, zero:{IdlePitch}]{Environment.NewLine}";
		}
	}

	public Turret(DataBase db, NdfObjectReference ndfObjectReference)
	{
		TurretRef = ndfObjectReference;
		RotationSpeed = (uint)((double)NDFWrappers.ndfGetFloatByKey("VitesseRotation", TurretRef.Instance) * (180.0 / Math.PI));
		RotationRange = (uint)((double)NDFWrappers.ndfGetFloatByKey("AngleRotationMax", TurretRef.Instance) * (180.0 / Math.PI));
		RotationIdle = (uint)((double)NDFWrappers.ndfGetFloatByKey("AngleRotationBase", TurretRef.Instance) * (180.0 / Math.PI));
		MinPitch = (uint)((double)NDFWrappers.ndfGetFloatByKey("AngleRotationMaxPitch", TurretRef.Instance) * (180.0 / Math.PI));
		MaxPitch = (uint)((double)NDFWrappers.ndfGetFloatByKey("AngleRotationMinPitch", TurretRef.Instance) * (180.0 / Math.PI));
		if (MaxPitch > 360)
		{
			MaxPitch = 0u;
		}
		IdlePitch = (uint)((double)NDFWrappers.ndfGetFloatByKey("AngleRotationBasePitch", TurretRef.Instance) * (180.0 / Math.PI));
		MountedList = (NdfCollection)NDFWrappers.ndfGetValueByKey("MountedWeaponDescriptorList", TurretRef.Instance.PropertyValues);
		if (MountedList == null || MountedList.Count <= 0)
		{
			return;
		}
		foreach (CollectionItemValueHolder v in MountedList)
		{
			Mounts.Add(new TurretMount(db, (NdfObjectReference)v.Value));
		}
	}
}
