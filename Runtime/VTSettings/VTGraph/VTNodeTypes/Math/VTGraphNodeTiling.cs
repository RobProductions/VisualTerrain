using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNodeTiling : VTGraphNode
	{
		public override string NodeTitle => "Tiling";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public float tilingX = 1f;
		[SerializeField]
		public float tilingY = 1f;

		public VTGraphNodeTiling()
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

			if (!inputGrid.IsNullOrEmpty())
			{
				float tileAmountX = 1f;
				float tileAmountY = 1f;
				if(tilingX > 0f)
				{
					tileAmountX = tilingX;
				}
				if(tilingY > 0f)
				{
					tileAmountY = tilingY;
				}

				for (int x = 0; x < inputGrid.Width; x++)
				{
					for (int y = 0; y < inputGrid.Height; y++)
					{
						//Sample a pixel scaled by the tiling amount
						int sampleX = Mathf.Clamp(Mathf.RoundToInt((x * tileAmountX) % inputGrid.Width), 0, inputGrid.Width - 1);
						int sampleY = Mathf.Clamp(Mathf.RoundToInt((y * tileAmountY) % inputGrid.Height), 0, inputGrid.Height - 1);

						float setValue = inputGrid.GetRangeValue(sampleX, sampleY);

						newGrid.SetRangeValue(x, y, setValue);
					}
				}
			}

			output.SetRangeGridValue(newGrid);

			//Handle Float
			output.SetFloatValue(input.GetFloatValue());
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatPropertyWithClamp("Tiling X", ref tilingX, 0f, Mathf.Infinity);
			RenderFloatPropertyWithClamp("Tiling Y", ref tilingY, 0f, Mathf.Infinity);
		}
#endif

	}
}