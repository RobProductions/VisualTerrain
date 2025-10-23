using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSimpleNoise : VTGraphNode
	{
		public override string NodeTitle
		{
			get
			{
				return "Simple Noise";
			}
		}

		[SerializeField]
		public float noiseScale = 10.0f;

		public VTGraphNodeSimpleNoise()
		{
			SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.Float);

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.Texture);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			if (output != null)
			{
				var noiseMap = GeneratePerlinNoiseMap(512, 512, 0, 0);
				output.SetTextureValue(noiseMap);
			}
		}

		public override bool RenderNodeProperties()
		{
			base.RenderNodeProperties();

			bool changed = false;

			return changed;
		}

		//NOISE GENERATION

		Texture2D GeneratePerlinNoiseMap(int width, int height, int xOffset, int yOffset)
		{
			var ret = new Texture2D(width, height);
			for(int i = 0; i < width; i++)
			{
				for(int j = 0; j < height; j++)
				{
					float xIndex = (float)i / width * noiseScale + xOffset;
					float yIndex = (float)j / height * noiseScale + yOffset;

					var sampleNoiseValue = Mathf.PerlinNoise(xIndex, yIndex);
					var newCol = new Color(sampleNoiseValue, sampleNoiseValue, sampleNoiseValue, 1.0f);
					ret.SetPixel(i, j, newCol);
				}
			}
			ret.Apply();
			return ret;
		}
	}
}


