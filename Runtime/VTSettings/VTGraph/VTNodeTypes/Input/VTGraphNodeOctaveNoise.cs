using Codice.Client.BaseCommands.Changelist;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeOctaveNoise : VTGraphNode
	{
		public override string NodeTitle => "Octave Noise";
		public override bool HasNodeProperties => true;

		public enum OctaveNoiseType
		{
			Perlin = 0,
			Voronoi = 1,
		}

		[SerializeField]
		public int octaveCount = 4;
		[SerializeField]
		public float lacunarity = 0.8f;
		[SerializeField]
		public float persistence = 0.7f;
		[SerializeField]
		public float octaveOffsetMultiplier = 100.0f;

		[SerializeField]
		public float noiseScale = 10.0f;
		[SerializeField]
		public float noiseStrength = 1.0f;
		[SerializeField]
		public float noiseOffsetX = 0.0f;
		[SerializeField]
		public float noiseOffsetY = 0.0f;

		public VTGraphNodeOctaveNoise()
		{
			SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.Float);

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();

			//Create result grid
			int resolution = settings.textureGenResolutionNumber;
			var newGrid = new VTRangeGrid(resolution, resolution);

			//For each point, generate layered noise and sum them together
			if(octaveCount > 0)
			{
				if(lacunarity <= 0.0f)
				{
					lacunarity = 0.001f;
				}
				float maxGridValue = 1.0f * octaveCount;

				for (int y = 0; y < newGrid.Height; y++)
				{
					for (int x = 0; x < newGrid.Width; x++)
					{
						float currentAmplitude = 1.0f;
						float currentFrequency = 1.0f;
						float sumValue = 0.0f;

						for (int octaveIndex = 0; octaveIndex < octaveCount; octaveIndex++)
						{
							float thisOctaveOffset = octaveIndex * octaveOffsetMultiplier;

							float sampleX = (float)x / newGrid.Width * noiseScale * currentFrequency + thisOctaveOffset;
							float sampleY = (float)y / newGrid.Height * noiseScale * currentFrequency + thisOctaveOffset;

							float perlinValue = VTNoiseGenUtils.GetPerlinNoiseValue(sampleX, sampleY, noiseStrength);
							sumValue += perlinValue * currentAmplitude;

							currentAmplitude *= persistence;
							currentFrequency /= lacunarity;
						}

						newGrid.SetRangeValue(x, y, sumValue / maxGridValue);
					}
				}
			}
			

			output.SetRangeGridValue(newGrid);

		}

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderIntPropertyWithClamp("Octave Count", ref octaveCount, 1, 10, 
				"The number of noisemaps that will be generated and layered.");
			RenderFloatPropertyWithClamp("Lacunarity", ref lacunarity, 0.001f, 2.0f,
				"The amount that each octave will be scaled in size. This value is mulitplied to adjust noise frequency.");
			RenderFloatPropertyWithClamp("Persistence", ref persistence, 0.0f, 2.0f,
				"The amount that each octave will influence the result. This value is multiplied to adjust noise amplitude.");

			EditorGUILayout.Space(5f);

			GUILayout.Label("Base Noise Properties", EditorStyles.boldLabel);

			RenderFloatProperty("Noise Scale", ref noiseScale);
			RenderFloatProperty("Noise Strength", ref noiseStrength);
			RenderFloatProperty("Noise Offset X", ref noiseOffsetX);
			RenderFloatProperty("Noise Offset Y", ref noiseOffsetY);
		}
	}
}