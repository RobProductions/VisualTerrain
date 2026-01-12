using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSplatLayerOutput : VTGraphNode
	{
		public override string NodeTitle => "Splat Layer Output";
		public override bool HasNodeProperties => true;
		public override bool HasDisableButton => true;

		//TODO: Create dropdown for terrain layer creation
		//So you can just input a texture and tiling settings
		[SerializeField]
		public int layerOrder = 0;
		[SerializeField]
		public TerrainLayer terrainLayer = null;

		public VTGraphNodeSplatLayerOutput()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Splatmap";

			SetupEmptyOutputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			outputConnections[0].slotHidden = true;
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			var output = GetOutputConnection();
			if (output != null)
			{
				output.SetRangeGridValue(GetInputConnection().rangeGridValue);
			}
		}

		public TerrainLayer GetNodeTerrainLayer()
		{
			return terrainLayer;
		}

		//PROPERTIES

#if UNITY_EDITOR

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			RenderIntProperty("Layer Order", ref layerOrder);

			var layerValue = (TerrainLayer)EditorGUILayout.ObjectField("Terrain Layer", terrainLayer, typeof(TerrainLayer), true);
			if (layerValue != terrainLayer)
			{
				InvokeBeginEditNodeProperty();
				terrainLayer = layerValue;
				InvokeEndEditNodeProperty();
			}
		}
#endif
	}
}