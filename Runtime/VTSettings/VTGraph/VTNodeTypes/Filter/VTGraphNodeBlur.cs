using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeBlur : VTGraphNode
	{
		public override string NodeTitle => "Box Blur";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float blurRadius = 0.0f;

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
				int blurCheckOffsetX = Mathf.RoundToInt(blurRadius * baseInputGrid.Width * 0.01f);
				int blurCheckOffsetY = Mathf.RoundToInt(blurRadius * baseInputGrid.Height * 0.01f);

				for (int y = 0; y < baseInputGrid.Height; y++)
				{
					for (int x = 0; x < baseInputGrid.Width; x++)
					{
						//Blur this row
						int horizontalCount = 0;
						int startCheck = Mathf.Max(x - blurCheckOffsetX, 0);
						int endCheck = Mathf.Min(baseInputGrid.Width - 1, x + blurCheckOffsetX);
						float averageValue = baseInputGrid.GetRangeValue(x, y);
						for(int checkIndex = startCheck; checkIndex < endCheck; checkIndex++)
						{
							averageValue += baseInputGrid.GetRangeValue(checkIndex, y);
							horizontalCount++;
						}
						if(horizontalCount > 0)
						{
							averageValue /= horizontalCount;
						}

						newGrid.SetRangeValue(x, y, averageValue);
					}
				}
				for (int x = 0; x < baseInputGrid.Width; x++)
				{
					for (int y = 0; y < baseInputGrid.Height; y++)
					{
						//Blur this column
						int verticalCount = 0;
						int startCheck = Mathf.Max(y - blurCheckOffsetY, 0);
						int endCheck = Mathf.Min(baseInputGrid.Height - 1, y + blurCheckOffsetY);
						float averageValue = baseInputGrid.GetRangeValue(x, y);
						for(int checkIndex = startCheck; checkIndex < endCheck; checkIndex++)
						{
							averageValue += baseInputGrid.GetRangeValue(x, checkIndex);
							verticalCount++;
						}
						if(verticalCount > 0)
						{
							averageValue /= verticalCount;
						}

						newGrid.SetRangeValue(x, y, averageValue);
					}
				}
			}

			output.SetRangeGridValue(newGrid);

			//Handle Float
			var baseInputFloat = GetInputConnection(0).GetFloatValue();

			output.SetFloatValue(baseInputFloat);
		}

		//RENDER PROPERTIES

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			float blurRadiusFloat = Mathf.Clamp((float)EditorGUILayout.FloatField("Blur Radius", blurRadius), 0.0f, 50f);
			if (blurRadiusFloat != blurRadius)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				blurRadius = blurRadiusFloat;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
	}
}
