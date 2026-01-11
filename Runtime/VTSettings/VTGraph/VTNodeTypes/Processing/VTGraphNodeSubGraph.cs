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

			//Ensure that we have the right number of input and output slots
			RefreshConnections();

			if(subGraphAsset != null)
			{
				//If we have a subgraph to use,
				//first set the value of all subgraph input nodes based on connections
				//to this node in the parent graph
				var subGraphInputNodes = subGraphAsset.GetSubGraphInputNodeList();
				for(int i = 0; i < inputConnections.Length; i++)
				{
					var thisConnection = inputConnections[i];
					var thisInputNode = subGraphInputNodes[i];

					thisInputNode.GetInputConnection().SetRangeGridValue(thisConnection.GetRangeGridValue());
					thisInputNode.GetInputConnection().SetFloatValue(thisConnection.GetFloatValue());
				}

				//Then set the value of all output connections
				//based on the contained subgraph
				for(int i = 0; i < outputConnections.Length; i++)
				{
					var thisConnection = outputConnections[i];
					var connectionValue = VTGraphValueInterface.GetSubGraphOutputSlotValue(subGraphAsset, i, settings);
					if(connectionValue.HasValue)
					{
						thisConnection.SetRangeGridValue(connectionValue.Value.rangeGridValue);
						thisConnection.SetFloatValue(connectionValue.Value.floatValue);
					}
				}
			}
		}

		public void RefreshConnections()
		{
			//Check for differing connection count
			if(subGraphAsset != null)
			{
				if(subGraphAsset.GetSubGraphInputCount() == inputConnections.Length && subGraphAsset.GetSubGraphOutputCount() == outputConnections.Length)
				{
					//No need to clear connections,
					//However we should check to make sure the names are consistent
					var subGraphInputNodes = subGraphAsset.GetSubGraphInputNodeList();
					var subGraphOutputNodes = subGraphAsset.GetSubGraphOutputNodeList();

					SetInputConnectionNames(subGraphInputNodes);
					SetOutputConnectionNames(subGraphOutputNodes);

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
				var subGraphInputNodes = subGraphAsset.GetSubGraphInputNodeList();
				var subGraphOutputNodes = subGraphAsset.GetSubGraphOutputNodeList();

				SetupEmptyInputConnections(subGraphInputNodes.Count, VTGraphConnectionSlot.SlotValueType.RangeGrid);
				SetInputConnectionNames(subGraphInputNodes);

				SetupEmptyOutputConnections(subGraphOutputNodes.Count, VTGraphConnectionSlot.SlotValueType.RangeGrid);
				SetOutputConnectionNames(subGraphOutputNodes);
			}
		}

		void SetInputConnectionNames(List<VTGraphNodeSGInValue> subGraphInputNodes)
		{
			for (int i = 0; i < subGraphInputNodes.Count; i++)
			{
				if (subGraphInputNodes[i].CustomName != "")
				{
					inputConnections[i].connectionSlotName = subGraphInputNodes[i].CustomName;
				}
				else
				{
					inputConnections[i].connectionSlotName = "Input " + i.ToString();
				}
			}
		}

		void SetOutputConnectionNames(List<VTGraphNodeSGOutValue> subGraphOutputNodes)
		{
			for (int i = 0; i < subGraphOutputNodes.Count; i++)
			{
				if (subGraphOutputNodes[i].CustomName != "")
				{
					outputConnections[i].connectionSlotName = subGraphOutputNodes[i].CustomName;
				}
				else
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