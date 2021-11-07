using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Game.Entity;
using VRageMath;

namespace Custom.Patches
{
	[HarmonyPatch]
	public class CameraPatches
	{
		static CameraPatches()
		{
			CameraPatches.characterSpring = AccessTools.Field(typeof(MyThirdPersonSpectator), "NormalSpringCharacter");
			CameraPatches.currentCamRadius = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_currentCameraRadius");
			CameraPatches.normalSpring = AccessTools.Field(typeof(MyThirdPersonSpectator), "NormalSpring");
			CameraPatches.PerformZoomInOut = AccessTools.Method(typeof(MyThirdPersonSpectator), "PerformZoomInOut", null, null);
			CameraPatches.posSafe = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_positionCurrentIsSafeSinceTime");
			CameraPatches.target = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_target");
			CameraPatches.position = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_position");
			CameraPatches.safeMaxDistanceTimeout = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_safeMaximumDistanceTimeout");
			CameraPatches.safeMinDistance = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_safeMinimumDistance");
			CameraPatches.safeObb = AccessTools.Method(typeof(MyThirdPersonSpectator), "ComputeCompleteSafeOBB", null, null);
			CameraPatches.safeStart = AccessTools.Method(typeof(MyThirdPersonSpectator), "FindSafeStart", null, null);
			CameraPatches.max = 200.0;
			CameraPatches.min = 0.125;
		}

		public CameraPatches()
		{
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "ComputeEntitySafeOBB")]
		public static IEnumerable<CodeInstruction> ComputeEntitySafeOBBTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].opcode == OpCodes.Brfalse)
				{
					list[i].opcode = OpCodes.Br;
					break;
				}
			}
			foreach (CodeInstruction codeInstruction in list)
			{
				yield return codeInstruction;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "FindSafeStart")]
		public static IEnumerable<CodeInstruction> FindSafeStartTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
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
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "HandleIntersection")]
		public static bool HandleIntersectionPrefix(MyEntity controlledEntity, ref Vector3D lastTargetPos)
		{
			MyEntity myEntity = controlledEntity.GetTopMostParent(null) ?? controlledEntity;
			MyCubeGrid myCubeGrid = myEntity as MyCubeGrid;
			if (myCubeGrid != null && myCubeGrid.IsStatic)
			{
				myEntity = controlledEntity;
			}
			if (myEntity == null)
			{
				return false;
			}
			MyOrientedBoundingBoxD myOrientedBoundingBoxD = (MyOrientedBoundingBoxD)CameraPatches.safeObb.Invoke(MyThirdPersonSpectator.Static, AccessTools.all, null, new object[]
			{
				myEntity
			}, null);
			MyOrientedBoundingBoxD myOrientedBoundingBoxD2 = new MyOrientedBoundingBoxD(myOrientedBoundingBoxD.Center, myOrientedBoundingBoxD.HalfExtent + CameraPatches.min, myOrientedBoundingBoxD.Orientation);
			LineD lineD = default(LineD);
			LineD lineD2 = default(LineD);
			Vector3D vector3D = default(Vector3D);
			CameraPatches.safeStart.Invoke(MyThirdPersonSpectator.Static, AccessTools.all, null, new object[]
			{
				controlledEntity,
				lineD,
				myOrientedBoundingBoxD,
				myOrientedBoundingBoxD2,
				vector3D,
				lineD2
			}, null);
			CameraPatches.safeMinDistance.SetValue(MyThirdPersonSpectator.Static, CameraPatches.min);
			TimeSpan? time = CameraPatches.posSafe.GetValue(MyThirdPersonSpectator.Static) as TimeSpan?;
			if (time != null && time.Value == TimeSpan.MaxValue)
			{
				MyThirdPersonSpectator.Static.ResetInternalTimers();
				MyThirdPersonSpectator.Static.ResetSpring();
				CameraPatches.safeMaxDistanceTimeout.SetValue(MyThirdPersonSpectator.Static, 0f, AccessTools.all, null, null);
			}
			Vector3D vector3D2 = (Vector3D)CameraPatches.target.GetValue(MyThirdPersonSpectator.Static);
			Vector3D vector3D3 = (Vector3D)CameraPatches.position.GetValue(MyThirdPersonSpectator.Static);
			CameraPatches.PerformZoomInOut.Invoke(MyThirdPersonSpectator.Static, AccessTools.all, null, new object[]
			{
				vector3D2,
				vector3D3
			}, null);
			CameraPatches.PerformZoomInOut.Invoke(MyThirdPersonSpectator.Static, AccessTools.all, null, new object[]
			{
				vector3D2,
				vector3D3
			}, null);
			CameraPatches.posSafe.SetValue(MyThirdPersonSpectator.Static, MySession.Static.ElapsedGameTime);
			return false;
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
			yield break;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "RecalibrateCameraPosition")]
		public static IEnumerable<CodeInstruction> RecalibrateCameraPositionTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction codeInstruction in instructions)
			{
				if (codeInstruction.Is(OpCodes.Ldc_R8, 2.5))
				{
					yield return new CodeInstruction(OpCodes.Ldc_R8, CameraPatches.min);
				}
				else if (codeInstruction.Is(OpCodes.Ldc_R8, 200))
				{
					yield return new CodeInstruction(OpCodes.Ldc_R8, CameraPatches.max);
				}
				else if (codeInstruction.Calls(AccessTools.Method("VRageMath.BoundingBox:Inflate", new Type[]
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
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "ResetViewerDistance")]
		public static IEnumerable<CodeInstruction> ResetViewerDistanceTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction codeInstruction in instructions)
			{
				if (codeInstruction.Is(OpCodes.Ldc_R8, 2.5))
				{
					yield return new CodeInstruction(OpCodes.Ldc_R8, CameraPatches.min);
				}
				else if (codeInstruction.Is(OpCodes.Ldc_R8, 200))
				{
					yield return new CodeInstruction(OpCodes.Ldc_R8, CameraPatches.max);
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
				if (codeInstruction.LoadsField(CameraPatches.normalSpring, false))
				{
					yield return new CodeInstruction(OpCodes.Ldfld, CameraPatches.characterSpring);
				}
				else
				{
					yield return codeInstruction;
				}
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "UpdateZoom")]
		public static IEnumerable<CodeInstruction> UpdateZoomTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction codeInstruction in instructions)
			{
				if (codeInstruction.Is(OpCodes.Ldc_R8, 2.5))
				{
					yield return new CodeInstruction(OpCodes.Ldc_R8, CameraPatches.min);
				}
				else if (codeInstruction.Is(OpCodes.Ldc_R8, 200))
				{
					yield return new CodeInstruction(OpCodes.Ldc_R8, CameraPatches.max);
				}
				else
				{
					yield return codeInstruction;
				}
			}
		}

		private static FieldInfo characterSpring;

		private static FieldInfo currentCamRadius;

		public static double max;

		public static double min;

		private static FieldInfo normalSpring;

		private static MethodInfo PerformZoomInOut;

		private static FieldInfo position;

		private static FieldInfo posSafe;

		private static FieldInfo safeMaxDistanceTimeout;

		private static FieldInfo safeMinDistance;

		private static MethodInfo safeObb;

		private static MethodInfo safeStart;

		private static FieldInfo target;
	}
}
