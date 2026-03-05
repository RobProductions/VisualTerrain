using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNodeBlur : VTGraphNode
	{
		public override string NodeTitle => "Box Blur";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float blurRadiusX = 0.0f;
		[SerializeField]
		public float blurRadiusY = 0.0f;

		public VTGraphNodeBlur()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Input Value";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			var baseInputGrid = GetInputConnection(0).GetRangeGridValue();

			//Handle RangeGrid blur
			var newGrid = new VTRangeGrid(baseInputGrid.Width, baseInputGrid.Height);

			if (!baseInputGrid.IsNullOrEmpty())
			{
				int blurCheckOffsetX = Mathf.RoundToInt(blurRadiusX * baseInputGrid.Width * 0.002f);
				int blurCheckOffsetY = Mathf.RoundToInt(blurRadiusY * baseInputGrid.Height * 0.002f);

				for (int y = 0; y < baseInputGrid.Height; y++)
				{
					//Blur this row with sliding window method
					float sumValue = 0;
					int windowSize = 0;

					//Compute the average for the first pixel
					for (int currentCheckX = 0; currentCheckX <= blurCheckOffsetX && currentCheckX < baseInputGrid.Width; currentCheckX++)
					{
						sumValue += baseInputGrid.GetRangeValue(currentCheckX, y);
						windowSize++;
					}

					for (int x = 0; x < baseInputGrid.Width; x++)
					{
						//Compute the average for the rest of the pixels
						//by adding right edge and subtracting left
						int rightIndex = x + blurCheckOffsetX + 1;
						if (rightIndex < baseInputGrid.Width)
						{
							sumValue += baseInputGrid.GetRangeValue(rightIndex, y);
							windowSize++;
						}

						//And remove left edge
						int leftIndex = x - blurCheckOffsetX;
						if (leftIndex >= 0)
						{
							sumValue -= baseInputGrid.GetRangeValue(leftIndex, y);
							windowSize--;
						}

						newGrid.SetRangeValue(x, y, sumValue / windowSize);
					}
				}
				for (int x = 0; x < baseInputGrid.Width; x++)
				{
					//Blur this column with sliding window method
					float sumValue = 0;
					int windowSize = 0;

					//Compute the average for the first pixel
					for (int currentCheckY = 0; currentCheckY <= blurCheckOffsetY && currentCheckY < baseInputGrid.Height; currentCheckY++)
					{
						sumValue += newGrid.GetRangeValue(x, currentCheckY);
						windowSize++;
					}

					for (int y = 0; y < baseInputGrid.Height; y++)
					{
						//Compute the average for the rest of the pixels
						//by adding bottom edge and subtracting top
						int bottomIndex = y + blurCheckOffsetY + 1;
						if (bottomIndex < baseInputGrid.Height)
						{
							sumValue += newGrid.GetRangeValue(x, bottomIndex);
							windowSize++;
						}

						//And remove top edge
						int topIndex = y - blurCheckOffsetY;
						if (topIndex >= 0)
						{
							sumValue -= newGrid.GetRangeValue(x, topIndex);
							windowSize--;
						}

						newGrid.SetRangeValue(x, y, sumValue / windowSize);
					}
				}
			}

			output.SetRangeGridValue(newGrid);

			//Handle Float
			var baseInputFloat = GetInputConnection(0).GetFloatValue();

			output.SetFloatValue(baseInputFloat);
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatPropertyWithClamp("Blur Radius X", ref blurRadiusX, 0.0f, 100f);
			RenderFloatPropertyWithClamp("Blur Radius Y", ref blurRadiusY, 0.0f, 100f);
		}
#endif
	}
}
