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
		[SerializeField]
		public readonly VTGraphNode parentNode = null;

		[SerializeField, HideInInspector, SerializeReference]
		private List<VTGraphConnectionSlot> connectedSlots = new List<VTGraphConnectionSlot>();

		public VTGraphConnectionSlot(NodeConnectionSlotType slotType, VTGraphNode createdOnNode)
		{
			connectionSlotType = slotType;
			parentNode = createdOnNode;
			if(createdOnNode == null)
			{
				VTLog.LogWarning("VTGraphConnectionSlot created with null parentNode!");
			}
		}

		//CONNECTIONS

		public void ClearConnectedSlots()
		{
			connectedSlots.Clear();
		}

		/// <summary>
		/// Add a reference to another ConnectionSlot, either input or output.
		/// Only the VTGraph should use this because it manages both node connections
		/// and keeps a reference to the connection link.
		/// </summary>
		/// <param name="otherSlot"></param>
		public void AddConnectedSlot(VTGraphConnectionSlot otherSlot)
		{
			if(otherSlot == null)
			{
				return;
			}
			if(otherSlot.connectionSlotType == connectionSlotType)
			{
				VTLog.LogError("In AddConnectedSlot, otherSlot had same connection type as this one!");
				return;
			}
			if(otherSlot == this)
			{
				VTLog.LogError("In AddConnectedSlot, otherSlot was the same as this one!");
				return;
			}
			if(connectionSlotType == NodeConnectionSlotType.Input && !IsConnectedSlotsEmpty())
			{
				VTLog.LogError("In AddConnectedSlot, tried to add more than 1 input to this slot!");
				return;
			}

			connectedSlots.Add(otherSlot);
		}

		/// <summary>
		/// Remove a ConnectionSlot from the connection list.
		/// Only VTGraph should use this to manage connection relations.
		/// </summary>
		/// <param name="otherSlot"></param>
		public void RemoveConnectedSlot(VTGraphConnectionSlot otherSlot)
		{
			if(connectedSlots.Contains(otherSlot))
			{
				RemoveConnectedSlotAtIndex(connectedSlots.IndexOf(otherSlot));
			}
		}

		/// <summary>
		/// Remove a ConnectionSlot at a specific index from the connection list.
		/// Only VTGraph should use this to manage connection relations.
		/// </summary>
		/// <param name="index"></param>
		public void RemoveConnectedSlotAtIndex(int index)
		{
			connectedSlots.RemoveAt(index);
		}

		/// <summary>
		/// True when we are able to add a connection to this slot.
		/// </summary>
		/// <returns></returns>
		public bool CanAddConnectedSlot()
		{
			if(connectionSlotType == NodeConnectionSlotType.Input)
			{
				return IsConnectedSlotsEmpty();
			}

			return true;
		}

		/// <summary>
		/// True when there are no other connections to this slot.
		/// </summary>
		/// <returns></returns>
		public bool IsConnectedSlotsEmpty()
		{
			return (connectedSlots.Count <= 0);
		}
	}
}