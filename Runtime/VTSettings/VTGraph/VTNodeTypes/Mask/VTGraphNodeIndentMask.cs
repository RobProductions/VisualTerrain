using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeIndentMask : VTGraphNode
	{
		public override string NodeTitle => "Indent Mask";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float checkDistance = 0.5f;
		[SerializeField]
		public bool checkBump = false;
		[SerializeField]
		public float maskStrength = 1.0f;

		public VTGraphNodeIndentMask()
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

			float bumpMultiplier = 1.0f;
			if(checkBump)
			{
				bumpMultiplier = -1.0f;
			}
			float distanceValue = checkDistance * 0.01f;
			int distanceInPixelsX = Mathf.RoundToInt((float)inputGrid.Width * distanceValue);
			int distanceInPixelsY = Mathf.RoundToInt((float)inputGrid.Height * distanceValue);

			var resultGrid = new VTRangeGrid(inputGrid.Width, inputGrid.Height);
			for (int y = 0; y < inputGrid.Height; y++)
			{
				for (int x = 0; x < inputGrid.Width; x++)
				{
					int checkPixelLeft = x - distanceInPixelsX;
					int checkPixelRight = x + distanceInPixelsX;
					int checkPixelBottom = y - distanceInPixelsY;
					int checkPixelTop = y + distanceInPixelsY;

					int valueCount = 0;

					float leftValue = 0.0f;
					if(checkPixelLeft > 0)
					{
						leftValue = inputGrid.GetRangeValue(checkPixelLeft, y);
						valueCount++;
					}
					float rightValue = 0.0f;
					if(checkPixelRight < inputGrid.Width)
					{
						rightValue = inputGrid.GetRangeValue(checkPixelRight, y);
						valueCount++;
					}
					float topValue = 0.0f;
					if(checkPixelTop < inputGrid.Height)
					{
						topValue = inputGrid.GetRangeValue(x, checkPixelTop);
						valueCount++;
					}
					float bottomValue = 0.0f;
					if(checkPixelBottom > 0)
					{
						bottomValue = inputGrid.GetRangeValue(x, checkPixelBottom);
						valueCount++;
					}

					var indentValue = 0.0f;
					if(valueCount > 0)
					{
						float averageValue = (leftValue + rightValue + topValue + bottomValue) / (float)valueCount;

						var currentValue = inputGrid.GetRangeValue(x, y);
						indentValue = bumpMultiplier * (currentValue - averageValue);
					}

					resultGrid.SetRangeValue(x, y, indentValue * maskStrength * 10f);
				}
			}
			output.SetRangeGridValue(resultGrid);

			//Handle float case
			output.SetFloatValue(input.GetFloatValue());
		}

		//RENDER PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatPropertyWithClamp("Check Distance", ref checkDistance, 0.0f, 50f,
				"The percentage distance that will be used to check indent edges.");

			RenderBoolProperty("Check Bump", ref checkBump,
				"When true, checks bumps instead of indents.");

			RenderFloatProperty("Mask Strength", ref maskStrength,
				"A multiplier that will be used on the final mask output.");
		}

#endif
	}
}