using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraph
	{
		[System.Serializable]
		public class VTGraphDisplayData
		{
			public Vector2 ViewOffset { get; set; } = Vector2.zero;
		}

		[SerializeField]
		public VTGraphDisplayData displayData = new VTGraphDisplayData();

		[System.Serializable]
		public class VTGraphConnectionReference
		{
			public readonly VTGraphConnectionSlot slot1;
			public readonly VTGraphConnectionSlot slot2;

			public VTGraphConnectionReference(VTGraphConnectionSlot slot1, VTGraphConnectionSlot slot2)
			{
				this.slot1 = slot1;
				this.slot2 = slot2;
			}
		}

		[SerializeField, SerializeReference]
		public List<VTGraphNode> nodeList = new List<VTGraphNode>();
		[SerializeField, SerializeReference]
		public List<VTGraphConnectionReference> connectionsList = new List<VTGraphConnectionReference>();

		//NODES

		public void CreateNode<T>() where T : VTGraphNode, new()
		{
			CreateNode<T>(Vector2.zero);
		}

		public void CreateNode<T>(Vector2 startingPosition) where T : VTGraphNode, new()
		{
			T newNode = new T();
			AddNode(newNode);

			SetNodePosition(newNode, startingPosition);
		}

		public void AddNode(VTGraphNode node)
		{
			nodeList.Add(node);
		}

		public void RemoveNode(VTGraphNode node)
		{
			if(!nodeList.Contains(node))
			{
				return;
			}

			//Remove all of this node's connections before deleting
			var thisNodeConnections = GetConnectionsWithNode(node);
			for(int i = thisNodeConnections.Count - 1; i >= 0; i--)
			{
				RemoveNodeConnection(thisNodeConnections[i]);
			}

			nodeList.Remove(node);
		}

		public void SetNodePosition(VTGraphNode node, Vector2 pos)
		{
			node.NodePosition = pos;
		}

		public void SetNodeIndex(VTGraphNode node, int newIndex)
		{
			if(newIndex >= 0 && newIndex < nodeList.Count)
			{
				if (nodeList.Contains(node))
				{
					var thisIndex = nodeList.IndexOf(node);
					if(thisIndex == newIndex)
					{
						return;
					}

					var oldNode = nodeList[newIndex];
					nodeList[newIndex] = node;
					nodeList[thisIndex] = oldNode;
				}
			}
		}

		//CONNECTIONS

		/// <summary>
		/// Add a connection between 2 connection slots in an arbitrary order.
		/// This must be done here in the graph so we keep a proper reference of it,
		/// not in the connection slots themselves.
		/// </summary>
		/// <param name="slot1"></param>
		/// <param name="slot2"></param>
		/// <returns>True when we did add a connection</returns>
		public bool AddNodeConnection(VTGraphConnectionSlot slot1, VTGraphConnectionSlot slot2)
		{
			if(slot1.connectionSlotType == slot2.connectionSlotType)
			{
				//Don't connect input to input
				//or output to output
				return false;
			}
			if(SlotsAreConnected(slot1, slot2))
			{
				//These slots are already connected
				return false;
			}

			//TODO: In the future, when we encounter an input slot that is already taken,
			//just disconnect that one first and then connect this

			if (!slot1.CanAddConnectedSlot() || !slot2.CanAddConnectedSlot())
			{
				return false;
			}

			slot1.AddConnectedSlot(slot2);
			slot2.AddConnectedSlot(slot1);

			var newConnectionRef = new VTGraphConnectionReference(slot1, slot2);
			connectionsList.Add(newConnectionRef);

			return true;
		}

		/// <summary>
		/// Disconnect ConnectionSlot references to each other
		/// and then remove the ConnectionRef in our connections list.
		/// </summary>
		/// <param name="connectionRef"></param>
		public void RemoveNodeConnection(VTGraphConnectionReference connectionRef)
		{
			if(!connectionsList.Contains(connectionRef))
			{
				return;
			}

			connectionRef.slot1.RemoveConnectedSlot(connectionRef.slot2);
			connectionRef.slot2.RemoveConnectedSlot(connectionRef.slot1);

			connectionsList.Remove(connectionRef);
		}

		/// <summary>
		/// True if we have a reference that says both slots
		/// are connected in any order already.
		/// </summary>
		/// <param name="slot1"></param>
		/// <param name="slot2"></param>
		/// <returns></returns>
		public bool SlotsAreConnected(VTGraphConnectionSlot slot1, VTGraphConnectionSlot slot2)
		{
			foreach(VTGraphConnectionReference connectionRef in connectionsList)
			{
				if(slot1 == connectionRef.slot1 && slot2 == connectionRef.slot2)
				{
					return true;
				}
				if (slot1 == connectionRef.slot2 && slot2 == connectionRef.slot1)
				{
					return true;
				}
			}

			return false;
		}

		public List<VTGraphConnectionReference> GetConnectionsWithNode(VTGraphNode node)
		{
			var ret = new List<VTGraphConnectionReference>();

			foreach(var connectionRef in connectionsList)
			{
				if(connectionRef.slot1.parentNode == node || connectionRef.slot2.parentNode == node)
				{
					ret.Add(connectionRef);
				}
			}

			return ret;
		}
	}
}