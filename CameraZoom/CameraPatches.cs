using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Sandbox.Engine.Utils;
using VRageMath;

namespace Camera.Zoom
{
	[HarmonyPatch]
	public class CameraPatches
	{
		static CameraPatches()
		{
			characterSpring = AccessTools.Field(typeof(MyThirdPersonSpectator), "NormalSpringCharacter");
			normalSpring = AccessTools.Field(typeof(MyThirdPersonSpectator), "NormalSpring");
			lookAt = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_lookAt");
			clampedlookAt = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_clampedlookAt");
		}

		public static BoundingBox Fix(BoundingBox bb)
		{
			bb.Scale(new Vector3(0.0001f));
			return bb;
		}

		public static BoundingBoxD Fix2(BoundingBoxD worldAABB)
		{
			return new BoundingBoxD(worldAABB.Center - new Vector3D(0.0001), worldAABB.Center + new Vector3D(0.0001));
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "ComputeEntitySafeOBB")]
		public static IEnumerable<CodeInstruction> ComputeEntitySafeOBBTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction code in instructions)
			{
				if (code.opcode == OpCodes.Stloc_1)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraPatches), "Fix", null, null));
					yield return code;
				}
				else if (code.LoadsField(AccessTools.Field(typeof(MyThirdPersonSpectator), "m_safeAABB")))
				{
					yield return code;
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraPatches), "Fix2", null, null));
				}
				else
					yield return code;
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

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "MergeAABB")]
		public static bool MergeAABB()
		{
			return false;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "RaycastOccludingObjects")]
		public static IEnumerable<CodeInstruction> RaycastOccludingObjectsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			yield return new CodeInstruction(OpCodes.Ldc_I4_0, null);
			yield return new CodeInstruction(OpCodes.Ret, null);
			yield break;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "RecalibrateCameraPosition")]
		public static IEnumerable<CodeInstruction> RecalibrateCameraPositionTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction codeInstruction in instructions)
			{
				if (codeInstruction.Calls(AccessTools.Method("VRageMath.BoundingBox:Inflate", new Type[]
				{
					typeof(float)
				}, null)))
				{
					yield return new CodeInstruction(OpCodes.Pop, null);
					yield return new CodeInstruction(OpCodes.Pop, null);
				}
				else
				{
					yield return codeInstruction;
				}
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "Update")]
		public static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction codeInstruction in instructions)
			{
				if (codeInstruction.LoadsField(normalSpring, false))
				{
					yield return new CodeInstruction(OpCodes.Ldfld, characterSpring);
				}
				else if (codeInstruction.LoadsField(clampedlookAt, false))
				{
					yield return new CodeInstruction(OpCodes.Ldfld, lookAt);
				}
				else
				{
					yield return codeInstruction;
				}
			}
		}

		private static FieldInfo clampedlookAt;
		private static FieldInfo lookAt;
		private static FieldInfo characterSpring;
		private static FieldInfo normalSpring;
	}
}
