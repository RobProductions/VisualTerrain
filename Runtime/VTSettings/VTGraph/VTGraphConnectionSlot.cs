using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphConnectionSlot
	{
		public enum NodeConnectionSlotType
		{
			Input = 0,
			Output = 1,
		}

		[SerializeField]
		public string connectionSlotName = "Connection";
		[field: SerializeField]
		public NodeConnectionSlotType connectionSlotType { get; private set; } = NodeConnectionSlotType.Input;
		[SerializeField, SerializeReference, HideInInspector]
		public VTGraphNode parentNode = null;
		[field: SerializeField]
		public int indexOnParentNode { get; private set; } = 0;
		[SerializeField]
		private int connectionCount = 0;
		[SerializeField]
		public bool slotHidden = false;

		public enum SlotValueType
		{
			RangeGrid = 0,
			Float = 1,
		}

		[field: SerializeField]
		public SlotValueType valueType { get; set; } = SlotValueType.RangeGrid;

		[NonSerialized]
		public VTRangeGrid defaultRangeGridValue = VTRangeGrid.Empty;
		[SerializeField]
		public float defaultFloatValue = 0.0f;

		[NonSerialized]
		public VTRangeGrid rangeGridValue = VTRangeGrid.Empty;
		[NonSerialized]
		public float floatValue = 0.0f;

		public VTGraphConnectionSlot(NodeConnectionSlotType slotType, VTGraphNode createdOnNode, SlotValueType slotValueType, int indexOnCreatedNode)
		{
			connectionSlotType = slotType;
			parentNode = createdOnNode;
			valueType = slotValueType;
			indexOnParentNode = indexOnCreatedNode;
			if(createdOnNode == null)
			{
				VTLog.LogWarning("VTGraphConnectionSlot created with null parentNode!");
			}
		}

		//GETTERS

		/// <summary>
		/// True when we are able to add a connection to this slot
		/// </summary>
		/// <returns></returns>
		public bool CanAddConnectedSlot()
		{
			if (connectionSlotType == NodeConnectionSlotType.Input)
			{
				return !IsConnected();
			}

			return true;
		}

		/// <summary>
		/// True when we have at least connection
		/// </summary>
		/// <returns></returns>
		public bool IsConnected()
		{
			return connectionCount > 0;
		}

		//SETTERS

		/// <summary>
		/// Called when connecting another input or output slot
		/// to this one
		/// </summary>
		public void AddConnectedSlot()
		{
			connectionCount++;
		}

		/// <summary>
		/// Called when removing a connected input or output slot
		/// </summary>
		public void RemoveConnectedSlot()
		{
			connectionCount--;
			if(connectionCount < 0)
			{
				VTLog.LogWarning("Connection count went < 0 in RemoveConnectedSlot!");
				connectionCount = 0;
			}
		}

		/// <summary>
		/// Reset connection count to 0
		/// </summary>
		public void ClearConnectedSlots()
		{
			connectionCount = 0;
		}

		//VALUE

		public void SetRangeGridValue(VTRangeGrid setGrid)
		{
			rangeGridValue = setGrid;
		}

		public VTRangeGrid GetRangeGridValue()
		{
			return rangeGridValue;
		}

		public void SetFloatValue(float setFloat)
		{
			floatValue = setFloat;
		}

		public float GetFloatValue()
		{
			return floatValue;
		}

	}
}