using System.Collections;
using System.Collections.Generic;
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
		public readonly NodeConnectionSlotType connectionSlotType = NodeConnectionSlotType.Input;
		[SerializeField, SerializeReference]
		public readonly VTGraphNode parentNode = null;
		[SerializeField]
		public readonly int indexOnParentNode = 0;
		[SerializeField]
		private int connectionCount = 0;

		public VTGraphConnectionSlot(NodeConnectionSlotType slotType, VTGraphNode createdOnNode, int indexOnCreatedNode)
		{
			connectionSlotType = slotType;
			parentNode = createdOnNode;
			indexOnParentNode = indexOnCreatedNode;
			if(createdOnNode == null)
			{
				VTLog.LogWarning("VTGraphConnectionSlot created with null parentNode!");
			}
		}

		//GETTERS

		/// <summary>
		/// True when we are able to add a connection to this slot.
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

		public void ClearConnectedSlots()
		{
			connectionCount = 0;
		}

	}
}