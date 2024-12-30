using System.Linq;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types;
using moddingSuite.Model.Ndfbin.Types.AllTypes;

namespace BGManager;

public class BGList : SortableBindingList<BG>
{
	public const string DivisionIconPrefix = "StrategicTexture_Division_";

	public const string BGIconPrefix = "StrategicTexture_BattleGroupLabel_";

	public DataBase.CampaignData CD { get; protected set; }

	public BGList(BGList all, FilterBG func)
	{
		foreach (BG bg in all)
		{
			if (func(bg))
			{
				bg.SortOwner = this;
				base.Items.Add(bg);
			}
		}
	}

	public BGList(DataBase.CampaignData cd, Progress progress)
	{
		CD = cd;
		bool progressDisplayed = !progress.Visible;
		NdfClass varClass = cd.NDFScripting.Classes.First((NdfClass name) => name.Name == "TModularUnitDescriptor");
		progress.SetJobTitle($"Loading {varClass.Instances.Count}:{cd.Name} battlegroups...");
		if (!progressDisplayed)
		{
			progress.Show();
		}
		progress.UpdateProgress(0, varClass.Instances.Count);
		int i = 0;
		foreach (NdfObject bg in varClass.Instances)
		{
			progress.UpdateProgress(i++, varClass.Instances.Count);
			BG battleGroup = new BG(this, bg, cd);
			if (battleGroup.Name.Length > 0)
			{
				base.Items.Add(battleGroup);
			}
		}
		varClass = cd.NDFAlternative.Classes.FirstOrDefault((NdfClass name) => name.Name == "TBattleGroupInstanceDescriptor");
		if (varClass != null)
		{
			foreach (NdfObject bgInitData in varClass.Instances)
			{
				NdfTrans v = (NdfTrans)NDFWrappers.ndfGetWrapperByKey(bgInitData, "Descriptor", NdfType.TransTableReference);
				string bgname = v.Value.ToString();
				string bgExportName = "";
				if (bgname.Length > 0)
				{
					bgExportName = NDFWrappers.ndfResolveImport(cd.NDFAlternative, bgname);
				}
				if (bgExportName.Length > 0)
				{
					base.Items.FirstOrDefault((BG fExport) => fExport.Export == bgExportName)?.ProcessInitData(bgInitData);
				}
			}
		}
		if (!progressDisplayed)
		{
			progress.Hide();
		}
	}
}
