using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Sandbox.Engine.Utils;

namespace Camera.Zoom
{
	[HarmonyPatch]
	public class CameraPatches
	{
		static CameraPatches()
		{
		}

		public CameraPatches()
		{
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "FindSafeStart")]
		public static IEnumerable<CodeInstruction> FindSafeStartTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = Enumerable.ToList<CodeInstruction>(instructions);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].opcode == OpCodes.Brfalse)
				{
					list[i].opcode = OpCodes.Brtrue;
					break;
				}
			}
			foreach (CodeInstruction codeInstruction in list)
			{
				yield return codeInstruction;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "IsCameraForced")]
		public static bool IsCameraForced(ref bool __result)
		{
			__result = false;
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "IsEntityFiltered")]
		public static bool IsEntityFiltered(ref bool __result)
		{
			__result = true;
			return false;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "RaycastOccludingObjects")]
		public static IEnumerable<CodeInstruction> RaycastOccludingObjectsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			yield return new CodeInstruction(OpCodes.Ldc_I4_0, null);
			yield return new CodeInstruction(OpCodes.Ret, null);
		}
	}
}
