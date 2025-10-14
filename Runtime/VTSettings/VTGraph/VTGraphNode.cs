using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNode
	{
		[field: SerializeField]
		public virtual string NodeTitle { get; }

		[SerializeField, SerializeReference]
		public VTGraphConnectionSlot[] inputConnections;
		[SerializeField, SerializeReference]
		public VTGraphConnectionSlot[] outputConnections;

		[field: SerializeField]
		public Vector2 NodePosition { get; set; } = Vector2.zero;
		[field: SerializeField]
		public bool IsExpanded { get; set; } = true;

		//SETUP

		public void SetupEmptyInputConnections(int inputCount, VTGraphConnectionSlot.SlotValueType valueType)
		{
			inputConnections = new VTGraphConnectionSlot[inputCount];
			for(int i = 0; i < inputConnections.Length; i++)
			{
				inputConnections[i] = new VTGraphConnectionSlot(VTGraphConnectionSlot.NodeConnectionSlotType.Input, this, valueType, i);
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

		public virtual void ProcessNode()
		{
			//TODO: This
		}

		public void ProcessInputSlots()
		{

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