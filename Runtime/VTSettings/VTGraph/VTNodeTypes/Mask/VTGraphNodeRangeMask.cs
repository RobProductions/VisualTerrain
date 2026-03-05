using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNodeRangeMask : VTGraphNode
	{
		public override string NodeTitle => "Range Mask";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float selectValue = 0.5f;
		[SerializeField]
		public float tolerance = 0.05f;
		[SerializeField]
		public float smoothTolerance = 0.1f;
		[SerializeField]
		public float maskStrength = 1.0f;

		public VTGraphNodeRangeMask()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Input Value";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var input = GetInputConnection();
			var inputGrid = input.GetRangeGridValue();

			if(tolerance < 0)
			{
				tolerance = 0;
			}
			if(smoothTolerance < 0)
			{
				smoothTolerance = 0;
			}

			var resultGrid = new VTRangeGrid(inputGrid.Width, inputGrid.Height);
			for (int x = 0; x < inputGrid.Width; x++)
			{
				for (int y = 0; y < inputGrid.Height; y++)
				{
					var remapValue = PerformMaskRemap(inputGrid.GetRangeValue(x, y));
					resultGrid.SetRangeValue(x, y, remapValue);
				}
			}
			output.SetRangeGridValue(resultGrid);

			//Handle float case
			output.SetFloatValue(PerformMaskRemap(input.GetFloatValue()));
		}

		float PerformMaskRemap(float inputValue)
		{
			//Check if it's within the tolerance range
			var minAmount = selectValue - tolerance;
			var maxAmount = selectValue + tolerance;
			if(inputValue > minAmount && inputValue < maxAmount)
			{
				return 1.0f * maskStrength;
			}

			//Handle outside smoothing
			var minAmountSmooth = minAmount - smoothTolerance;
			var maxAmountSmooth = maxAmount + smoothTolerance;
			if (inputValue > minAmountSmooth && inputValue < maxAmountSmooth)
			{
				if (inputValue < selectValue)
				{
					//Leading up to select value
					var towardsInput = Mathf.InverseLerp(minAmountSmooth, minAmount, inputValue);
					return towardsInput * maskStrength;
				}
				else
				{
					//Away from select value
					var awayFromInput = (1f - Mathf.InverseLerp(maxAmount, maxAmountSmooth, inputValue));
					return awayFromInput * maskStrength;
				}
			}

			//Not within any range
			return 0.0f;
		}

		//RENDER PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatProperty("Select Value", ref selectValue,
				"The resulting mask will include points with this value.");

			RenderFloatPropertyWithClamp("Tolerance", ref tolerance, 0.0f, Mathf.Infinity,
				"The distance from the select value which will be treated as fully selected (value of 1.0f).");

			RenderFloatPropertyWithClamp("Smooth Tolerance", ref smoothTolerance, 0.0f, Mathf.Infinity,
				"Beyond this distance from the tolerance value, values within this range will smoothly fall off towards 0.");

			RenderFloatProperty("Mask Strength", ref maskStrength,
				"A multiplier that will be used on the final mask output.");
		}

#endif

	}
}