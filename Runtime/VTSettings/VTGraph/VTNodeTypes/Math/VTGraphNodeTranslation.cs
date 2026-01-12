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
		public float offsetX = 0f;
		[SerializeField]
		public float offsetY = 0f;
		[SerializeField]
		public float rotationDegrees = 0f;
		[SerializeField]
		public float scaleX = 1f;
		[SerializeField]
		public float scaleY = 1f;

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
				float pivotX = inputGrid.Width * 0.5f;
				float pivotY = inputGrid.Height * 0.5f;

				float offsetFloatX = offsetX * inputGrid.Width * 0.01f;
				float offsetFloatY = offsetY * inputGrid.Height * 0.01f;

				float rotationRadians = -rotationDegrees * Mathf.Deg2Rad;
				float rotationCos = Mathf.Cos(-rotationRadians);
				float rotationSin = Mathf.Sin(-rotationRadians);

				float inverseScaleX = 1.0f / scaleX;
				float inverseScaleY = 1.0f / scaleY;

				for (int x = 0; x < inputGrid.Width; x++)
				{
					for (int y = 0; y < inputGrid.Height; y++)
					{
						//Move current pixel to -50% to 50% so it's centered around 0
						float centeredXPixel = x - pivotX;
						float centeredYPixel = y - pivotY;

						//Translate pixel position
						float offsetXPixel = centeredXPixel - offsetFloatX;
						float offsetYPixel = centeredYPixel - offsetFloatY;

						//Rescale pixel
						float scaledXPixel = offsetXPixel * inverseScaleX;
						float scaledYPixel = offsetYPixel * inverseScaleY;

						//Rotate pixel
						float rotatedXPixel = scaledXPixel * rotationCos - scaledYPixel * rotationSin;
						float rotatedYPixel = scaledXPixel * rotationSin + scaledYPixel * rotationCos;

						//Sample a pixel using the translation
						int sampleX = Mathf.RoundToInt(rotatedXPixel + pivotX);
						int sampleY = Mathf.RoundToInt(rotatedYPixel + pivotY);

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

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderFloatProperty("Offset X", ref offsetX);
			RenderFloatProperty("Offset Y", ref offsetY);
			RenderFloatProperty("Rotation Degrees", ref rotationDegrees);
			RenderFloatProperty("Scale X", ref scaleX);
			RenderFloatProperty("Scale Y", ref scaleY);

			RenderPropertyHeading("Clip Settings");

			RenderFloatProperty("Clip Value", ref clipValue,
				"This is the value that will be used for pixels if the translation produces empty space in the output grid.");
		}
#endif
	}
}