using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphNodeSubGraph : VTGraphNode
	{
		public override string NodeTitle => "Sub Graph";
		public override bool HasNodeProperties => true;

		[SerializeField]
		public VTSubGraphAsset subGraphAsset = null;

		public VTGraphNodeSubGraph()
		{
			SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.RangeGrid);

			SetupEmptyOutputConnections(0, VTGraphConnectionSlot.SlotValueType.RangeGrid);
		}

		public override void ProcessNode(VTGraphProcessingSettings settings)
		{
			base.ProcessNode(settings);

			RefreshConnections();

			var output = GetOutputConnection();
			if (output != null)
			{

			}
		}

		void RefreshConnections()
		{
			//Check for differing connection count
			if(subGraphAsset != null)
			{
				if(subGraphAsset.GetSubGraphInputCount() == inputConnections.Length && subGraphAsset.GetSubGraphOutputCount() == outputConnections.Length)
				{
					//No need to clear connections and do work
					return;
				}
			}

			//Clear existing connections
			if(parentGraph != null)
			{
				var allNodeConnections = parentGraph.GetConnectionsWithNode(this);
				for (int i = allNodeConnections.Count - 1; i >= 0; i--)
				{
					VTGraphConnection thisConnection = allNodeConnections[i];
					parentGraph.RemoveNodeConnection(thisConnection);
				}
			}
			else
			{
				//Uh oh
			}

			//Create the new input connections based on lengths
			if (subGraphAsset == null)
			{
				SetupEmptyInputConnections(0, VTGraphConnectionSlot.SlotValueType.RangeGrid);
				SetupEmptyOutputConnections(0, VTGraphConnectionSlot.SlotValueType.RangeGrid);
			}
			else
			{
				int inputCount = subGraphAsset.GetSubGraphInputCount();
				int outputCount = subGraphAsset.GetSubGraphOutputCount();

				SetupEmptyInputConnections(inputCount, VTGraphConnectionSlot.SlotValueType.RangeGrid);
				for (int i = 0; i < inputCount; i++)
				{
					inputConnections[i].connectionSlotName = "Input " + i.ToString();
					//TODO: Input and output node names from node name
				}

				SetupEmptyOutputConnections(outputCount, VTGraphConnectionSlot.SlotValueType.RangeGrid);
				for (int i = 0; i < outputCount; i++)
				{
					outputConnections[i].connectionSlotName = "Output " + i.ToString();
				}
			}
		}

		//RENDERING

		public override void RenderNodeProperties()
		{
			base.RenderNodeProperties();

			GUILayout.Label("Sub Graph Asset");
			VTSubGraphAsset newSubGraphAsset = (VTSubGraphAsset)EditorGUILayout.ObjectField(subGraphAsset, typeof(VTSubGraphAsset), allowSceneObjects: false);
			if (newSubGraphAsset != subGraphAsset)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				subGraphAsset = newSubGraphAsset;
				RefreshConnections();
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
	}
}