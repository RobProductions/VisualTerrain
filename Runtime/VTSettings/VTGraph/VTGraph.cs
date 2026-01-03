using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RobProductions.VisualTerrain.Runtime.VTGraphConnectionSlot;

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
			[field: SerializeField]
			public float ViewScale { get; set; } = 1.0f;
		}

		[SerializeField]
		public VTGraphDisplayData displayData = new VTGraphDisplayData();

		public enum GraphType
		{
			Height = 0,
			Texture = 1,
			TerrainObject = 2,
			CustomObject = 3,
			SubGraph = 4
		}

		[SerializeField]
		public GraphType graphType = GraphType.Height;

		[SerializeField, SerializeReference]
		public List<VTGraphNode> nodeList = new List<VTGraphNode>();
		[SerializeField, SerializeReference]
		public List<VTGraphConnection> connectionsList = new List<VTGraphConnection>();

		//LIFECYCLE

		public void OnGraphEnable()
		{
			foreach(VTGraphNode thisNode in nodeList)
			{
				thisNode.SetParentGraph(this);
			}
		}

		public void OnGraphDisable()
		{

		}

		//GRAPH

		public void SetGraphType(GraphType v)
		{
			graphType = v;
		}

		//NODES

		public VTGraphNode CreateNode<T>(T existingNode) where T : VTGraphNode, new ()
		{
			return CreateNode<T>();
		}

		public VTGraphNode CreateNode<T>(T existingNode, Vector2 startingPosition) where T : VTGraphNode, new()
		{
			return CreateNode<T>(startingPosition);
		}

		public VTGraphNode CreateNode<T>() where T : VTGraphNode, new()
		{
			return CreateNode<T>(Vector2.zero);
		}

		public VTGraphNode CreateNode<T>(Vector2 startingPosition) where T : VTGraphNode, new()
		{
			T newNode = new T();
			AddNode(newNode);

			SetNodePosition(newNode, startingPosition);

			return newNode;
		}

		/// <summary>
		/// Create a node with the given runtime type.
		/// If the type is not accepted, prints an error and returns null.
		/// </summary>
		/// <param name="nodeType"></param>
		/// <param name="startingPosition"></param>
		/// <returns></returns>
		public VTGraphNode CreateNode(System.Type nodeType, Vector2 startingPosition)
		{
			if (!typeof(VTGraphNode).IsAssignableFrom(nodeType))
			{
				VTLog.LogError("NodeType " + nodeType.ToString() + " is not inherited from VTGraphNode in VTGraph.CreateNode()!");
				return null;
			}

			VTGraphNode newNode = (VTGraphNode)Activator.CreateInstance(nodeType);
			AddNode(newNode);

			SetNodePosition(newNode, startingPosition);

			return newNode;
		}

		/// <summary>
		/// Add the node to this graph's nodelist and set the parent graph
		/// of the node to this.
		/// </summary>
		/// <param name="node"></param>
		public void AddNode(VTGraphNode node)
		{
			node.SetParentGraph(this);
			nodeList.Add(node);
		}

		/// <summary>
		/// Remove the node from the graph's nodelist and clear
		/// the parent graph reference.
		/// </summary>
		/// <param name="node"></param>
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

			node.SetParentGraph(null);
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

		//PROCESSING

		/// <summary>
		/// Process all nodes in this island group by first traversing to
		/// the leaf nodes and calculating inputs backwards from
		/// there. When complete, all connected nodes
		/// will be up to date in their output values.
		/// </summary>
		/// <param name="startingNode"></param>
		public void ProcessAllConnectedNodes(VTGraphNode startingNode, VTGraphProcessingSettings settings)
		{
			//Visit all leaf branches to process them so that
			//all required inputs get calculated
			Stack<VTGraphNode> stack = new Stack<VTGraphNode>();
			stack.Push(startingNode);
			HashSet<VTGraphNode> handledNodeList = new HashSet<VTGraphNode>();

			while (stack.Count > 0)
			{
				VTGraphNode currentNode = stack.Pop();
				if(handledNodeList.Contains(currentNode))
				{
					continue;
				}
				handledNodeList.Add(currentNode);
				bool processedNode = false;

				foreach(VTGraphConnectionSlot slot in currentNode.outputConnections)
				{
					if(slot.IsConnected())
					{
						var allConnections = GetConnectionsToSlot(slot);
						foreach(VTGraphConnection connection in allConnections)
						{
							if (connection.inputSlot.parentNode != currentNode)
							{
								stack.Push(connection.inputSlot.parentNode);
							}
						}
					}
					else
					{
						if(!processedNode)
						{
							ProcessNode(currentNode, settings);
							processedNode = true;
						}
					}
				}
			}
		}

		/// <summary>
		/// Process all input slot values from connected nodes
		/// and then process this node's output values.
		/// </summary>
		/// <param name="node"></param>
		public void ProcessNode(VTGraphNode node)
		{
			ProcessNode(node, new VTGraphProcessingSettings());
		}

		/// <summary>
		/// Process all input slot values from connected nodes
		/// and then process this node's output values.
		/// Use VTGraphProcessingSettings to create
		/// more performant preview processing.
		/// </summary>
		/// <param name="node"></param>
		public void ProcessNode(VTGraphNode node, VTGraphProcessingSettings settings)
		{
			TraverseInputsProcessNode(node, new HashSet<VTGraphNode>(), settings);
		}

		/// Calculate the input slot values for this node by processing 
		/// the output values of their connections via recursion,
		/// and then process this node's output. Adds this node
		/// to the handledNodes to stop infinite chains.
		void TraverseInputsProcessNode(VTGraphNode node, HashSet<VTGraphNode> handledNodes, VTGraphProcessingSettings settings)
		{
			if(handledNodes.Contains(node))
			{
				//We already handled this node in the recursion chain.
				return;
			}
			handledNodes.Add(node);
			foreach (VTGraphConnectionSlot slot in node.inputConnections)
			{
				CalculateInputValueFromConnection(slot, handledNodes, settings);
			}
			node.ProcessNode(settings);
		}

		/// <summary>
		/// Calculate the given input slot's value
		/// by processing connected nodes.
		/// </summary>
		/// <param name="slot"></param>
		void CalculateInputValueFromConnection(VTGraphConnectionSlot slot, HashSet<VTGraphNode> handledNodes, VTGraphProcessingSettings settings)
		{
			var connection = GetConnectionToInputSlot(slot);
			if (connection != null)
			{
				//Process the value of the connected node
				TraverseInputsProcessNode(connection.outputSlot.parentNode, handledNodes, settings);
				//Now we can set this value from that
				slot.SetRangeGridValue(connection.outputSlot.rangeGridValue);
				slot.SetFloatValue(connection.outputSlot.floatValue);
			}
			else
			{
				//Just set the default value
				bool calculatingSubgraphNode = slot.parentNode.SubGraphNode && settings.calculatingSubgraph;
				if(!calculatingSubgraphNode)
				{
					//But only if we're not calculating a node for an unseen subgraph 
					slot.SetRangeGridValue(slot.defaultRangeGridValue);
					slot.SetFloatValue(slot.defaultFloatValue);
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
			if (slot1.slotHidden || slot2.slotHidden)
			{
				//Cannot connect hidden slots
				return false;
			}
			if(slot1.IsConnected() && slot1.connectionSlotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
			{
				//Disconnect existing connections if we're a full input
				var existingConnection = GetConnectionToInputSlot(slot1);
				RemoveNodeConnection(existingConnection);
			}
			if (slot2.IsConnected() && slot2.connectionSlotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
			{
				//Disconnect existing connections if we're a full input
				var existingConnection = GetConnectionToInputSlot(slot2);
				RemoveNodeConnection(existingConnection);
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

			//Reprocess the input node since it has now gotten a new value
			//ProcessAllConnectedNodes(newConnectionRef.inputSlot.parentNode, new VTGraphProcessingSettings());

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

			//Reprocess just the input node since the output doesn't change
			//ProcessAllConnectedNodes(connection.inputSlot.parentNode);
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

		/// <summary>
		/// Get a list of all the nodes that are connected to this
		/// node via GraphConnection, on all slot types.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public List<VTGraphNode> GetConnectedNodes(VTGraphNode node)
		{
			var ret = new List<VTGraphNode>();
			var allConnections = GetConnectionsWithNode(node);
			foreach(VTGraphConnection connection in allConnections)
			{
				if(connection.inputSlot.parentNode != node)
				{
					if(!ret.Contains(connection.inputSlot.parentNode))
					{
						ret.Add(connection.inputSlot.parentNode);
					}
				}
				if(connection.outputSlot.parentNode != node)
				{
					if (!ret.Contains(connection.outputSlot.parentNode))
					{
						ret.Add(connection.outputSlot.parentNode);
					}
				}
			}

			return ret;
		}

		/// <summary>
		/// Return all nodes connected to output slots of this node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public List<VTGraphNode> GetOutputConnectedNodes(VTGraphNode node)
		{
			var ret = new List<VTGraphNode>();
			var allConnections = GetConnectionsWithNode(node);
			foreach (VTGraphConnection connection in allConnections)
			{
				if(connection.inputSlot.parentNode == node)
				{
					//This is an input connection
					continue;
				}
				if (!ret.Contains(connection.inputSlot.parentNode))
				{
					ret.Add(connection.inputSlot.parentNode);
				}
			}

			return ret;
		}

		/// <summary>
		/// Get all connections associated with this node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Get the node attached to this input slot if present,
		/// null if not connected.
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public VTGraphNode GetNodeConnectedToInputSlot(VTGraphConnectionSlot slot)
		{
			if (slot.connectionSlotType != NodeConnectionSlotType.Input)
			{
				VTLog.LogWarning("Tried to get node connected to InputSlot but slot type was not Input!");
				return null;
			}
			var connection = GetConnectionToInputSlot(slot);
			if(connection != null)
			{
				return connection.outputSlot.parentNode;
			}

			return null;
		}

		/// <summary>
		/// Get an InputSlot's connection if present, null if not connected.
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public VTGraphConnection GetConnectionToInputSlot(VTGraphConnectionSlot slot)
		{
			if (slot.connectionSlotType != VTGraphConnectionSlot.NodeConnectionSlotType.Input)
			{
				VTLog.LogWarning("Tried to get InputSlot connection when slot was not type Input in GetConnectionToInputSlot!");
				return null;
			}
			var connections = GetConnectionsToSlot(slot);
			if (connections.Count < 1)
			{
				return null;
			}
			if (connections.Count > 1)
			{
				VTLog.LogWarning("Slot with type input had more than one connection in GetConnectionToInputSlot!");
			}

			return connections[0];
		}

		/// <summary>
		/// Get all connections that are associated with this slot.
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
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