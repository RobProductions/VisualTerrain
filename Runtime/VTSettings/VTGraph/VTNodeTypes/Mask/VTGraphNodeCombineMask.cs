using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeCombineMask : VTGraphNode
	{
		public override string NodeTitle => "Combine Mask";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float maskStrength = 1.0f;
		[SerializeField]
		public bool setValueAsMask = false;

		public VTGraphNodeCombineMask()
		{
			SetupEmptyInputConnections(3, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Base Value";
			inputConnections[1].connectionSlotName = "Set Value";
			inputConnections[2].connectionSlotName = "Mask";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var baseInputGrid = GetInputConnection(0).GetRangeGridValue();
			var setInputGrid = GetInputConnection(1).GetRangeGridValue();
			var maskInputGrid = GetInputConnection(2).GetRangeGridValue();

			//Handle RangeGrid mask
			var newGrid = new VTRangeGrid(baseInputGrid.Width, baseInputGrid.Height);

			if(!baseInputGrid.IsNullOrEmpty())
			{
				for (int x = 0; x < baseInputGrid.Width; x++)
				{
					for (int y = 0; y < baseInputGrid.Height; y++)
					{
						var basePixel = baseInputGrid.GetRangeValue(x, y);
						var setPixel = 0f;
						if (!setInputGrid.IsNullOrEmpty())
						{
							setPixel = setInputGrid.GetRangeValue(x, y);
						}
						var maskPixel = 0f;
						if (!maskInputGrid.IsNullOrEmpty())
						{
							maskPixel = maskInputGrid.GetRangeValue(x, y);
						}

						newGrid.SetRangeValue(x, y, GetInterpolatedSetValue(basePixel, setPixel, maskPixel, setValueAsMask || maskInputGrid.IsNullOrEmpty()));
					}
				}
			}

			output.SetRangeGridValue(newGrid);

			//Handle Float mask
			var baseInputFloat = GetInputConnection(0).GetFloatValue();
			var setInputFloat = GetInputConnection(1).GetFloatValue();
			var maskInputFloat = GetInputConnection(2).GetFloatValue();

			output.SetFloatValue(GetInterpolatedSetValue(baseInputFloat, setInputFloat, maskInputFloat, setValueAsMask));
		}

		float GetInterpolatedSetValue(float baseValue, float setValue, float maskInterpolation, bool maskOnSetValue)
		{
			if(maskOnSetValue)
			{
				//Just use the set value if it is above 0
				if(setValue == 0)
				{
					return baseValue;
				}
				return setValue;
			}
			//Lerp between the original and new value
			//based on the mask value
			return Mathf.Lerp(baseValue, setValue, maskInterpolation * maskStrength);
		}

		//RENDER PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatPropertyWithClamp("Mask Strength", ref maskStrength, 0.0f, Mathf.Infinity);
			RenderBoolProperty("Set Value Is Mask", ref setValueAsMask);
		}
#endif
	}
}