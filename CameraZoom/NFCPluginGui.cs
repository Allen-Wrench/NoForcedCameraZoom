using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using Sandbox.Graphics.GUI;

namespace Camera.Zoom.Gui
{
	public class NFCPluginGui : MyGuiScreenBase
	{
		private NFCConfig Config => NoForcedCameraPlugin.Config;

		public NFCPluginGui() : base(new Vector2?(new Vector2(0.5f, 0.5f)), new Vector4?(MyGuiConstants.SCREEN_BACKGROUND_COLOR), new Vector2?(new Vector2(0.45f, 0.35f)))
		{
			CanHideOthers = false;
			EnabledBackgroundFade = false;
			CloseButtonEnabled = true;

			NoForcedCameraPlugin.LoadConfig();
			RecreateControls(true);
		}

		public override string GetFriendlyName()
		{
			return "NoForcedCameraZoomConfig";
		}

		public override void RecreateControls(bool constructor)
		{
			base.RecreateControls(constructor);
			MyGuiControlLabel title = new MyGuiControlLabel(position: new Vector2(0f, -0.142f), text: "No Forced Camera Zoom Settings", textScale: 1.0f, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
			Controls.Add(title);
			MyGuiControlLabel label = new MyGuiControlLabel(position: new Vector2(-0.11f, -0.072f), text: "Show BoundingBox Overlay");
			Controls.Add(label);
			MyGuiControlCheckbox bbCheckbox = new MyGuiControlCheckbox(position: new Vector2(0.1f, -0.07f), isChecked: Config.ShowBBOverlay);
			bbCheckbox.IsCheckedChanged = delegate (MyGuiControlCheckbox control) { Config.ShowBBOverlay = control.IsChecked; };
			Controls.Add(bbCheckbox);
			MyGuiControlSlider slider = new MyGuiControlSlider(position: new Vector2(0f, 0.02f), defaultValue: Config.AdjustmentSpeed, labelText: "Offset Adjustment Speed", showLabel: true);
			slider.MinValue = 0.01f;
			slider.ValueChanged = delegate (MyGuiControlSlider control) {  Config.AdjustmentSpeed = control.Value; };
			slider.CustomLabelPosition = true;
			slider.CustomLabelText = true;
			slider.Label.Position = new Vector2(slider.Position.X + 0.08f, slider.Position.Y - 0.055f);
			Controls.Add(slider);
			MyGuiControlLabel controlsLabel = new MyGuiControlLabel(position: new Vector2(0f, 0.08f), text: "Controls", textScale: 0.6f, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
			Controls.Add(controlsLabel);
			MyGuiControlMultilineText controlsText = new MyGuiControlMultilineText(position: new Vector2(0f, 0.131f), size: new Vector2(0.44f, 0.1f), backgroundColor: new Vector4?(MyGuiConstants.SCREEN_BACKGROUND_COLOR.ToGray()), contents: helpText, textScale: 0.6f);
			Controls.Add(controlsText);
		}

		private readonly StringBuilder helpText = new StringBuilder()
			.AppendLine("Enable third person camera re-positioning mode by holding Control and Shift.")
			.AppendLine("While holding Ctrl+Shift, use the block rotation controls to adjust the camera offset.")
			.AppendLine("Holding Ctrl+Shift and pressing backspace will reset the offset to default.");

		protected override void OnClosed()
		{
			NoForcedCameraPlugin.SaveConfig();
			base.OnClosed();
		}
	}
}
