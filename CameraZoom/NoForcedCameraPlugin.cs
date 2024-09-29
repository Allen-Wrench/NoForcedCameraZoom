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
		public static Vector3I CameraOffset;

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
	}
}
