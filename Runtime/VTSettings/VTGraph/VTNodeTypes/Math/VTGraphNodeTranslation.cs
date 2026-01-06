using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeTranslation : VTGraphNode
	{
		public override string NodeTitle => "Translation";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float offsetX = 0;
		[SerializeField]
		public float offsetY = 0;

		[SerializeField]
		public float clipValue = 0.0f;

		public VTGraphNodeTranslation()
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

			//Handle RangeGrid
			var newGrid = new VTRangeGrid(inputGrid.Width, inputGrid.Height);

			if(!inputGrid.IsNullOrEmpty())
			{
				for (int x = 0; x < inputGrid.Width; x++)
				{
					for (int y = 0; y < inputGrid.Height; y++)
					{
						//Sample value but at offset position and also account for resolution changes
						float offsetFloatX = offsetX * inputGrid.Width * 0.01f;
						float offsetFloatY = offsetY * inputGrid.Height * 0.01f;

						int sampleX = x - Mathf.RoundToInt(offsetFloatX);
						int sampleY = y - Mathf.RoundToInt(offsetFloatY);

						float setValue = clipValue;
						if(sampleX > 0 && sampleX < inputGrid.Width && sampleY > 0 && sampleY < inputGrid.Height)
						{
							//Only sample if it's within range of input
							setValue = inputGrid.GetRangeValue(sampleX, sampleY);
						}

						newGrid.SetRangeValue(x, y, setValue);
					}
				}
			}

			output.SetRangeGridValue(newGrid);

			//Handle Float
			output.SetFloatValue(input.GetFloatValue());
		}

		//RENDER PROPERTIES

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatProperty("Offset X", ref offsetX);
			RenderFloatProperty("Offset Y", ref offsetY);
			RenderFloatProperty("Clip Value", ref clipValue);
		}
	}
}