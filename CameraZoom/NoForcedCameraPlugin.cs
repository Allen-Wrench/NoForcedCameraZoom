using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Camera.Zoom.Gui;
using HarmonyLib;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.GUI;
using VRage.Plugins;
using VRage.Utils;

namespace Camera.Zoom
{
	public class NoForcedCameraPlugin : IDisposable, IPlugin
	{
		public NoForcedCameraPlugin()
		{
			Config = new NFCConfig();
			try
			{
				if (!File.Exists(configFilePath))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(NFCConfig));
					serializer.Serialize(XmlWriter.Create(configFilePath), Config);
					NewUpdate = true;
				}
				else
				{
					if (LoadConfig() && latestPatchNotes != Config.LatestPatchNotes)
					{
						NewUpdate = true;
					}
				}
			}
			catch { }
		}

		public void Dispose()
		{
		}

		public void Init(object gameInstance)
		{
			new Harmony("Camera.Zoom").PatchAll(Assembly.GetExecutingAssembly());
			if (NewUpdate)
			{
				MyGuiScreenMainMenu.OnOpened += ShowNewUpdatePopup;
			}
		}

		public void Update()
		{
		}

		public void OpenConfigDialog()
		{
			MyScreenManager.AddScreen(new NFCPluginGui());
		}

		public static bool LoadConfig()
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(NFCConfig));
				Config = (NFCConfig)serializer.Deserialize(XmlReader.Create(configFilePath));
				return true;
			}
			catch { return false; }
		}

		public static void SaveConfig()
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(NFCConfig));
				serializer.Serialize(XmlWriter.Create(configFilePath), Config);
			}
			catch { }
		}

		private static void ShowNewUpdatePopup()
		{
			MyGuiScreenMainMenu.OnOpened -= ShowNewUpdatePopup;
			NewUpdate = false;
			Config.LatestPatchNotes = latestPatchNotes;
			MyGuiSandbox.Show(new StringBuilder(latestPatchNotes), MyStringId.GetOrCompute("No Forced Camera Zoom plugin has updated!"), MyMessageBoxStyleEnum.Info);
			SaveConfig();
		}

		public static NFCConfig Config;
		public static bool NewUpdate { get; private set; }

		private static readonly string configFilePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".xml");
		private static readonly string latestPatchNotes =
			"- New configuration UI accessible through the plugin menu." + "\n" +
			"- Possible fix to allow offsets to persist through a Nexus server change." + "\n" +
			"- These fancy new update notifications.";
	}
}
