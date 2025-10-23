#if UNITY_EDITOR

using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTEditorGraphView
	{
		public class GraphViewStyles
		{
			//UI Values
			public readonly Vector2 nodeBaseSize = new Vector2(200f, 50f);
			public readonly Vector2 nodeExpandedSize = new Vector2(200f, 130f);
			public readonly float nodeExtraHeightPerSlot = 30f;
			public readonly float nodePreviewImageHeight = 76f;
			public readonly float nodePreviewImagePadding = 8f;
			public readonly float nodeTitleOffset = 4.5f;

			public readonly float connectionPointInitialVertical = 24f;
			public readonly float connectionPointVerticalSpacing = 24f;
			public readonly float connectionPointSize = 11f;
			public readonly float halfConnectionPointSize = 7f;
			public readonly float connectionLineWidth = 3f;
			public readonly float connectionPointExtraClickHeight = 5f;
			public readonly Vector2 connectionLabelInputOffset = new Vector2(15f, -4f);

			public readonly float grid1Spacing = 20f;
			public readonly float grid2Spacing = 80f;

			//UI Colors
			public readonly Color connectionLineColor = new Color(0.8f, 0.85f, 0.92f);
			public readonly Color connectionLineDragColor = new Color(0.7f, 0.72f, 1.0f);
			public readonly Color windowBGColor = new Color(0.4f, 0.4f, 0.4f);
			public readonly Color nodeLabelColor = new Color(1.0f, 1.0f, 1.0f, 0.4f);

			//UI Styles

			public GUIStyle windowBgStyle;

			public GUIStyle defaultNodeStyle;
			public GUIStyle selectedNodeStyle;
			public GUIStyle nodeTitleStyle;

			public GraphViewStyles()
			{
				defaultNodeStyle = new GUIStyle();
				//defaultNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
				defaultNodeStyle.normal.background = EditorGUIUtility.Load("mini btn on@2x") as Texture2D;
				defaultNodeStyle.normal.textColor = Color.black;
				defaultNodeStyle.alignment = TextAnchor.UpperCenter;
				defaultNodeStyle.padding = new RectOffset(10, 10, 10, 10);
				defaultNodeStyle.border = new RectOffset(10, 10, 10, 10);

				selectedNodeStyle = new GUIStyle(defaultNodeStyle);
				//selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
				selectedNodeStyle.normal.background = EditorGUIUtility.Load("mini btn on focus@2x") as Texture2D;

				nodeTitleStyle = new GUIStyle(defaultNodeStyle);
				nodeTitleStyle.normal.background = null;

				//Background solid color texture
				windowBgStyle = new GUIStyle(GUI.skin.box);

				//windowBgStyle.normal.background = EditorGUIUtility.Load("transparent") as Texture2D;
			}
		}

		private GraphViewStyles styles;

		private class GraphViewData
		{
			public VTGraph currentGraph;

			public List<VTGraphNode> selectedGraphNodes = new List<VTGraphNode>();
			public VTGraphNode draggingNode;
			public VTGraphNode startClickOnNode;
			public VTGraphConnectionSlot draggingConnectionSlot;
			public bool draggedNodeOnMousePress = false;
			public bool draggedViewOnMousePress = false;

			/// <summary>
			/// When we don't have a currentGraph loaded, we still let the
			/// user move the graph view around and use this to track the offset
			/// </summary>
			public Vector2 noGraphViewOffset = Vector2.zero;

			public Vector2 draggingConnectionMousePosition = Vector2.zero;

			public Dictionary<VTGraphNode, Texture2D> nodePreviewImageMap = new Dictionary<VTGraphNode, Texture2D>();

			public VTGraphProcessingSettings previewProcessingSettings = new VTGraphProcessingSettings(
				VTGraphProcessingSettings.TextureGenerationResolution.RestrictToSize,
				VTGraphProcessingSettings.TextureOutputResolution.RestrictToSize
			);
		}

		private GraphViewData data = new GraphViewData();

		private VTEditorWindow parentWindow;

		public VTEditorGraphView(VTEditorWindow parentWindow)
		{
			this.parentWindow = parentWindow;
		}

		//LIFECYCLE

		public void OnEnable()
		{

		}

		public void OnDisable()
		{

		}

		/// <summary>
		/// GUI styles can only be created
		/// from OnGUI since we rely on GUI.skin,
		/// so only call this from OnGUI thread
		/// </summary>
		void CreateGUIStyles()
		{
			if(styles == null)
			{
				styles = new GraphViewStyles();
			}
		}

		//GRAPH

		public void SetTargetGraph(VTGraph newGraph)
		{
			if(data.currentGraph == newGraph)
			{
				return;
			}

			if(data.currentGraph != null)
			{

			}

			data.currentGraph = newGraph;

			data.draggingNode = null;
			data.startClickOnNode = null;
			data.draggedViewOnMousePress = false;
			ClearSelectedGraphNodes();
			data.draggingConnectionSlot = null;

			RegenerateAllNodePreviewImages(true);
		}

		public void RegenerateAllNodePreviewImages(bool markEditAsset)
		{
			data.nodePreviewImageMap.Clear();
			if (data.currentGraph == null)
			{
				return;
			}

			foreach(VTGraphNode node in data.currentGraph.nodeList)
			{
				TryUpdateNodePreviewImage(node);
			}

			if(markEditAsset)
			{
				parentWindow.EditedAsset();
			}
		}

		public void TryUpdateNodePreviewImage(VTGraphNode node)
		{
			if(node == null)
			{
				return;
			}

			if(data.nodePreviewImageMap.ContainsKey(node))
			{
				data.nodePreviewImageMap.Remove(node);
			}

			var output = node.GetOutputConnection();
			if (output != null)
			{
				
				data.currentGraph.ProcessNode(node, data.previewProcessingSettings);
				var textureValue = output.GetTextureValue();
				if(textureValue != null)
				{
					data.nodePreviewImageMap[node] = textureValue;
				}
			}
		}

		public void TryUpdateOutputConnectedPreviews(VTGraphNode node)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			HashSet<VTGraphNode> handledNodes = new HashSet<VTGraphNode>();
			Stack<VTGraphNode> outputConnectedNodes = new Stack<VTGraphNode>();
			outputConnectedNodes.Push(node);

			while(outputConnectedNodes.Count > 0)
			{
				var thisNode = outputConnectedNodes.Pop();
				if(handledNodes.Contains(thisNode))
				{
					continue;
				}
				handledNodes.Add(thisNode);
				TryUpdateNodePreviewImage(thisNode);

				foreach (VTGraphConnectionSlot slot in thisNode.outputConnections)
				{
					if (slot.IsConnected())
					{
						var allConnections = data.currentGraph.GetConnectionsToSlot(slot);
						foreach (VTGraphConnection connection in allConnections)
						{
							if (connection.inputSlot.parentNode != thisNode)
							{
								outputConnectedNodes.Push(connection.inputSlot.parentNode);
							}
						}
					}
				}
			}
		}

		//NODE/GRAPH INTERACTIONS

		void CreateNodeAtPosition<T>(Vector2 position) where T : VTGraphNode, new()
		{
			if(data.currentGraph == null)
			{
				return;
			}
			var positionMinusOffset = position - GetCurrentViewOffset();

			parentWindow.RegisterAssetDataUndo("Created New Node");
			data.currentGraph.CreateNode<T>(positionMinusOffset);
			parentWindow.EditedAsset();
		}

		void DeleteNodes(List<VTGraphNode> nodeList)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			parentWindow.RegisterAssetStructureUndo("Deleted Nodes");
			foreach(VTGraphNode node in nodeList)
			{
				data.currentGraph.RemoveNode(node);
			}
			RegenerateAllNodePreviewImages(false);
			parentWindow.EditedAsset();
		}

		void UpdateNodePreview(List<VTGraphNode> nodeList)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			//parentWindow.RegisterAssetStructureUndo("Processed Nodes");
			foreach (VTGraphNode node in nodeList)
			{
				TryUpdateNodePreviewImage(node);
			}
			parentWindow.EditedAsset();
		}

		void CreateNodeConnection(VTGraphConnectionSlot slot1, VTGraphConnectionSlot slot2)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			parentWindow.RegisterAssetStructureUndo("Connected Nodes");
			data.currentGraph.AddNodeConnection(slot1, slot2);

			//Update preview of both nodes
			TryUpdateOutputConnectedPreviews(slot1.parentNode);
			TryUpdateOutputConnectedPreviews(slot2.parentNode);

			parentWindow.EditedAsset();
		}

		void DeleteNodeConnection(VTGraphConnection connection)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			parentWindow.RegisterAssetStructureUndo("Disconnected Nodes");
			data.currentGraph.RemoveNodeConnection(connection);

			//Update preview of the input node and its output
			TryUpdateOutputConnectedPreviews(connection.inputSlot.parentNode);

			parentWindow.EditedAsset();
		}

		void SetNodesExpanded(List<VTGraphNode> nodes, bool setExpanded)
		{
			if (data.currentGraph == null)
			{
				return;
			}

			parentWindow.RegisterAssetStructureUndo("Set Nodes Expanded");
			foreach (VTGraphNode node in nodes)
			{
				if(!data.currentGraph.nodeList.Contains(node))
				{
					continue;
				}
				if(node.IsExpanded == setExpanded)
				{
					continue;
				}
				node.IsExpanded = setExpanded;
			}
			parentWindow.EditedAsset();
		}

		void BringNodeToFront(VTGraphNode node)
		{
			if(data.currentGraph == null)
			{
				return;
			}
			if(!data.currentGraph.nodeList.Contains(node))
			{
				return;
			}
			var indexOfNode = data.currentGraph.nodeList.IndexOf(node);
			var lastIndex = data.currentGraph.nodeList.Count - 1;

			if (data.currentGraph.nodeList.Count > 1 && indexOfNode != lastIndex)
			{
				parentWindow.RegisterAssetStructureUndo("Changed Node Order");
				data.currentGraph.SetNodeIndex(node, lastIndex);
				parentWindow.EditedAsset();
			}
		}

		void DragNode(VTGraphNode node, Vector2 delta)
		{
			if (data.currentGraph == null)
			{
				return;
			}

			parentWindow.RegisterAssetDataUndo("Dragged Node");
			data.currentGraph.SetNodePosition(node, node.NodePosition + delta);
			parentWindow.EditedAsset();
		}

		void DragNodes(List<VTGraphNode> draggingNodes, Vector2 delta)
		{
			if (data.currentGraph == null)
			{
				return;
			}

			parentWindow.RegisterAssetDataUndo("Dragged Nodes");
			foreach(VTGraphNode node in draggingNodes)
			{
				data.currentGraph.SetNodePosition(node, node.NodePosition + delta);
			}
			parentWindow.EditedAsset();
		}

		void DragView(Vector2 delta)
		{
			if (data.currentGraph != null)
			{
				//No need to register undo for view change
				data.currentGraph.displayData.ViewOffset += delta;
				parentWindow.EditedAsset();
			}
			else
			{
				data.noGraphViewOffset += delta;
			}
		}

		void FocusView(List<VTGraphNode> optionalFocusNodes)
		{
			if(data.currentGraph != null)
			{
				//No need to register undo for view change
				var accumulatedViewOffset = Vector2.zero;
				var parentWindowCenter = new Vector2(parentWindow.position.width * 0.5f, parentWindow.position.height * 0.5f) - (styles.nodeBaseSize * 0.5f);
				if(optionalFocusNodes != null && optionalFocusNodes.Count > 0)
				{
					foreach (VTGraphNode node in optionalFocusNodes)
					{
						accumulatedViewOffset += -node.NodePosition + parentWindowCenter;
					}
					accumulatedViewOffset /= optionalFocusNodes.Count;
				}
				else if (data.currentGraph.nodeList.Count > 0)
				{
					foreach(VTGraphNode node in data.currentGraph.nodeList)
					{
						accumulatedViewOffset += -node.NodePosition + parentWindowCenter;
					}
					accumulatedViewOffset /= data.currentGraph.nodeList.Count;
				}

				data.currentGraph.displayData.ViewOffset = accumulatedViewOffset;
				parentWindow.EditedAsset();
			}
			else
			{
				data.noGraphViewOffset = Vector2.zero;
			}
		}

		//EVENTS AND SELECTION

		/// <summary>
		/// Process events for the graph view.
		/// If we definitely handled an event, return true.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public bool ProcessEvents(Event e)
		{
			switch (e.type)
			{
				case EventType.MouseDown:
					var overNode = GetTopNodeAtPosition(e.mousePosition);
					if (e.button == 1)
					{
						if(overNode != null)
						{
							if(!data.selectedGraphNodes.Contains(overNode))
							{
								//We haven't already selected this node, so just selection to it
								SetSelectedGraphNode(overNode);
							}
						}
						ProcessContextMenu(e.mousePosition, overNode);
						GUI.changed = true;
					}
					else if (e.button == 0)
					{
						var overSlot = GetTopConnectionSlotAtPosition(e.mousePosition, true);
						if(overSlot != null && !overSlot.slotHidden)
						{
							data.draggingConnectionSlot = overSlot;
							data.draggingConnectionMousePosition = e.mousePosition;
						}
						if(overNode != null)
						{
							data.draggingNode = overNode;
							data.startClickOnNode = overNode;
						}
					}
					break;
				case EventType.MouseDrag:
					if ((e.button == 0 && e.alt) || e.button == 2)
					{
						//Left alt + left click or middle click will drag the view
						data.draggedViewOnMousePress = true;
						DragView(e.delta);
						GUI.changed = true;
						return true;
					}
					else if (e.button == 0)
					{
						//Regular left click can drag a node or connection point
						if(data.draggedViewOnMousePress)
						{
							//But not if we were dragging the view first
							return false;
						}

						if(data.currentGraph != null && data.draggingConnectionSlot != null)
						{
							data.draggingConnectionMousePosition = e.mousePosition;
							GUI.changed = true;
							return true;
						}

						if(data.currentGraph != null && data.draggingNode != null)
						{
							if(data.selectedGraphNodes.Contains(data.draggingNode))
							{
								//Drag the whole group
								DragNodes(data.selectedGraphNodes, e.delta);
							}
							else
							{
								if(e.shift)
								{
									//Add to selected node group and drag
									AddSelectedGraphNode(data.draggingNode);
									DragNodes(data.selectedGraphNodes, e.delta);
								}
								else
								{
									//Just select this node and drag it
									SetSelectedGraphNode(data.draggingNode);
									DragNode(data.draggingNode, e.delta);
								}
							}

							data.draggedNodeOnMousePress = true;
							GUI.changed = true;
							return true;
						}
					}
					break;
				case EventType.MouseUp:
					if (e.button == 0 && !e.alt)
					{
						if (data.currentGraph != null && !data.draggedViewOnMousePress)
						{
							bool selectedANode = false;
							var nodeAtMousePosition = GetTopNodeAtPosition(e.mousePosition);
							if (nodeAtMousePosition != null && data.startClickOnNode == nodeAtMousePosition)
							{
								//Select a node or add it to selection
								if (e.shift)
								{
									AddSelectedGraphNode(nodeAtMousePosition);
								}
								else if (!data.draggedNodeOnMousePress)
								{
									SetSelectedGraphNode(nodeAtMousePosition);
								}
								selectedANode = true;
								GUI.changed = true;
							}

							if (!selectedANode)
							{
								ClearSelectedGraphNodes();
								GUI.changed = true;
							}
						}

						data.startClickOnNode = null;
						data.draggedViewOnMousePress = false;
					}
					if (e.button == 0)
					{
						if(data.draggingConnectionSlot != null)
						{
							//Handle ending connection creation
							var overSlot = GetTopConnectionSlotAtPosition(e.mousePosition, true);
							if (overSlot != null)
							{
								CreateNodeConnection(data.draggingConnectionSlot, overSlot);
							}
						}

						data.draggingNode = null;
						data.draggedNodeOnMousePress = false;
						data.draggingConnectionSlot = null;
					}
					break;
				case EventType.ScrollWheel:
					//OnScroll(e.delta, e.mousePosition);
					break;
				case EventType.KeyDown:
					if(e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
					{
						//Delete the selected node
						if (data.selectedGraphNodes.Count > 0)
						{
							DeleteNodes(data.selectedGraphNodes);
							GUI.changed = true;
						}
					}
					else if (e.keyCode == KeyCode.F)
					{
						//Focus the view on all nodes or selected node
						FocusView(data.selectedGraphNodes);
						GUI.changed = true;
					}
					else if (e.keyCode == KeyCode.E)
					{
						//Set the selected nodes expanded status
						if(data.selectedGraphNodes.Count > 0)
						{
							bool setExpanded = true;
							if (data.selectedGraphNodes[0].IsExpanded)
							{
								setExpanded = false;
							}
							SetNodesExpanded(data.selectedGraphNodes, setExpanded);
							GUI.changed = true;
						}
					}
					break;
			}

			return false;
		}

		void ProcessContextMenu(Vector2 mousePosition, VTGraphNode overNode)
		{
			GenericMenu genericMenu = new GenericMenu();
			if (data.currentGraph != null)
			{
				genericMenu.AddItem(new GUIContent("Add Test Node/Test Node"), false, () => CreateNodeAtPosition<VTGraphNodeTest>(mousePosition));
				genericMenu.AddItem(new GUIContent("Add Input Node/Simple Noise"), false, () => CreateNodeAtPosition<VTGraphNodeSimpleNoise>(mousePosition));
				genericMenu.AddItem(new GUIContent("Add Math Node/Arithmetic"), false, () => CreateNodeAtPosition<VTGraphNodeArithmetic>(mousePosition));
				genericMenu.AddItem(new GUIContent("Add Output Node/Height Output"), false, () => CreateNodeAtPosition<VTGraphNodeHeightOutput>(mousePosition));

				if(overNode != null)
				{
					genericMenu.AddSeparator("");
					string deleteNodeTitle = "Delete Node";
					string updateNodePreviewTitle = "Update Node Preview";
					//string processConnectedNodeTitle = "Process All Connected";
					if (data.selectedGraphNodes.Count > 1 && data.selectedGraphNodes.Contains(overNode))
					{
						//Rename title because we're affecting multiple nodes
						deleteNodeTitle = "Delete Nodes";
						updateNodePreviewTitle = "Update Node Preview";
					}

					var closureNodes = data.selectedGraphNodes;
					genericMenu.AddItem(new GUIContent(updateNodePreviewTitle), false, () => UpdateNodePreview(closureNodes));
					genericMenu.AddItem(new GUIContent(deleteNodeTitle), false, () => DeleteNodes(closureNodes));
				}
			}
			if (genericMenu.GetItemCount() > 0)
			{
				genericMenu.ShowAsContext();
			}
		}

		void ClearSelectedGraphNodes()
		{
			data.selectedGraphNodes.Clear();
			GUI.FocusControl(null);
		}

		void RemoveSelectedGraphNode(VTGraphNode v)
		{
			if(v == null)
			{
				return;
			}
			if(data.selectedGraphNodes.Contains(v))
			{
				data.selectedGraphNodes.Remove(v);
			}
		}

		void AddSelectedGraphNode(VTGraphNode v)
		{
			if(v == null)
			{
				return;
			}
			if(data.selectedGraphNodes.Contains(v))
			{
				return;
			}

			data.selectedGraphNodes.Add(v);
			GUI.FocusControl(null);
		}

		void SetSelectedGraphNode(VTGraphNode v)
		{
			if(v == null)
			{
				return;
			}
			ClearSelectedGraphNodes();
			AddSelectedGraphNode(v);
			BringNodeToFront(v);
		}

		public List<VTGraphNode> GetSelectedGraphNodes()
		{
			return data.selectedGraphNodes;
		}

		//RENDERING

		public void DrawGraphView(Rect graphViewRect)
		{
			//First created needed GUIStyles
			CreateGUIStyles();

			//Then draw background grid
			DrawBackgroundColor(graphViewRect);
			DrawGrid(styles.grid1Spacing, 0.1f, Color.black, graphViewRect);
			DrawGrid(styles.grid2Spacing, 0.25f, Color.black, graphViewRect);

			//Draw nodes
			DrawGraphConnections();
			DrawGraphNodes();
			if(data.draggingConnectionSlot != null)
			{
				var halfConnectionSize = Vector2.one * styles.halfConnectionPointSize;
				var slotPosition = GetConnectionSlotPosition(data.draggingConnectionSlot) + halfConnectionSize;
				if(data.draggingConnectionSlot.connectionSlotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
				{
					DrawGraphConnectionLine(slotPosition, data.draggingConnectionMousePosition, true);
				}
				else
				{
					DrawGraphConnectionLine(data.draggingConnectionMousePosition, slotPosition, true);
				}
			}
		}

		void DrawGraphNodes()
		{
			if(data.currentGraph == null)
			{
				return;
			}

			for (int i = 0; i < data.currentGraph.nodeList.Count; i++)
			{
				var thisNode = data.currentGraph.nodeList[i];
				DrawGraphNode(thisNode);
			}
		}

		void DrawGraphConnections()
		{
			if (data.currentGraph == null)
			{
				return;
			}

			for(int i = 0; i < data.currentGraph.connectionsList.Count; i++)
			{
				var thisConnection = data.currentGraph.connectionsList[i];
				DrawGraphConnection(thisConnection);
			}
		}

		void DrawGraphNode(VTGraphNode node)
		{
			if(data.currentGraph == null)
			{
				return;
			}
			if (node == null)
			{
				VTLog.LogError("Node was null in DrawGraphNode!");
				return;
			}

			var nodeRect = GetNodeRenderRect(node);

			var finalNodeStyle = styles.defaultNodeStyle;
			if(data.selectedGraphNodes.Contains(node))
			{
				finalNodeStyle = styles.selectedNodeStyle;
			}

			//Render the node box
			GUI.Box(nodeRect, "", finalNodeStyle);

			//Render label
			var titleBGRect = new Rect(nodeRect.position + new Vector2(5f, 4f), new Vector2(nodeRect.size.x - 10f, 16f));

			var bgColor = GUI.backgroundColor;
			GUI.backgroundColor = styles.nodeLabelColor;
			GUI.Box(titleBGRect, "", GUI.skin.box);
			GUI.backgroundColor = bgColor;

			var titleRect = new Rect(nodeRect.position - new Vector2(0.0f, styles.nodeTitleOffset), nodeRect.size);
			GUI.Label(titleRect, node.NodeTitle, styles.nodeTitleStyle);

			if(node.IsExpanded)
			{
				if(data.nodePreviewImageMap.ContainsKey(node))
				{
					if (data.nodePreviewImageMap[node] != null)
					{
						var textureRelativePos = new Vector2(styles.nodePreviewImagePadding, nodeRect.size.y - styles.nodePreviewImageHeight - styles.nodePreviewImagePadding);
						var textureSize = new Vector2(nodeRect.size.x - (styles.nodePreviewImagePadding * 2.0f), styles.nodePreviewImageHeight);

						var textureRegion = new Rect(nodeRect.position + textureRelativePos, textureSize);
						GUI.DrawTexture(textureRegion, data.nodePreviewImageMap[node], ScaleMode.ScaleToFit);
					}
				}
			}

			//var nodeOutput = node.outputConnections
			DrawConnectionSlotsForNode(node);
		}

		void DrawConnectionSlotsForNode(VTGraphNode node)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			foreach (VTGraphConnectionSlot slot in node.inputConnections)
			{
				DrawGraphConnectionSlot(slot);
			}
			foreach (VTGraphConnectionSlot slot in node.outputConnections)
			{
				DrawGraphConnectionSlot(slot);
			}
		}

		void DrawGraphConnectionSlot(VTGraphConnectionSlot slot)
		{
			if(slot.slotHidden)
			{
				//Don't draw hidden slots
				return;
			}

			//Draw the connection dot
			var defaultColor = GUI.color;

			GUI.color = Color.gray;

			var slotRect = GetConnectionSlotRenderRect(slot);
			GUI.Box(slotRect, "", EditorStyles.radioButton);

			GUI.color = defaultColor;

			//Draw the connection slot name label
			if(slot.connectionSlotName != "")
			{
				if(slot.connectionSlotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
				{
					var slotLabelPos = slotRect.position + styles.connectionLabelInputOffset;
					var slotLabelRect = new Rect(slotLabelPos, new Vector2(styles.nodeBaseSize.x, slotRect.size.y * 2.0f));
					GUI.Label(slotLabelRect, slot.connectionSlotName);
				}
			}
		}

		void DrawGraphConnectionLine(Vector3 startPosition, Vector3 endPosition, bool inProgressLine, VTGraphConnection optionalConnection = null)
		{
			Vector3 startTangent = startPosition + (Vector3.left * 40f);
			Vector3 endTangent = endPosition - (Vector3.left * 40f);

			Handles.DrawBezier(
				startPosition,
				endPosition,
				startTangent,
				endTangent,
				inProgressLine ? styles.connectionLineDragColor : styles.connectionLineColor,
				null,
				styles.connectionLineWidth
			);

			if(!inProgressLine && optionalConnection != null)
			{
				if (Handles.Button((startPosition + endPosition) * 0.5f, Quaternion.identity, 4, 8, Handles.CircleHandleCap))
				{
					var closureConnection = optionalConnection;
					DeleteNodeConnection(optionalConnection);
				}
			}
		}

		void DrawGraphConnection(VTGraphConnection connection)
		{
			if(connection == null)
			{
				VTLog.LogError("Connection was null in DrawGraphConnection!");
				return;
			}
			if(connection.inputSlot == null)
			{
				VTLog.LogError("InputSlot was null in DrawGraphConnection!");
				return;
			}
			if(connection.outputSlot == null)
			{
				VTLog.LogError("OutputSlot was null in DrawGraphConnection!");
				return;
			}

			var halfConnectionSize = Vector2.one * styles.halfConnectionPointSize;
			var startPosition = GetConnectionSlotPosition(connection.inputSlot) + halfConnectionSize;
			var endPosition = GetConnectionSlotPosition(connection.outputSlot) + halfConnectionSize;
			DrawGraphConnectionLine(startPosition, endPosition, false, connection);
		}

		//NODE UTIL

		VTGraphNode GetTopNodeAtPosition(Vector2 graphPosition)
		{
			if(data.currentGraph == null)
			{
				return null;
			}

			VTGraphNode ret = null;
			foreach (var node in data.currentGraph.nodeList)
			{
				var nodeRect = GetNodeRenderRect(node);
				if (nodeRect.Contains(graphPosition))
				{
					ret = node;
				}
			}

			return ret;
		}

		VTGraphConnectionSlot GetTopConnectionSlotAtPosition(Vector2 graphPosition, bool addExtraClickHeight)
		{
			if(data.currentGraph == null)
			{
				return null;
			}

			float extraClickHeight = 0.0f;
			if(addExtraClickHeight)
			{
				extraClickHeight = styles.connectionPointExtraClickHeight;
			}

			VTGraphConnectionSlot ret = null;
			foreach(var node in data.currentGraph.nodeList)
			{
				foreach(VTGraphConnectionSlot inputSlot in node.inputConnections)
				{
					var slotBounds = GetConnectionSlotRenderRect(inputSlot);
					slotBounds.height += extraClickHeight;
					if(slotBounds.Contains(graphPosition))
					{
						ret = inputSlot;
					}
				}
				foreach(VTGraphConnectionSlot outputSlot in node.outputConnections)
				{
					var slotBounds = GetConnectionSlotRenderRect(outputSlot);
					slotBounds.height += extraClickHeight;
					if (slotBounds.Contains(graphPosition))
					{
						ret = outputSlot;
					}
				}
			}
			return ret;
		}

		Vector2 GetCurrentViewOffset()
		{
			if(data.currentGraph != null)
			{
				return data.currentGraph.displayData.ViewOffset;
			}

			return data.noGraphViewOffset;
		}

		Rect GetNodeRenderRect(VTGraphNode node)
		{
			if(node == null)
			{
				VTLog.LogError("Node was null in GetNodeRenderRect!");
				return new Rect();
			}
			var finalNodePosition = node.NodePosition + GetCurrentViewOffset();
			var nodeSize = styles.nodeBaseSize;
			if(node.IsExpanded)
			{
				nodeSize = styles.nodeExpandedSize;
			}

			var maxSlotCount = Mathf.Max(node.inputConnections.Length, node.outputConnections.Length) - 1;
			if(maxSlotCount > 0)
			{
				//Add extra height for multiple slots
				nodeSize.y += styles.nodeExtraHeightPerSlot * maxSlotCount;
			}

			var nodeRect = new Rect(finalNodePosition, nodeSize);

			return nodeRect;
		}

		Vector2 GetRelativeConnectionSlotPosition(VTGraphConnectionSlot.NodeConnectionSlotType slotType, Rect parentNodeRect, int slotIndex)
		{
			float halfConnectionSize = styles.connectionPointSize * 0.5f;
			if(slotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
			{
				return new Vector2(-halfConnectionSize, styles.connectionPointInitialVertical + (styles.connectionPointVerticalSpacing * slotIndex));
			}

			return new Vector2(parentNodeRect.width - (halfConnectionSize * 1.8f), styles.connectionPointInitialVertical + (styles.connectionPointVerticalSpacing * slotIndex));
		}

		Vector2 GetConnectionSlotPosition(VTGraphConnectionSlot slot)
		{
			var parentNode = slot.parentNode;
			var relativePos = GetRelativeConnectionSlotPosition(slot.connectionSlotType, GetNodeRenderRect(parentNode), slot.indexOnParentNode);
			return GetNodeRenderRect(parentNode).position + relativePos;
		}

		Rect GetConnectionSlotRenderRect(VTGraphConnectionSlot slot)
		{
			var slotPosition = GetConnectionSlotPosition(slot);
			return new Rect(slotPosition, Vector2.one * styles.connectionPointSize);
		}

		//BG

		private void DrawBackgroundColor(Rect graphViewRect)
		{
			if(styles == null)
			{
				VTLog.LogError("Error: Styles was null in DrawBackgroundColor");
				return;
			}

			var defaultColor = GUI.color;

			GUI.color = styles.windowBGColor;
			GUI.Box(graphViewRect, "", styles.windowBgStyle);

			GUI.color = defaultColor;
		}

		private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor, Rect graphViewRect)
		{
			int widthDivs = Mathf.CeilToInt(graphViewRect.width / gridSpacing) + 12;
			int heightDivs = Mathf.CeilToInt(graphViewRect.height / gridSpacing) + 12;

			Handles.BeginGUI();
			Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			var viewOffsetPos = GetCurrentViewOffset();
			Vector3 newOffset = new Vector3(viewOffsetPos.x % gridSpacing, viewOffsetPos.y % gridSpacing, 0);
			Vector3 graphStartOffset = new Vector3(graphViewRect.x, graphViewRect.y, 0);

			for (int i = 0; i < widthDivs; i++)
			{
				Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, graphViewRect.height * 1.5f, 0f) + newOffset);
			}

			for (int j = 0; j < heightDivs; j++)
			{
				Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(graphViewRect.width * 1.5f, gridSpacing * j, 0f) + newOffset);
			}

			Handles.color = Color.white;
			Handles.EndGUI();
		}
	}
}

#endif