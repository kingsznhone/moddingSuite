using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using moddingSuite.BL;
using moddingSuite.BL.Ndf;
using moddingSuite.Model.Edata;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types;
using moddingSuite.Model.Ndfbin.Types.AllTypes;
using moddingSuite.Model.Trad;

namespace BGManager;

public class NDFWrappers
{
	public static void ndfLoadBinary(EdataManager edataManager, string key, ref NdfBinary ndf, ref EdataContentFile contentFile)
	{
		ObservableCollection<EdataContentFile> _files = edataManager.Files;
		foreach (EdataContentFile file in _files)
		{
			if (file.Path.IndexOf(key) >= 0)
			{
				NdfbinReader ndfbinReader = new NdfbinReader();
				ndf = ndfbinReader.Read(edataManager.GetRawData(file));
				contentFile = file;
				break;
			}
		}
	}

	public static ObservableCollection<TradEntry> ndfLoadHash(EdataManager edataManager, string key)
	{
		ObservableCollection<EdataContentFile> _files = edataManager.Files;
		foreach (EdataContentFile file in _files)
		{
			if (file.Path.IndexOf(key) >= 0)
			{
				TradManager tMan = new TradManager(edataManager.GetRawData(file));
				return tMan.Entries;
			}
		}
		return null;
	}

	public static string ndfGetString(NdfValueWrapper p)
	{
		if (p.Type == NdfType.TableString)
		{
			NdfString nS = (NdfString)p;
			return (nS.Value != null) ? nS.ToString() : "";
		}
		if (p.Type == NdfType.WideString)
		{
			NdfWideString nS2 = (NdfWideString)p;
			return nS2.ToString();
		}
		if (p.Type == NdfType.Unset)
		{
			return "";
		}
		return p.Type.ToString();
	}

	public static string ndfGetHashString(NdfValueWrapper p, ObservableCollection<TradEntry> hash)
	{
		if (p.Type == NdfType.LocalisationHash)
		{
			NdfLocalisationHash nS = (NdfLocalisationHash)p;
			TradEntry te = hash.FirstOrDefault((TradEntry HashSet) => HashSet.Hash.SequenceEqual(nS.Value));
			if (te != null)
			{
				return te.Content.ToString();
			}
		}
		else if (p.Type == NdfType.Unset)
		{
			return "";
		}
		return p.Type.ToString();
	}

	public static int ndfGetInt(NdfValueWrapper p)
	{
		if (p != null && p.Type == NdfType.Int32)
		{
			NdfInt32 v = (NdfInt32)p;
			return (int)v.Value;
		}
		if (p != null && p.Type == NdfType.UInt32)
		{
			NdfUInt32 v2 = (NdfUInt32)p;
			return (int)(uint)v2.Value;
		}
		return 0;
	}

	public static int ndfGetNumber(NdfValueWrapper p)
	{
		if (p != null && p.Type == NdfType.Int32)
		{
			NdfInt32 v = (NdfInt32)p;
			return (int)v.Value;
		}
		if (p != null && p.Type == NdfType.UInt32)
		{
			NdfUInt32 v2 = (NdfUInt32)p;
			return (int)(uint)v2.Value;
		}
		if (p != null && p.Type == NdfType.Float32)
		{
			NdfSingle v3 = (NdfSingle)p;
			return (int)v3.Value;
		}
		return 0;
	}

	public static float ndfGetFloat(NdfValueWrapper p)
	{
		if (p != null && p.Type == NdfType.Float32)
		{
			NdfSingle v = (NdfSingle)p;
			return v.Value;
		}
		return 0f;
	}

	public static uint ndfGetUInt32(NdfValueWrapper p)
	{
		if (p.Type == NdfType.UInt32)
		{
			NdfUInt32 v = (NdfUInt32)p;
			return (uint)v.Value;
		}
		return 0u;
	}

	public static uint ndfGetUIntByKey(string key, NdfObject values)
	{
		foreach (NdfPropertyValue value in values.PropertyValues)
		{
			string name = value.Property.Name;
			if (name == key && value.Value != null)
			{
				return ndfGetUInt32(value.Value);
			}
		}
		return 0u;
	}

