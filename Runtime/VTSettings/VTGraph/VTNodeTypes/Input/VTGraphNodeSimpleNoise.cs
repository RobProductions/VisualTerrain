using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNodeSimpleNoise : VTGraphNode
	{
		public override string NodeTitle => "Simple Noise";
		public override bool HasNodeProperties => true;

		public enum SimpleNoiseType
		{
			Perlin = 0,
			Voronoi = 1,
		}

		public SimpleNoiseType noiseType = SimpleNoiseType.Perlin;

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

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			if (output != null)
			{
				int resolution = settings.textureGenResolutionNumber;

				VTRangeGrid noiseMap;
				if(noiseType == SimpleNoiseType.Perlin)
				{
					noiseMap = VTNoiseGenUtils.GeneratePerlinRangeGrid(resolution, resolution, noiseOffsetX, noiseOffsetY, noiseScale, noiseStrength);
				}
				else
				{
					noiseMap = VTNoiseGenUtils.GenerateVoronoiRangeGrid(resolution, resolution, noiseOffsetX, noiseOffsetY, noiseScale, noiseStrength);
				}

				output.SetRangeGridValue(noiseMap);
			}
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			SimpleNoiseType setNoiseType = (SimpleNoiseType)EditorGUILayout.EnumPopup("Noise Type", noiseType);
			if (setNoiseType != noiseType)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				noiseType = setNoiseType;
				endEditNodePropertyEvent?.Invoke(this);
			}

			RenderFloatProperty("Noise Scale", ref noiseScale);
			RenderFloatProperty("Noise Strength", ref noiseStrength);
			RenderFloatProperty("Noise Offset X", ref noiseOffsetX);
			RenderFloatProperty("Noise Offset Y", ref noiseOffsetY);
		}

#endif

	}
}


