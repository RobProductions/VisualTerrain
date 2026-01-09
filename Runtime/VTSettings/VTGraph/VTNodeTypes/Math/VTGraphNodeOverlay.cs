using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeOverlay : VTGraphNode
	{
		public override string NodeTitle => "Overlay";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float overlayMultiplier = 1.0f;

		public VTGraphNodeOverlay()
		{
			SetupEmptyInputConnections(2, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Base Input";
			inputConnections[1].connectionSlotName = "Overlay Input";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var inputGrid = GetInputConnection(0).GetRangeGridValue();
			var overlayGrid = GetInputConnection(1).GetRangeGridValue();

			//Handle grid overlay
			if (!inputGrid.IsNullOrEmpty() && !overlayGrid.IsNullOrEmpty())
			{
				//We have both base input and overlay grid
				var newGrid = new VTRangeGrid(inputGrid.Width, inputGrid.Height);

				for (int y = 0; y < inputGrid.Height; y++)
				{
					for (int x = 0; x < inputGrid.Width; x++)
					{
						float baseValue = inputGrid.GetRangeValue(x, y);
						//Remap 0 to 1 to -1 to 1
						float remapValue = (overlayGrid.GetRangeValue(x, y) * 2.0f) - 1f;
						//Apply remap value with the given overlay strength
						newGrid.SetRangeValue(x, y, baseValue + (remapValue * overlayMultiplier));
					}
				}

				output.SetRangeGridValue(newGrid);
			}
			else
			{
				output.SetRangeGridValue(inputGrid);
			}

			//Handle float
			output.SetFloatValue(GetInputConnection().GetFloatValue());
		}

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatProperty("Overlay Multiplier", ref overlayMultiplier,
				"The overlay input is remapped from 0 to 1 to -1 to 1 and scaled by this multiplier before adding it to the base value.");
		}
	}
}