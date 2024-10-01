using System;
using System.Reflection;
using HarmonyLib;
using Sandbox.Game.World;
using VRage.Plugins;
using VRageMath;

namespace Camera.Zoom
{
	public class NoForcedCameraPlugin : IDisposable, IPlugin
	{

		public NoForcedCameraPlugin()
		{
		}

		public void Dispose()
		{
		}

		public void Init(object gameInstance)
		{
			new Harmony("Camera.Zoom").PatchAll(Assembly.GetExecutingAssembly());

		}

		public void Update()
		{
		}

		public void OpenConfigDialog()
		{
			return;
		}
	}
}
