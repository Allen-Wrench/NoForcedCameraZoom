using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game.Entity;
using VRage.Game.Utils;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Camera.Zoom
{
	[HarmonyPatch]
	public class CameraPatches
	{
		private static FieldInfo clampedlookAt;
		private static FieldInfo lookAt;
		private static FieldInfo characterSpring;
		private static FieldInfo normalSpring;
		private static FieldInfo target;
		private static FieldInfo lastControllerEntity;
		private static Func<IMyControllableEntity, MyEntity> getControlledEntity;

		public static Vector3D currentCameraOffset;
		public static Dictionary<long, Vector3D> offsetStorage;

		private static Dictionary<MyStringId, Vector3D> adjustControls;

		static CameraPatches()
		{
			characterSpring = AccessTools.Field(typeof(MyThirdPersonSpectator), "NormalSpringCharacter");
			normalSpring = AccessTools.Field(typeof(MyThirdPersonSpectator), "NormalSpring");
			lookAt = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_lookAt");
			clampedlookAt = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_clampedlookAt");
			target = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_target");
			lastControllerEntity = AccessTools.Field(typeof(MyThirdPersonSpectator), "m_lastControllerEntity");
			getControlledEntity = AccessTools.MethodDelegate<Func<IMyControllableEntity, MyEntity>>(AccessTools.Method(typeof(MyThirdPersonSpectator), "GetControlledEntity"));
			offsetStorage = new Dictionary<long, Vector3D>();
			adjustControls = new Dictionary<MyStringId, Vector3D>();
			adjustControls[MyStringId.GetOrCompute("CUBE_ROTATE_HORISONTAL_POSITIVE")] = new Vector3D(1, 0, 0);
			adjustControls[MyStringId.GetOrCompute("CUBE_ROTATE_HORISONTAL_NEGATIVE")] = new Vector3D(-1, 0, 0);
			adjustControls[MyStringId.GetOrCompute("CUBE_ROTATE_VERTICAL_POSITIVE")] = new Vector3D(0, 1, 0);
			adjustControls[MyStringId.GetOrCompute("CUBE_ROTATE_VERTICAL_NEGATIVE")] = new Vector3D(0, -1, 0);
			adjustControls[MyStringId.GetOrCompute("CUBE_ROTATE_ROLL_POSITIVE")] = new Vector3D(0, 0, 1);
			adjustControls[MyStringId.GetOrCompute("CUBE_ROTATE_ROLL_NEGATIVE")] = new Vector3D(0, 0, -1);
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
				else if (codeInstruction.StoresField(target))
				{
					yield return new CodeInstruction(OpCodes.Ldloc_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraPatches), nameof(TargetOffset)));
					yield return codeInstruction;
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
				else if (codeInstruction.StoresField(target))
				{
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraPatches), nameof(TargetOffset)));
					yield return codeInstruction;
				}
				else if (codeInstruction.StoresField(lastControllerEntity))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraPatches), nameof(ControllerChanged)));
					yield return codeInstruction;
				}
				else if (codeInstruction.opcode == OpCodes.Ret)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, target);
					yield return new CodeInstruction(OpCodes.Ldloc_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraPatches), nameof(ChangeOffset)));
					yield return codeInstruction;
				}
				else
				{
					yield return codeInstruction;
				}
			}
		}


		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MyThirdPersonSpectator), "SetPositionAndLookAt")]
		public static IEnumerable<CodeInstruction> SetPositionAndLookAtTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction codeInstruction in instructions)
			{
				if (codeInstruction.StoresField(target))
				{
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraPatches), nameof(TargetOffset)));
				}
				yield return codeInstruction;
			}
		}

		public static Vector3D TargetOffset(Vector3D target, IMyControllableEntity entity)
		{
			if (entity != null && entity.Entity is MyTerminalBlock && (entity.Entity as MyTerminalBlock).CubeGrid != null && offsetStorage.ContainsKey(entity.Entity.EntityId))
			{
				MyCubeGrid grid = (entity.Entity as MyTerminalBlock).CubeGrid;
				Vector3D vec = target - grid.PositionComp.GetPosition();
				return target + Vector3D.TransformNormal(currentCameraOffset, grid.WorldMatrix) - vec;
			}
			return target;
		}

		public static IMyControllableEntity ControllerChanged(IMyControllableEntity entity)
		{
			currentCameraOffset = Vector3D.Zero;

			if (offsetStorage.ContainsKey(entity.Entity.EntityId))
			{
				currentCameraOffset = offsetStorage[entity.Entity.EntityId];
			}

			return entity;
		}

		public static void ChangeOffset(Vector3D target, MyEntity controlledEntity)
		{
			if (controlledEntity == null || controlledEntity is MyCharacter) 
				return;

			MyTerminalBlock block = controlledEntity as MyTerminalBlock;
			if (block == null)
				return;

			MyCubeGrid grid = block.CubeGrid;
			if (grid == null || grid.PositionComp == null)
				return;

			Vector3D min = grid.PositionComp.LocalAABB.Min;
			Vector3D max = grid.PositionComp.LocalAABB.Max;

			if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsAnyShiftKeyPressed())
			{
				BoundingBox localAABB = grid.PositionComp.LocalAABB;
				MatrixD matrixD = grid.WorldMatrix;
				MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(localAABB, matrixD);
				MyRenderProxy.DebugDrawOBB(obb, Color.Lime, 0.01f, false, true, false);
				MyRenderProxy.DebugDrawSphere(target, 0.5f, Color.Red, 2f, false, true);
				if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad0) && offsetStorage.ContainsKey(controlledEntity.EntityId))
				{
					currentCameraOffset = Vector3D.Zero;
					offsetStorage.Remove(controlledEntity.EntityId);
					return;
				}
				foreach (KeyValuePair<MyStringId, Vector3D> item in adjustControls)
				{
					if (MyInput.Static.IsNewGameControlPressed(item.Key))
					{
						currentCameraOffset += item.Value;
						currentCameraOffset = Vector3D.Clamp(currentCameraOffset, min, max);
						offsetStorage[controlledEntity.EntityId] = currentCameraOffset;
						break;
					}
				}
			}

			return;
		}
	}
}
