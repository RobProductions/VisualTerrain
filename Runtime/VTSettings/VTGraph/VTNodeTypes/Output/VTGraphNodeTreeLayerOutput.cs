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

		//RENDERING

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();


			var treeObjectValue = (GameObject)EditorGUILayout.ObjectField("Tree Prototype", treePrototype, typeof(GameObject), true);
			if (treeObjectValue != treePrototype)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				treePrototype = treeObjectValue;
				endEditNodePropertyEvent?.Invoke(this);
			}

			var bendFactorValue = EditorGUILayout.FloatField("Bend Factor", treeBendFactor);
			if (bendFactorValue != treeBendFactor)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				treeBendFactor = bendFactorValue;
				endEditNodePropertyEvent?.Invoke(this);
			}

			var navMeshLODIndexValue = EditorGUILayout.IntField("Navmesh LOD Index", navMeshLODIndex);
			if (navMeshLODIndexValue != navMeshLODIndex)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				navMeshLODIndex = navMeshLODIndexValue;
				endEditNodePropertyEvent?.Invoke(this);
			}


		}
	}
}