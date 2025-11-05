using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
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

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			float selectValueFloat = (float)EditorGUILayout.FloatField("Select Value", selectValue);
			if (selectValueFloat != selectValue)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				selectValue = selectValueFloat;
				endEditNodePropertyEvent?.Invoke(this);
			}

			float toleranceFloat = Mathf.Clamp((float)EditorGUILayout.FloatField("Tolerance", tolerance), 0.0f, Mathf.Infinity);
			if (toleranceFloat != tolerance)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				tolerance = toleranceFloat;
				endEditNodePropertyEvent?.Invoke(this);
			}

			float smoothToleranceFloat = Mathf.Clamp((float)EditorGUILayout.FloatField("Smooth Tolerance", smoothTolerance), 0.0f, Mathf.Infinity);
			if (smoothToleranceFloat != smoothTolerance)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				smoothTolerance = smoothToleranceFloat;
				endEditNodePropertyEvent?.Invoke(this);
			}

			float strengthFloat = Mathf.Clamp((float)EditorGUILayout.FloatField("Mask Strength", maskStrength), 0.0f, Mathf.Infinity);
			if (strengthFloat != maskStrength)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				maskStrength = strengthFloat;
				endEditNodePropertyEvent?.Invoke(this);
			}


		}
	}
}