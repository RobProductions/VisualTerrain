using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeAngleMask : VTGraphNode
	{
		public override string NodeTitle => "Angle Mask";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float maskStrength = 1.0f;
		[SerializeField]
		public bool resolutionIndependent = true;

		public VTGraphNodeAngleMask()
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

			//Handle RangeGrid mask
			var newGrid = new VTRangeGrid(inputGrid.Width, inputGrid.Height);
			float resolutionScalerX = resolutionIndependent ? inputGrid.Width : 1.0f;
			float resolutionScalerY = resolutionIndependent ? inputGrid.Height : 1.0f;

			for (int x = 0; x < inputGrid.Width; x++)
			{
				for (int y = 0; y < inputGrid.Height; y++)
				{
					if (x > 0 && x < inputGrid.Width - 1 && y > 0 && y < inputGrid.Height - 1)
					{
						//Middle pixel case
						var left = inputGrid.GetRangeValue(x - 1, y);
						var right = inputGrid.GetRangeValue(x + 1, y);
						var top = inputGrid.GetRangeValue(x, y + 1);
						var bottom = inputGrid.GetRangeValue(x, y - 1);

						var horizontal = Mathf.Abs((left - right) / 2f) * resolutionScalerX;
						var vertical = Mathf.Abs((top - bottom) / 2f) * resolutionScalerY;
						var setValue = Mathf.Sqrt((horizontal * horizontal) + (vertical * vertical)) * 0.1f * maskStrength;
						newGrid.SetRangeValue(x, y, setValue);
					}
					else
					{
						//Jank edge case
						var thisValue = inputGrid.GetRangeValue(x, y);

						var verticalCount = 0;
						var horizontalCount = 0;
						var right = thisValue;
						var top = thisValue;
						var bottom = thisValue;
						var left = thisValue;

						if (x > 0)
						{
							left = inputGrid.GetRangeValue(x - 1, y);
							horizontalCount++;
						}
						if (x < inputGrid.Width - 1)
						{
							right = inputGrid.GetRangeValue(x + 1, y);
							horizontalCount++;
						}
						if (y > 0)
						{
							top = inputGrid.GetRangeValue(x, y - 1);
							verticalCount++;
						}
						if (y < inputGrid.Height - 1)
						{
							bottom = inputGrid.GetRangeValue(x, y + 1);
							verticalCount++;
						}

						if (horizontalCount == 0)
						{
							horizontalCount++;
						}
						if (verticalCount == 0)
						{
							verticalCount++;
						}

						var horizontal = Mathf.Abs((left - right) / horizontalCount) * resolutionScalerX;
						var vertical = Mathf.Abs((top - bottom) / verticalCount) * resolutionScalerY;

						var setValue = Mathf.Sqrt((horizontal * horizontal) + (vertical * vertical)) * 0.1f * maskStrength;
						newGrid.SetRangeValue(x, y, setValue);
					}
				}
			}

			output.SetRangeGridValue(newGrid);

			//Handle Float mask
			output.SetFloatValue(input.GetFloatValue());
		}

		//RENDER PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatPropertyWithClamp("Mask Strength", ref maskStrength, 0.0f, Mathf.Infinity);
		}

#endif

	}
}