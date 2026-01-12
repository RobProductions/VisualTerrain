using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeTexture : VTGraphNode
	{
		public override string NodeTitle => "Texture";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public Texture2D inputTexture;

		public VTGraphNodeTexture()
		{
			SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.RangeGrid);

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();

			var newGrid = new VTRangeGrid(settings.textureGenResolutionNumber, settings.textureGenResolutionNumber);

			if(inputTexture != null && inputTexture.isReadable)
			{
				for(int y = 0; y < newGrid.Height; y++)
				{
					for(int x = 0; x < newGrid.Width; x++)
					{
						//Get the percent into the RangeGrid
						float percentX = (float)x / newGrid.Width;
						float percentY = (float)y / newGrid.Height;

						//Convert percent into sample pixel from input texture dimensions
						float samplePositionX = percentX * inputTexture.width;
						float samplePositionY = percentY * inputTexture.height;

						//Get the pixel at the sample position
						int pixelX = Mathf.Clamp(Mathf.FloorToInt(samplePositionX), 0, inputTexture.width - 1);
						int pixelY = Mathf.Clamp(Mathf.FloorToInt(samplePositionY), 0, inputTexture.height - 1);
						Color colorValue = inputTexture.GetPixel(pixelX, pixelY);
						newGrid.SetRangeValue(x, y, colorValue.r);
					}
				}
			}

			output.SetRangeGridValue(newGrid);
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			var content = new GUIContent("Input Texture", 
				"The texture that will be sampled to create a RangeGrid of the desired size. " +
				"Note that the texture must have the Read/Write property enabled to be read properly.");
			var newValue = (Texture2D)EditorGUILayout.ObjectField(content, inputTexture, typeof(Texture2D), false);
			if (newValue != inputTexture)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				inputTexture = newValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
#endif
	}
}