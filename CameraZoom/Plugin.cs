using System;
using HarmonyLib;
using VRage.Plugins;

namespace Camera.Zoom
{
	public class Plugin : IDisposable, IPlugin
	{
		public Plugin()
		{
		}

		public void Dispose()
		{
		}

		public void Init(object gameInstance)
		{
			new Harmony("Camera.Zoom").PatchAll();
		}

		public void Update()
		{
		}
	}
}
