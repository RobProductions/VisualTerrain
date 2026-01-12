using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeErosion : VTGraphNode
	{
		public override string NodeTitle => "Erosion";
		public override bool HasNodeProperties => true;

		public enum ErodeFormulaType
		{
			OneOverX = 0,
			EulerXPower1 = 1,
			EulerXPower2 = 2,
			EulerXPower3 = 3,
		}

		[SerializeField]
		public ErodeFormulaType erosionFormulaType = ErodeFormulaType.OneOverX;
		[SerializeField]
		public float erosionCurveSteepness = 0.5f;
		[SerializeField]
		public float erosionMultiplier = 1.0f;

		public VTGraphNodeErosion()
		{
			SetupEmptyInputConnections(2, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Base Input";
			inputConnections[1].connectionSlotName = "Erosion Mask";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var inputGrid = GetInputConnection(0).GetRangeGridValue();
			var erosionMaskGrid = GetInputConnection(1).GetRangeGridValue();

			//Handle grid erosion
			if(!inputGrid.IsNullOrEmpty() && !erosionMaskGrid.IsNullOrEmpty())
			{
				//We have both base input and erosion mask
				output.SetRangeGridValue(ProcessErosion(inputGrid, erosionMaskGrid, erosionFormulaType));
			}
			else
			{
				output.SetRangeGridValue(inputGrid);
			}

			//Handle float
			output.SetFloatValue(GetInputConnection().GetFloatValue());
		}

		//EROSION

		VTRangeGrid ProcessErosion(VTRangeGrid inputGrid, VTRangeGrid influenceMask, ErodeFormulaType formulaType)
		{
			var newGrid = new VTRangeGrid(inputGrid.Width, inputGrid.Height);

			for (int y = 0; y < inputGrid.Height; y++)
			{
				for(int x = 0; x < inputGrid.Width; x++)
				{
					//Early check for if we're in the influence range
					var influenceValue = influenceMask.GetRangeValue(x, y);
					var inputValue = inputGrid.GetRangeValue(x, y);

					float sinkAmount = 0.0f;
					if(formulaType == ErodeFormulaType.OneOverX)
					{
						//The formula 1 / 1 + kx where k is strength
						//will give a smooth step from 1 to 0,
						//but we want to remap to 0 to 1 so use 1 - formula
						sinkAmount = 1f - (1f / (1f + erosionCurveSteepness * influenceValue));
					}
					else if (formulaType == ErodeFormulaType.EulerXPower1)
					{
						//The formula e ^ (-kx ^ p) will give a smooth curve value
						//And we take 1 - formula to remap to 0 to 1
						sinkAmount = 1f - (Mathf.Exp(Mathf.Pow(-erosionCurveSteepness * influenceValue, 1)));
					}
					else if (formulaType == ErodeFormulaType.EulerXPower2)
					{
						sinkAmount = 1f - (Mathf.Exp(Mathf.Pow(-erosionCurveSteepness * influenceValue, 2)));
					}
					else if (formulaType == ErodeFormulaType.EulerXPower3)
					{
						sinkAmount = 1f - (Mathf.Exp(Mathf.Pow(-erosionCurveSteepness * influenceValue, 3)));
					}
					//Then subtract height based on erosion curve and multiplier
					newGrid.SetRangeValue(x, y, inputValue - (sinkAmount * erosionMultiplier));
				}
			}

			return newGrid;
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			ErodeFormulaType formulaValue = (ErodeFormulaType)EditorGUILayout.EnumPopup("Formula Type", erosionFormulaType);
			if (formulaValue != erosionFormulaType)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				erosionFormulaType = formulaValue;
				endEditNodePropertyEvent?.Invoke(this);
			}

			RenderFloatPropertyWithClamp("Erosion Curve", ref erosionCurveSteepness, 0f, 30f, 
				"The steepness of the curve used to calculate erosion sinking.");
			RenderFloatPropertyWithClamp("Erosion Multiplier", ref erosionMultiplier, -50f, 50f, 
				"The final amount of erosion that will be applied to the base input.");
		}
#endif
	}
}