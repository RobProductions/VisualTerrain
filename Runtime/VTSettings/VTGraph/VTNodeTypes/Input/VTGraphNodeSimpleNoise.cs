using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSimpleNoise : VTGraphNode
	{
		public override string NodeTitle => "Simple Noise";
		public override bool HasNodeProperties => true;

		public enum SimpleNoiseType
		{
			Perlin = 0,
			Voronoi = 1,
		}

		[SerializeField]
		public float noiseScale = 10.0f;
		[SerializeField]
		public float noiseStrength = 1.0f;
		[SerializeField]
		public float noiseOffsetX = 0.0f;
		[SerializeField]
		public float noiseOffsetY = 0.0f;

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
				int resolution = settings.textureGenResolutionNumber;
				var noiseMap = GeneratePerlinNoiseMap(resolution, resolution, noiseOffsetX, noiseOffsetY);
				output.SetTextureValue(noiseMap);
			}
		}

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			float scaleFloat = (float)EditorGUILayout.FloatField("Noise Scale", noiseScale);
			if (scaleFloat != noiseScale)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				noiseScale = scaleFloat;
				endEditNodePropertyEvent?.Invoke(this);
			}

			float strengthFloat = (float)EditorGUILayout.FloatField("Noise Strength", noiseStrength);
			if (strengthFloat != noiseStrength)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				noiseStrength = strengthFloat;
				endEditNodePropertyEvent?.Invoke(this);
			}

			float offsetFloatX = (float)EditorGUILayout.FloatField("Noise Offset X", noiseOffsetX);
			if (offsetFloatX != noiseOffsetX)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				noiseOffsetX = offsetFloatX;
				endEditNodePropertyEvent?.Invoke(this);
			}

			float offsetFloatY = (float)EditorGUILayout.FloatField("Noise Offset Y", noiseOffsetY);
			if (offsetFloatY != noiseOffsetY)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				noiseOffsetY = offsetFloatY;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}

		//NOISE GENERATION

		Texture2D GeneratePerlinNoiseMap(int width, int height, float xOffset, float yOffset)
		{
			var ret = new Texture2D(width, height);
			for(int i = 0; i < width; i++)
			{
				for(int j = 0; j < height; j++)
				{
					float xIndex = (float)i / width * noiseScale + xOffset;
					float yIndex = (float)j / height * noiseScale + yOffset;

					var sampleNoiseValue = Mathf.PerlinNoise(xIndex, yIndex) * noiseStrength;
					var newCol = new Color(sampleNoiseValue, sampleNoiseValue, sampleNoiseValue, 1.0f);
					ret.SetPixel(i, j, newCol);
				}
			}
			ret.Apply();
			return ret;
		}
	}
}


