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
			[field: SerializeField]
			public Vector2 ViewOffset { get; set; } = Vector2.zero;
		}

		[SerializeField]
		public VTGraphDisplayData displayData = new VTGraphDisplayData();

		[SerializeField, SerializeReference]
		public List<VTGraphNode> nodeList = new List<VTGraphNode>();
		[SerializeField, SerializeReference]
		public List<VTGraphConnection> connectionsList = new List<VTGraphConnection>();

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
			if(slot1.parentNode == slot2.parentNode)
			{
				//Cannot connect node to itself
				return false;
			}
			if(slot1.IsConnected() && slot1.connectionSlotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
			{
				//Disconnect existing connections if we're a full input
				var existingConnections = GetConnectionsToSlot(slot1);
				foreach(VTGraphConnection connection in existingConnections)
				{
					RemoveNodeConnection(connection);
				}
			}
			if (slot2.IsConnected() && slot2.connectionSlotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
			{
				//Disconnect existing connections if we're a full input
				var existingConnections = GetConnectionsToSlot(slot2);
				foreach (VTGraphConnection connection in existingConnections)
				{
					RemoveNodeConnection(connection);
				}
			}
			if (!slot1.CanAddConnectedSlot() || !slot2.CanAddConnectedSlot())
			{
				//We still can't add the connection for some reason, so bail
				return false;
			}

			slot1.AddConnectedSlot();
			slot2.AddConnectedSlot();
			var inputSlot = slot1;
			var outputSlot = slot2;
			if(slot1.connectionSlotType == VTGraphConnectionSlot.NodeConnectionSlotType.Output)
			{
				inputSlot = slot2;
				outputSlot = slot1;
			}
			var newConnectionRef = new VTGraphConnection(inputSlot, outputSlot);
			connectionsList.Add(newConnectionRef);

			return true;
		}

		/// <summary>
		/// Disconnect ConnectionSlot references to each other
		/// and then remove the VTGraphConnection in our connections list.
		/// </summary>
		/// <param name="connectionRef"></param>
		public void RemoveNodeConnection(VTGraphConnection connection)
		{
			if(!connectionsList.Contains(connection))
			{
				return;
			}

			connection.inputSlot.RemoveConnectedSlot();
			connection.outputSlot.RemoveConnectedSlot();

			connectionsList.Remove(connection);
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
			foreach(VTGraphConnection connection in connectionsList)
			{
				if(slot1 == connection.inputSlot && slot2 == connection.outputSlot)
				{
					return true;
				}
				if (slot1 == connection.outputSlot && slot2 == connection.inputSlot)
				{
					return true;
				}
			}

			return false;
		}

		public List<VTGraphConnection> GetConnectionsWithNode(VTGraphNode node)
		{
			var ret = new List<VTGraphConnection>();

			foreach(var connection in connectionsList)
			{
				if((connection.inputSlot != null && connection.inputSlot.parentNode == node) || (connection.outputSlot != null && connection.outputSlot.parentNode == node))
				{
					ret.Add(connection);
				}
			}

			return ret;
		}

		public List<VTGraphConnection> GetConnectionsToSlot(VTGraphConnectionSlot slot)
		{
			var ret = new List<VTGraphConnection>();

			foreach (var connection in connectionsList)
			{
				if ((connection.inputSlot != null && connection.inputSlot == slot) || (connection.outputSlot != null && connection.outputSlot == slot))
				{
					ret.Add(connection);
				}
			}

			return ret;
		}
	}
}