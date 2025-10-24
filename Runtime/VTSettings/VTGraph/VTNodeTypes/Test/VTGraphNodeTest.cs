using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeTest : VTGraphNode
	{
		public override string NodeTitle => "Test Node";

		public VTGraphNodeTest()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			inputConnections[0].connectionSlotName = "Input Slot";
			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
			outputConnections[0].connectionSlotName = "Output Slot";
		}


		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);
			
			var output = GetOutputConnection();
			if (output != null)
			{
				if(settings.textureOutputResolution == VTGraphProcessingSettings.TextureOutputResolution.Full)
				{
					output.SetTextureValue(GetInputConnection().textureValue);
				}
				else if (settings.textureOutputResolution == VTGraphProcessingSettings.TextureOutputResolution.RestrictToSize)
				{
					output.SetTextureValue(GetInputConnection().textureValue);
					/*
					var inputTex = GetInputConnection().textureValue;
					if (inputTex != null)
					{
						Texture2D newTex = new Texture2D(inputTex.width, inputTex.height);
						var pixels = inputTex.GetPixels32();
						newTex.SetPixels32(pixels);
						
						//TODO: Use if UNITY_6000_ OR NEWER to reinitialize instead of resize
						//newTex.Resize(4096, 4096);

						newTex.Apply();

						output.SetTextureValue(newTex);
					}
					else
					{
						output.SetTextureValue(null);
					}
					*/
				}
			}
		}
	}
}
