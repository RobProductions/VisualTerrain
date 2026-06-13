using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNodeCombineMask : VTGraphNode
	{
		public override string NodeTitle => "Combine Mask";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float maskStrength = 1.0f;
		[SerializeField]
		public bool setValueAboveZero = false;

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

			var baseInputFloat = GetInputConnection(0).GetFloatValue();
			var setInputFloat = GetInputConnection(1).GetFloatValue();
			var maskInputFloat = GetInputConnection(2).GetFloatValue();

			//Handle RangeGrid mask
			var newGrid = new VTRangeGrid(baseInputGrid.Width, baseInputGrid.Height);

			if(!baseInputGrid.IsNullOrEmpty())
			{
				for (int x = 0; x < baseInputGrid.Width; x++)
				{
					for (int y = 0; y < baseInputGrid.Height; y++)
					{
						var basePixel = baseInputGrid.GetRangeValue(x, y);

						//Gather the set value
						var setPixel = 0f;
						if(GetInputConnection(1).valueType == VTGraphConnectionSlot.SlotValueType.Float)
						{
							//In float type, we can set a specific value for all regions based on mask
							setPixel = setInputFloat;
						}
						else
						{
							if (!setInputGrid.IsNullOrEmpty())
							{
								setPixel = setInputGrid.GetRangeValue(x, y);
							}
						}

						//Gather the mask value
						var maskPixel = 0f;
						if(GetInputConnection(2).valueType == VTGraphConnectionSlot.SlotValueType.Float)
						{
							//In float type, we can globally set the mask influence
							maskPixel = maskInputFloat;
						}
						else
						{
							if (!maskInputGrid.IsNullOrEmpty())
							{
								//We can pull from the mask grid
								maskPixel = maskInputGrid.GetRangeValue(x, y);
							}
							else
							{
								//We have no mask, so use the set pixel as the interpolation
								maskPixel = setPixel;
							}
						}

						newGrid.SetRangeValue(x, y, GetInterpolatedSetValue(basePixel, setPixel, maskPixel, setValueAboveZero));
					}
				}
			}

			output.SetRangeGridValue(newGrid);

			//Handle Float mask
			output.SetFloatValue(GetInterpolatedSetValue(baseInputFloat, setInputFloat, maskInputFloat, setValueAboveZero));
		}

		float GetInterpolatedSetValue(float baseValue, float setValue, float maskInterpolation, bool setValueAboveZero)
		{
			if(setValueAboveZero)
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
			RenderBoolProperty("Set Value Above Zero", ref setValueAboveZero,
				"When enabled, a special mode occurs where the set value for a pixel is used only when it is > 0 and mask is ignored.");
		}
#endif
	}
}