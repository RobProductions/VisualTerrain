using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeTreeLayerOutput : VTGraphNode
	{
		public override string NodeTitle => "Tree Layer Output";
		public override bool HasNodeProperties => true;
		public override bool HasDisableButton => true;

		[SerializeField]
		public GameObject treePrototype = null;
		[SerializeField]
		public float treeBendFactor = 1.0f;
		[SerializeField]
		public int navMeshLODIndex = 0;

		[SerializeField]
		public float placementDensity = 0.1f;
		[SerializeField]
		public float placementJitterRange = 5f;
		[SerializeField]
		public bool placementRevalidateValue = true;

		[SerializeField]
		public Vector2 instanceWidthScaleRange = new Vector2(1.0f, 1.0f);
		[SerializeField]
		public Vector2 instanceHeightScaleRange = new Vector2(1.0f, 1.0f);

		public VTGraphNodeTreeLayerOutput()
		{
			SetupEmptyInputConnections(1, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			inputConnections[0].connectionSlotName = "Tree Map";

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

		public GameObject GetNodeTreePrototype()
		{
			return treePrototype;
		}

		public float GetNodeTreeBendFactor()
		{
			return treeBendFactor;
		}

		public int GetNodeNavMeshLODIndex()
		{
			return navMeshLODIndex;
		}

		public float GetNodePlacementDensity()
		{
			return placementDensity;
		}

		public float GetNodePlacementJitterRange()
		{
			return placementJitterRange;
		}

		public bool GetNodePlacementRevalidateValue()
		{
			return placementRevalidateValue;
		}

		public Vector2 GetNodeInstanceWidthScaleRange()
		{
			return instanceWidthScaleRange;
		}

		public Vector2 GetNodeInstanceHeightScaleRange()
		{
			return instanceHeightScaleRange;
		}

		//RENDERING

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			EditorGUILayout.LabelField("Prototype Settings", EditorStyles.boldLabel);

			RenderGameObjectProperty("Tree Prototype", ref treePrototype, true);
			RenderFloatProperty("Bend Factor", ref treeBendFactor);
			RenderIntProperty("Navmesh LOD Index", ref navMeshLODIndex,
				"The index of LOD value used when generating a navmesh. This allows you to use low-res versions of trees for nav calcuations.");

			EditorGUILayout.Space(5f);

			EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);

			RenderFloatProperty("Density", ref placementDensity,
				"For each world unit, this many trees will be attempted to be placed in a grid pattern and later jittered/randomized.");
			RenderFloatProperty("Jitter Range", ref placementJitterRange,
				"Each tree grid position will be jittered to some position within this range as a radius. The range is in world units.");
			RenderBoolProperty("Revalidate Position Value", ref placementRevalidateValue);

			EditorGUILayout.LabelField("Instance Settings", EditorStyles.boldLabel);

			RenderVector2Property("Width Scale Range", ref instanceWidthScaleRange,
				"When an instance is placed, its width will be random between the range of this X and Y value.");
			RenderVector2Property("Height Scale Range", ref instanceHeightScaleRange,
				"When an instance is placed, its height will be random between the range of this X and Y value.");
		}
	}
}