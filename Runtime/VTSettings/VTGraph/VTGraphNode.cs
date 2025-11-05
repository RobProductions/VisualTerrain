using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNode
	{
		public virtual string NodeTitle { get => "Unnamed Node"; }
		public virtual bool HasNodeProperties { get => false; }
		public virtual bool HasDisableButton { get => false; }

		[SerializeField, SerializeReference]
		public VTGraphConnectionSlot[] inputConnections;
		[SerializeField, SerializeReference]
		public VTGraphConnectionSlot[] outputConnections;

		[field: SerializeField]
		public string CustomName { get; set; } = "";
		[field: SerializeField]
		public Vector2 NodePosition { get; set; } = Vector2.zero;
		[field: SerializeField]
		public bool IsExpanded { get; set; } = true;
		[field: SerializeField]
		public bool IsDisabled { get; set; } = false;

		protected VTGraph parentGraph = null;

		public delegate void BeginEditNodeProperty(string description);
		public BeginEditNodeProperty beginEditNodePropertyEvent;
		public delegate void EndEditNodeProperty(VTGraphNode editedOnNode);
		public EndEditNodeProperty endEditNodePropertyEvent;

		//SETUP

		public void SetParentGraph(VTGraph v)
		{
			parentGraph = v;
		}

		public void SetupEmptyInputConnections(int inputCount, VTGraphConnectionSlot.SlotValueType defaultValueType)
		{
			inputConnections = new VTGraphConnectionSlot[inputCount];
			for(int i = 0; i < inputConnections.Length; i++)
			{
				inputConnections[i] = new VTGraphConnectionSlot(VTGraphConnectionSlot.NodeConnectionSlotType.Input, this, defaultValueType, i);
			}
		}

		public void SetupEmptyOutputConnections(int outputCount, VTGraphConnectionSlot.SlotValueType valueType)
		{
			outputConnections = new VTGraphConnectionSlot[outputCount];
			for (int i = 0; i < outputConnections.Length; i++)
			{
				outputConnections[i] = new VTGraphConnectionSlot(VTGraphConnectionSlot.NodeConnectionSlotType.Output, this, valueType, i);
			}
		}

		//PROCESSING

		/// <summary>
		/// Naively processes the node output value
		/// based on the input. The input values
		/// will not search for connections or process
		/// other nodes. That is left to VTGraph.ProcessNode().
		/// </summary>
		public virtual void ProcessNode(VTGraphProcessingSettings settings)
		{
			return;
		}

		public virtual void RenderNodeProperties()
		{
			return;
		}

		//GETTERS

		/// <summary>
		/// Returns the first output connection.
		/// </summary>
		/// <returns></returns>
		public VTGraphConnectionSlot GetOutputConnection()
		{
			return GetOutputConnection(0);
		}

		/// <summary>
		/// Returns the output connection at a specific index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public VTGraphConnectionSlot GetOutputConnection(int index)
		{
			if(outputConnections.Length > index)
			{
				return outputConnections[index];
			}

			return null;
		}

		/// <summary>
		/// Returns the first input connection.
		/// </summary>
		/// <returns></returns>
		public VTGraphConnectionSlot GetInputConnection()
		{
			return GetInputConnection(0);
		}

		/// <summary>
		/// Returns the input connection at a specific index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public VTGraphConnectionSlot GetInputConnection(int index)
		{
			if (inputConnections.Length > index)
			{
				return inputConnections[index];
			}

			return null;
		}

	}
}