	public static int ndfGetIntByKey(string key, NdfObject values)
	{
		foreach (NdfPropertyValue value in values.PropertyValues)
		{
			string name = value.Property.Name;
			if (name == key && value.Value != null)
			{
				return ndfGetInt(value.Value);
			}
		}
		return 0;
	}

	public static float ndfGetFloatByKey(string key, NdfObject values)
	{
		if (key != null && key.Length > 0 && values != null && values.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == key) != null)
		{
			NdfValueWrapper v = values.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == key).Value;
			if (v.Type == NdfType.Float32)
			{
				return ((NdfSingle)v).Value;
			}
		}
		return 0f;
	}

	public static string ndfGetStringByKey(string key, NdfObject values)
	{
		foreach (NdfPropertyValue value in values.PropertyValues)
		{
			string name = value.Property.Name;
			if (name == key && value.Value != null)
			{
				return ndfGetString(value.Value);
			}
		}
		return "";
	}

	public static string ndfGetHashStringByKey(string key, NdfObject value, ObservableCollection<TradEntry> hash)
	{
		NdfPropertyValue v = value.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == key);
		return (v != null && v.Value != null) ? ndfGetHashString(v.Value, hash) : "";
	}

	public static List<int> ndfGetIntList(NdfCollection values)
	{
		List<int> list = new List<int>();
		foreach (CollectionItemValueHolder v in values)
		{
			list.Add(ndfGetInt(v.Value));
		}
		return list;
	}

	public static List<string> ndfGetHashStringList(NdfCollection values, ObservableCollection<TradEntry> hash)
	{
		List<string> slist = new List<string>();
		foreach (CollectionItemValueHolder v in values)
		{
			slist.Add(ndfGetHashString(v.Value, hash));
		}
		return slist;
	}

	public static List<int> ndfGetIntListByKey(string key, NdfObject values)
	{
		foreach (NdfPropertyValue value in values.PropertyValues)
		{
			string name = value.Property.Name;
			if (name == key && value.Value != null && value.Type == NdfType.List)
			{
				return ndfGetIntList((NdfCollection)value.Value);
			}
		}
		return null;
	}

	public static NdfValueWrapper ndfGetWrapperByKey(NdfObject obj, string key, NdfType ndfType)
	{
		NdfPropertyValue value = obj.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == key);
		return (value == null || value.Type == NdfType.Unset) ? null : value.Value;
	}

	public static NdfCollection ndfGetCollectionByKey(string key, ObservableCollection<NdfPropertyValue> properties)
	{
		NdfPropertyValue v = properties.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == key);
		if (v != null && v.Type == NdfType.List)
		{
			return (NdfCollection)v.Value;
		}
		return null;
	}

	public static NdfValueWrapper ndfGetValueFromMap(string key, NdfCollection mapCollection)
	{
		foreach (CollectionItemValueHolder token in mapCollection)
		{
			if (token.Value.Type != NdfType.Map)
			{
				int i = 0;
				if (int.TryParse(key, out i) && i >= 0 && i < mapCollection.Count)
				{
					return mapCollection[i].Value;
				}
				return null;
			}
			NdfMap map = (NdfMap)token.Value;
			string mapKey = ndfGetString(map.Key.Value);
			if (key == mapKey)
			{
				MapValueHolder v = (MapValueHolder)map.Value;
				return v.Value;
			}
		}
		return null;
	}

	public static NdfValueWrapper ndfGetValueByKey(string key, ObservableCollection<NdfPropertyValue> propertyValues)
	{
		NdfPropertyValue v = propertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == key);
		if (v != null && v.Value != null && v.Type != NdfType.Unknown && v.Type != NdfType.Unset)
		{
			return v.Value;
		}
		return null;
	}

	public static bool ndfGetBoolIsTrueByKey(string key, NdfObject ndfObject)
	{
		ObservableCollection<NdfPropertyValue> propertyValues = ndfObject.PropertyValues;
		NdfPropertyValue v = propertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == key);
		if (v != null && v.Value != null && v.Type == NdfType.Boolean)
		{
			return (bool)((NdfBoolean)v.Value).Value;
		}
		return false;
	}

	public static void ndfSetString(ObservableCollection<NdfStringReference> strings, NdfObject ndfObject, string propName, string value)
	{
		NdfStringReference sr = strings.FirstOrDefault((NdfStringReference fValue) => fValue.Value == value);
		if (sr == null)
		{
			sr = new NdfStringReference();
			sr.Value = value;
			int oldCount = strings.Count;
			sr.Id = strings.Count;
			strings.Add(sr);
			Gauges.Log("ndfSetString(was {0} now {1} items) - added string [{2}]", oldCount, strings.Count, value);
		}
		ndfSetPropertyValue(ndfObject, propName, new NdfString(sr));
	}

	public static void ndfSetPropertyValue(NdfObject ndfObject, string propName, NdfValueWrapper value)
	{
		NdfPropertyValue pv = ndfObject.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == propName);
		if (pv != null)
		{
			pv.Value = value;
		}
	}

	public static void ndfSetHashString(ObservableCollection<TradEntry> hashList, NdfValueWrapper hash, string newValue)
	{
		if (hash.Type == NdfType.LocalisationHash)
		{
			NdfLocalisationHash lh = (NdfLocalisationHash)hash;
			TradEntry te = hashList.FirstOrDefault((TradEntry HashSet) => HashSet.Hash.SequenceEqual(lh.Value));
			if (te != null)
			{
				te.Content = newValue;
				te.ContLen = (uint)newValue.Length;
			}
		}
	}

	public static void ndfSetMapValue(NdfBinary ndfBinary, NdfCollection mapCollection, string key, NdfValueWrapper value)
	{
		foreach (CollectionItemValueHolder token in mapCollection)
		{
			NdfMap map = (NdfMap)token.Value;
			string mapKey = ndfGetString(map.Key.Value);
			if (key == mapKey)
			{
				map.Value = new MapValueHolder(value, ndfBinary);
			}
		}
	}

	public static NdfObject ndfCopyInstance(NdfObject instToCopy)
	{
		NdfObject newInst = instToCopy.Class.Manager.CreateInstanceOf(instToCopy.Class, instToCopy.IsTopObject);
		foreach (NdfPropertyValue propertyValue in instToCopy.PropertyValues)
		{
			if (propertyValue.Type != NdfType.Unset)
			{
				NdfPropertyValue receiver = newInst.PropertyValues.Single((NdfPropertyValue x) => x.Property == propertyValue.Property);
				receiver.Value = GetCopiedValue(propertyValue);
			}
		}
		instToCopy.Class.Instances.Add(newInst);
		return newInst;
	}

	private static NdfValueWrapper GetCopiedValue(IValueHolder toCopy)
	{
		NdfValueWrapper copiedValue = null;
		switch (toCopy.Value.Type)
		{
		case NdfType.ObjectReference:
			copiedValue = ((!(toCopy.Value is NdfObjectReference origInst) || origInst.Instance.IsTopObject) ? NdfTypeManager.GetValue(toCopy.Value.GetBytes(), toCopy.Value.Type, toCopy.Manager) : new NdfObjectReference(origInst.Class, ndfCopyInstance(origInst.Instance).Id));
			break;
		case NdfType.List:
		case NdfType.MapList:
		{
			List<CollectionItemValueHolder> copiedItems = new List<CollectionItemValueHolder>();
			if (toCopy.Value is NdfCollection collection)
			{
				copiedItems.AddRange(collection.Select((CollectionItemValueHolder entry) => new CollectionItemValueHolder(GetCopiedValue(entry), toCopy.Manager)));
			}
			copiedValue = new NdfCollection(copiedItems);
			break;
		}
		case NdfType.Map:
			if (toCopy.Value is NdfMap map)
			{
				copiedValue = new NdfMap(new MapValueHolder(GetCopiedValue(map.Key), toCopy.Manager), new MapValueHolder(GetCopiedValue(map.Value as IValueHolder), toCopy.Manager), toCopy.Manager);
			}
			break;
		default:
			copiedValue = NdfTypeManager.GetValue(toCopy.Value.GetBytes(), toCopy.Value.Type, toCopy.Manager);
			break;
		}
		return copiedValue;
	}

	public static string ndfResolveImport(NdfBinary ndfBin, string transTableKey)
	{
		NdfTranReference obj = ndfBin.Trans.FirstOrDefault((NdfTranReference Value) => Value.Value == transTableKey);
		if (obj == null)
		{
			return "";
		}
		int trnIdx = ndfBin.Trans.IndexOf(obj);
		int ieIdx = 0;
		uint lowerIdx = 0u;
		foreach (uint pairIdx in ndfBin.Import)
		{
			if (ieIdx > 1 && pairIdx == trnIdx && (ndfBin.Import[ieIdx - 2] == 0 || (ieIdx + 1 < ndfBin.Import.Count && ndfBin.Import[ieIdx + 1] == 0)))
			{
				lowerIdx = ndfBin.Import[ieIdx - 1];
				if (lowerIdx >= ndfBin.Import.Count)
				{
					lowerIdx = 0u;
				}
				if (lowerIdx != 0)
				{
					break;
				}
			}
			ieIdx++;
		}
		return (lowerIdx == 0 || lowerIdx >= ndfBin.Trans.Count) ? "" : ndfBin.Trans[(int)lowerIdx].Value;
	}

	public static NdfTrans ndfCreateTransReference(NdfBinary ndfBin, string objName, string extraString = "")
	{
		NdfTranReference trans = new NdfTranReference();
		if (objName == null || objName.Length == 0)
		{
			return null;
		}
		NdfTranReference obj = ndfBin.Trans.FirstOrDefault((NdfTranReference fValue) => fValue.Value == objName);
		if (obj == null)
		{
			if (extraString == null || extraString.Length == 0)
			{
				return null;
			}
			NdfTranReference tRef = null;
			if (ndfBin.Trans.FirstOrDefault((NdfTranReference fName) => fName.Value == extraString) == null)
			{
				tRef = new NdfTranReference();
				tRef.Value = extraString;
				tRef.Id = ndfBin.Trans.Count + 1;
				ndfBin.Trans.Add(tRef);
				tRef = new NdfTranReference();
				tRef.Value = objName;
				tRef.Id = ndfBin.Trans.Count - 1;
				ndfBin.Trans.Add(tRef);
				ndfBin.Import.Add((uint)(tRef.Id + 1));
				ndfBin.Import.Add((uint)tRef.Id);
				ndfBin.Import.Add(0u);
				return ndfCreateTransReference(ndfBin, objName);
			}
			return null;
		}
		int upperIdx = ndfBin.Trans.IndexOf(obj);
		uint lowerIdx = 0u;
		int i = 0;
		foreach (uint pairIdx in ndfBin.Import)
		{
			if (i > 0 && pairIdx == upperIdx && ndfBin.Import[i - 1] == 0)
			{
				lowerIdx = ndfBin.Import[i + 1];
				if (lowerIdx >= ndfBin.Import.Count)
				{
					lowerIdx = 0u;
				}
				if (lowerIdx != 0)
				{
					break;
				}
			}
			i++;
		}
		trans.Value = ndfBin.Trans[(int)lowerIdx].Value;
		trans.Id = (int)lowerIdx;
		return new NdfTrans(trans);
	}

	public static string ndfResolveExport(NdfBinary ndfBin, uint id)
	{
		int i = 0;
		uint exportId = 0u;
		foreach (uint v in ndfBin.Export)
		{
			if (i > 0 && v == id)
			{
				exportId = ndfBin.Export[i - 1];
				if (exportId >= ndfBin.Trans.Count)
				{
					exportId = 0u;
				}
				if (exportId != 0)
				{
					break;
				}
			}
			i++;
		}
		return (exportId == 0) ? "" : ndfBin.Trans[(int)exportId].Value;
	}

	public static string ndfGetTransTable(NdfObject ndfObj, NdfBinary ndfBin, string key)
	{
		NdfValueWrapper t = ndfGetWrapperByKey(ndfObj, key, NdfType.TransTableReference);
		if (t == null)
		{
			return "";
		}
		if (t.Type != NdfType.TransTableReference)
		{
			return t.Type.ToString();
		}
		NdfTrans v = (NdfTrans)t;
		string result = "";
		if (v != null && v.Value != null)
		{
			result = v.Value.ToString();
			if (result.Length > 0)
			{
				result = ndfResolveImport(ndfBin, result);
			}
		}
		return result;
	}

	public static NdfObjectReference ndfCreateObjectReference(NdfBinary ndfBinary, string className, uint id)
	{
		NdfObjectReference objRef = null;
		NdfClass ndfClass = ndfBinary.Classes.FirstOrDefault((NdfClass fName) => fName.Name == className);
		if (ndfClass != null && ndfClass.Instances.FirstOrDefault((NdfObject fId) => fId.Id == id) != null)
		{
			objRef = new NdfObjectReference(ndfClass, id);
		}
		return objRef;
	}

	public static bool ndfCheckChain(NdfObject ndfObject, List<string> chain)
	{
		NdfValueWrapper value = null;
		NdfObject ndfObj = ndfObject;
		NdfPropertyValue propValue = null;
		int i;
		for (i = 0; i < chain.Count; i++)
		{
			if (ndfObj == null)
			{
				return false;
			}
			string chainLink = chain[i];
			if (propValue == null)
			{
				propValue = ndfObj.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == chain[i]);
				if (propValue == null)
				{
					return false;
				}
			}
			else if (propValue.Type == NdfType.List)
			{
				value = ndfGetValueFromMap(chain[i], (NdfCollection)propValue.Value);
				if (value == null)
				{
					return false;
				}
				if (value.Type != NdfType.ObjectReference)
				{
					return false;
				}
				ndfObj = ((NdfObjectReference)value).Instance;
				propValue = null;
			}
			else
			{
				if (propValue.Type != NdfType.ObjectReference)
				{
					return false;
				}
				NdfObjectReference or = (NdfObjectReference)propValue.Value;
				ndfObj = or.Instance;
				propValue = ndfObj.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == chain[i]);
			}
		}
		return (propValue != null) ? (propValue.Value != null) : (ndfObj != null);
	}

	public static NdfValueWrapper ndfGetValueByChain(NdfObject ndfObject, List<string> chain)
	{
		NdfValueWrapper value = null;
		NdfObject ndfObj = ndfObject;
		NdfPropertyValue propValue = null;
		int i;
		for (i = 0; i < chain.Count; i++)
		{
			if (ndfObj == null)
			{
				return null;
			}
			string chainLink = chain[i];
			if (propValue == null)
			{
				propValue = ndfObj.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == chain[i]);
				if (propValue == null)
				{
					return null;
				}
			}
			else if (propValue.Type == NdfType.List)
			{
				value = ndfGetValueFromMap(chain[i], (NdfCollection)propValue.Value);
				if (value == null)
				{
					return null;
				}
				if (value.Type == NdfType.ObjectReference)
				{
					ndfObj = ((NdfObjectReference)value).Instance;
					if (i + 1 == chain.Count)
					{
						return value;
					}
				}
				else if (value.Type != NdfType.Unknown && value.Type != NdfType.Unknown && i + 1 == chain.Count)
				{
					return value;
				}
				propValue = null;
			}
			else
			{
				if (propValue.Type != NdfType.ObjectReference)
				{
					return null;
				}
				NdfObjectReference or = (NdfObjectReference)propValue.Value;
				ndfObj = or.Instance;
				propValue = ndfObj.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == chain[i]);
			}
		}
		return propValue?.Value;
	}
}
