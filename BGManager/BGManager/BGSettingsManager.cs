#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace BGManager;

public static class BGSettingsManager
{
	public static readonly string SettingsPath = "bgsettings.xml";

	public static BGSettings Load()
	{
		BGSettings settings = new BGSettings();
		if (!File.Exists(SettingsPath))
		{
			return settings;
		}
		XmlSerializer serializer = new XmlSerializer(typeof(BGSettings));
		using (FileStream fs = new FileStream(SettingsPath, FileMode.Open))
		{
			try
			{
				settings = serializer.Deserialize(fs) as BGSettings;
			}
			catch (InvalidOperationException ex)
			{
				Trace.TraceError($"Error while loading BGSettings: {ex}");
			}
		}
		return settings;
	}

	public static bool Save(BGSettings settingsToSave)
	{
		if (settingsToSave == null)
		{
			return false;
		}
		string dir = Path.GetDirectoryName(SettingsPath);
		if (dir != null && dir.Length > 0 && !Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		try
		{
			using FileStream fs = File.Create(SettingsPath);
			XmlSerializer serializer = new XmlSerializer(typeof(BGSettings));
			serializer.Serialize(fs, settingsToSave);
			fs.Flush();
		}
		catch (UnauthorizedAccessException uaex)
		{
			Trace.TraceError("Error while saving settings: {0}", uaex);
			return false;
		}
		catch (IOException ioex)
		{
			Trace.TraceError("Error while saving settings: {0}", ioex);
			return false;
		}
		return true;
	}
}
