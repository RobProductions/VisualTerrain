#if UNITY_EDITOR

using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTEditorGraphView
	{
		/// <summary>
		/// The amount of view scaling that can happen
		/// before the value gets clamped.
		/// </summary>
		private readonly Vector2 viewScaleRange = new Vector2(0.2f, 1.4f);
		private readonly float stopSmallGridAtScale = 0.4f;
		private readonly float viewScaleButtonChangeAmount = 0.05f;
		private readonly float viewScaleScrollChangeAmount = 0.02f;

		public class GraphViewStyles
		{
			//UI Values
			public readonly Vector2 nodeBaseSize = new Vector2(200f, 50f);
			public readonly Vector2 nodeExpandedSize = new Vector2(200f, 130f);
			public readonly float nodeExtraHeightPerSlot = 30f;
			public readonly float nodePreviewImageHeight = 80f;
			public readonly float nodePreviewImagePadding = 8f;
			public readonly float nodeTitleOffset = 4.5f;

			public readonly float connectionPointInitialVertical = 24f;
			public readonly float connectionPointVerticalSpacing = 24f;
			public readonly float connectionPointSize = 14f;
			public readonly float halfConnectionPointSize = 7f;
			public readonly float connectionLineWidth = 3f;
			public readonly float connectionPointExtraClickHeight = 5f;
			public readonly Vector2 connectionLabelInputOffset = new Vector2(15f, -6.5f);

			public readonly float grid1Spacing = 20f;
			public readonly float grid2Spacing = 80f;
			public readonly float gridLineWidth = 0f;

			//UI Colors
			public readonly Color connectionLineColor = new Color(0.8f, 0.85f, 0.92f);
			public readonly Color connectionLineDragColor = new Color(0.7f, 0.72f, 1.0f);
			public readonly Color windowBGColor = new Color(0.4f, 0.4f, 0.4f);
			public readonly Color nodeLabelColor = new Color(1.0f, 1.0f, 1.0f, 0.4f);
			public readonly Color disableButtonBGEnabledColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
			public readonly Color disableButtonBGDisabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

			//UI Styles

			public GUIStyle windowBgStyle;

			public GUIStyle defaultNodeStyle;
			public GUIStyle selectedNodeStyle;
			public GUIStyle nodeTitleStyle;
			public GUIStyle nodeTextStyle;

			public Texture2D nodeDisabledTexture;
			public Texture2D nodeEnabledTexture;

			public Texture2D nodeUpdateTexture;

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

				//Scalable text style
				nodeTextStyle = new GUIStyle(GUI.skin.label);

				//Background solid color texture
				windowBgStyle = new GUIStyle(GUI.skin.box);

				//windowBgStyle.normal.background = EditorGUIUtility.Load("transparent") as Texture2D;

				//Disable button
				nodeEnabledTexture = EditorGUIUtility.Load("scenevis_visible_hover@2x") as Texture2D;
				nodeDisabledTexture = EditorGUIUtility.Load("scenevis_hidden_hover@2x") as Texture2D;

				//Update buttons
				nodeUpdateTexture = EditorGUIUtility.Load("RotateTool@2x") as Texture2D;
			}
		}

		private GraphViewStyles styles;

		private class GraphViewData
		{
			public VTGraph currentGraph;
			public Rect currentRenderRect = new Rect();

			public List<VTGraphNode> selectedGraphNodes = new List<VTGraphNode>();
			public VTGraphNode draggingNode;
			public VTGraphNode startClickOnNode;
			public VTGraphConnectionSlot draggingConnectionSlot;
			public bool draggedNodeOnMousePress = false;
			public bool draggedViewOnMousePress = false;

			/// <summary>
			/// When we don't have a currentGraph loaded, we still let the
			/// user move the graph view around and use this to track the offset.
			/// </summary>
			public Vector2 noGraphViewOffset = Vector2.zero;
			/// <summary>
			/// When we don't have a currentGraph loaded, we still let the
			/// user scale the graph view and track it with this.
			/// </summary>
			public float noGraphViewScale = 1.0f;

			public Vector2 draggingConnectionMousePosition = Vector2.zero;

			public Dictionary<VTGraphNode, Texture2D> nodePreviewImageMap = new Dictionary<VTGraphNode, Texture2D>();

			public VTGraphProcessingSettings nodeThumbProcessingSettings = new VTGraphProcessingSettings(
				textureGenResolutionNumber: 256,
				thumbnailMode: true
			);
		}

		private GraphViewData data = new GraphViewData();

		private readonly Vector2 defaultDuplicateOffsetValue = new Vector2(60.0f, 80.0f);

		private VTEditorMainPanel mainPanel;

		public VTEditorGraphView(VTEditorMainPanel mainPanel)
		{
			this.mainPanel = mainPanel;
		}

		//LIFECYCLE

		public void OnEnable()
		{
			if(data.currentGraph != null)
			{
				data.currentGraph.OnGraphEnable();
			}
		}

		public void OnDisable()
		{
			if(data.currentGraph != null)
			{
				data.currentGraph.OnGraphDisable();
			}
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
				//Clear old graph
				data.currentGraph.OnGraphDisable();
			}

			data.currentGraph = newGraph;

			if(data.currentGraph != null)
			{
				//Enable lifecycle on new graph
				data.currentGraph.OnGraphEnable();
			}

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

			var nodesAsArray = data.currentGraph.nodeList.ToArray();
			VTEditorCoroutine.Start(CoroutineRegeneratePreviewImages(nodesAsArray));

			if(markEditAsset)
			{
				mainPanel.EditedAsset();
			}
		}

		IEnumerator CoroutineRegeneratePreviewImages(VTGraphNode[] nodeList)
		{
			foreach (VTGraphNode node in nodeList)
			{
				if(node != null)
				{
					TryUpdateNodePreviewImage(node);
					GUI.changed = true;
					//Wait a frame before continuing so that the preview update is time spliced
					yield return null;
				}
			}

			yield return null;
		}

		public void TryUpdateNodePreviewImage(VTGraphNode node)
		{
			if(node == null)
			{
				return;
			}
			var currentAsset = mainPanel.GetMainSettingsAsset();
			if(currentAsset != null)
			{
				//TODO: Maybe update processing settings based on user setting?

				//data.previewThumbProcessingSettings.textureGenResolutionNumber =
			}
			data.nodeThumbProcessingSettings.contextAsset = currentAsset;

			//Clear the preview to make a new one
			RemoveNodePreviewImage(node);

			var output = node.GetOutputConnection();
			if (output != null)
			{
				//If we have an output node, process it and get the output texture
				data.currentGraph.ProcessNode(node, data.nodeThumbProcessingSettings);
				var gridValue = output.GetRangeGridValue();
				if(!gridValue.IsNullOrEmpty())
				{
					//We have a valid RangeGrid, so convert it to Texture
					data.nodePreviewImageMap[node] = gridValue.ToGrayscale();

					if(node is VTGraphNodeHeightOutput && currentAsset != null)
					{
						currentAsset.SetCachedThumbnailHeightmapGrid(gridValue);
						mainPanel.EditedAsset();
					}
				}
				else
				{
					//We just have a float value,
					//so create a 1x1 preview image representing that value as a color
					data.nodePreviewImageMap[node] = VTImageProcessingUtils.GenerateBlankTextureWithValue(1, output.GetFloatValue());
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

		public void UpdateSceneDependentNodePreviews()
		{
			if(data.currentGraph == null)
			{
				return;
			}

			foreach(VTGraphNode thisNode in data.currentGraph.nodeList)
			{
				if(thisNode.SceneDependent)
				{
					TryUpdateOutputConnectedPreviews(thisNode);
				}
			}
		}

		public void RemoveNodePreviewImage(VTGraphNode node)
		{
			if (data.nodePreviewImageMap.ContainsKey(node))
			{
				data.nodePreviewImageMap.Remove(node);
			}
		}

		//NODE/GRAPH INTERACTIONS

		/// <summary>
		/// Duplicate all nodes in the list and move them individually
		/// by the offset value. Note that this does not yet
		/// copy node properties... that's tough.
		/// </summary>
		/// <param name="nodeList"></param>
		/// <param name="offsetPosition"></param>
		void DuplicateNodesWithOffset(List<VTGraphNode> nodeList, Vector2 offsetPosition)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			mainPanel.RegisterAssetDataUndo("Duplicated Nodes");
			foreach(VTGraphNode thisNode in nodeList)
			{
				//For each node, create a new one of the same type
				var newNodePosition = thisNode.NodePosition + offsetPosition;

				data.currentGraph.CreateNode(thisNode.GetType(), newNodePosition);
			}
			RegenerateAllNodePreviewImages(true);
			mainPanel.EditedAsset();
		}

		/// <summary>
		/// Create a node at the given position and mark
		/// the asset as edited with undo data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="position"></param>
		VTGraphNode CreateNodeAtPosition<T>(Vector2 position) where T : VTGraphNode, new()
		{
			if(data.currentGraph == null)
			{
				return null;
			}
			var positionMinusOffset = (position - GetCurrentViewOffset()) / GetCurrentViewScale();

			mainPanel.RegisterAssetDataUndo("Created New Node");
			var newNode = data.currentGraph.CreateNode<T>(positionMinusOffset);
			TryUpdateNodePreviewImage(newNode);
			mainPanel.EditedAsset();

			return newNode;
		}

		/// <summary>
		/// Delete the given nodes from the graph and mark
		/// the asset as edited with undo data.
		/// </summary>
		/// <param name="nodeList"></param>
		void DeleteNodes(List<VTGraphNode> nodeList)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			mainPanel.RegisterAssetStructureUndo("Deleted Nodes");

			List<VTGraphNode> affectedNodes = new List<VTGraphNode>();

			foreach(VTGraphNode node in nodeList)
			{
				//Get a list of all nodes affected if this one is deleted
				var allConnectedNodes = data.currentGraph.GetOutputConnectedNodes(node);
				foreach(VTGraphNode connectedNode in allConnectedNodes)
				{
					if(affectedNodes.Contains(connectedNode))
					{
						continue;
					}
					affectedNodes.Add(connectedNode);
				}
				//Remove the node
				RemoveNodePreviewImage(node);
				data.currentGraph.RemoveNode(node);
			}

			//Now iterate through all affected nodes and update their islands
			foreach(VTGraphNode affectedNode in affectedNodes)
			{
				TryUpdateOutputConnectedPreviews(affectedNode);
			}

			mainPanel.EditedAsset();
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
				TryUpdateOutputConnectedPreviews(node);
			}
			mainPanel.EditedAsset();
		}

		void CreateNodeConnection(VTGraphConnectionSlot slot1, VTGraphConnectionSlot slot2)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			mainPanel.RegisterAssetStructureUndo("Connected Nodes");
			data.currentGraph.AddNodeConnection(slot1, slot2);

			//Update preview of both nodes
			TryUpdateOutputConnectedPreviews(slot1.parentNode);
			TryUpdateOutputConnectedPreviews(slot2.parentNode);

			mainPanel.EditedAsset();
		}

		void DeleteNodeConnection(VTGraphConnection connection)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			mainPanel.RegisterAssetStructureUndo("Disconnected Nodes");
			data.currentGraph.RemoveNodeConnection(connection);

			//Update preview of the input node and its output
			TryUpdateOutputConnectedPreviews(connection.inputSlot.parentNode);

			mainPanel.EditedAsset();
		}

		void SetNodesExpanded(List<VTGraphNode> nodes, bool setExpanded)
		{
			if (data.currentGraph == null)
			{
				return;
			}

			mainPanel.RegisterAssetStructureUndo("Set Nodes Expanded");
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
			mainPanel.EditedAsset();
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
				mainPanel.RegisterAssetStructureUndo("Changed Node Order");
				data.currentGraph.SetNodeIndex(node, lastIndex);
				mainPanel.EditedAsset();
			}
		}

		void DragNode(VTGraphNode node, Vector2 delta)
		{
			if (data.currentGraph == null)
			{
				return;
			}

			mainPanel.RegisterAssetDataUndo("Dragged Node");
			data.currentGraph.SetNodePosition(node, node.NodePosition + (delta / GetCurrentViewScale()));
			mainPanel.EditedAsset();
		}

		void DragNodes(List<VTGraphNode> draggingNodes, Vector2 delta)
		{
			if (data.currentGraph == null)
			{
				return;
			}

			mainPanel.RegisterAssetDataUndo("Dragged Nodes");
			foreach(VTGraphNode node in draggingNodes)
			{
				data.currentGraph.SetNodePosition(node, node.NodePosition + (delta / GetCurrentViewScale()));
			}
			mainPanel.EditedAsset();
		}

		void DragView(Vector2 delta)
		{
			if (data.currentGraph != null)
			{
				//No need to register undo for view change
				data.currentGraph.displayData.ViewOffset += (delta / GetCurrentViewScale());
				mainPanel.EditedAsset();
			}
			else
			{
				data.noGraphViewOffset += (delta / GetCurrentViewScale());
			}
		}

		void ScaleView(float amount, Vector2 zoomCenterPoint)
		{
			if (data.currentGraph != null)
			{
				SetViewScale(data.currentGraph.displayData.ViewScale + amount, zoomCenterPoint);
			}
			else
			{
				SetViewScale(data.noGraphViewScale + amount, zoomCenterPoint);
			}
		}

		void SetViewScale(float setValue, Vector2 zoomCenterPoint)
		{
			if (data.currentGraph != null)
			{
				//No need to register undo for view change
				var oldScale = data.currentGraph.displayData.ViewScale;
				data.currentGraph.displayData.ViewScale = setValue;
				data.currentGraph.displayData.ViewScale = Mathf.Clamp(data.currentGraph.displayData.ViewScale, viewScaleRange.x, viewScaleRange.y);

				var adjustFactor = (zoomCenterPoint - data.currentGraph.displayData.ViewOffset) * (data.currentGraph.displayData.ViewScale / oldScale);
				data.currentGraph.displayData.ViewOffset = zoomCenterPoint - adjustFactor;
				mainPanel.EditedAsset();
			}
			else
			{
				data.noGraphViewScale = setValue;
				data.noGraphViewScale = Mathf.Clamp(data.noGraphViewScale, viewScaleRange.x, viewScaleRange.y);
			}
		}

		void FocusView(List<VTGraphNode> optionalFocusNodes)
		{
			if(data.currentGraph != null)
			{
				//No need to register undo for view change
				var accumulatedViewOffset = Vector2.zero;

				var parentWindowCenter = new Vector2(mainPanel.data.storedGuiWindowRect.width * 0.5f, mainPanel.data.storedGuiWindowRect.height * 0.5f) - (styles.nodeBaseSize * 0.5f);
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
				mainPanel.EditedAsset();
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
					data.draggedViewOnMousePress = false;
					break;
				case EventType.ScrollWheel:
					if(Mathf.Abs(e.delta.y) > 1f)
					{
						ScaleView(-e.delta.y * viewScaleScrollChangeAmount, GetOffsetZoomCenterPoint());
						GUI.changed = true;
					}
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
					else if (e.keyCode == KeyCode.Equals)
					{
						ScaleView(viewScaleButtonChangeAmount, GetOffsetZoomCenterPoint());
						GUI.changed = true;
					}
					else if (e.keyCode == KeyCode.Minus)
					{
						ScaleView(-viewScaleButtonChangeAmount, GetOffsetZoomCenterPoint());
						GUI.changed = true;
					}
					else if (e.keyCode == KeyCode.Alpha0)
					{
						SetViewScale(1.0f, GetOffsetZoomCenterPoint());
						GUI.changed = true;
					}
					else if (e.keyCode == KeyCode.D)
					{
						if(e.control || e.command)
						{
							//Duplicate selected nodes
							if(data.selectedGraphNodes.Count > 0)
							{
								DuplicateNodesWithOffset(data.selectedGraphNodes, defaultDuplicateOffsetValue);
								GUI.changed = true;
							}
						}
					}
					break;
				case EventType.DragUpdated:
				case EventType.DragPerform:
					//Check for drag and drop types to show correct visual and handle drop
					bool isPerform = e.type == EventType.DragPerform;
					bool doShowAvailableDrop = false;
					var draggedObjects = DragAndDrop.objectReferences;
					var mousePosition = e.mousePosition;

					if (draggedObjects.Length == 1 && data.currentRenderRect.Contains(mousePosition) && data.currentGraph != null)
					{
						//We can only handle 1 drag and drop object for now
						var thisDragObject = draggedObjects[0];
						if (thisDragObject is TerrainLayer && data.currentGraph.graphType == VTGraph.GraphType.Texture)
						{
							doShowAvailableDrop = true;

							if(isPerform)
							{
								var newSplatNode = CreateNodeAtPosition<VTGraphNodeSplatLayerOutput>(mousePosition);
								if(newSplatNode != null)
								{
									newSplatNode.InvokeBeginEditNodeProperty();
									(newSplatNode as VTGraphNodeSplatLayerOutput).terrainLayer = thisDragObject as TerrainLayer;
									newSplatNode.InvokeEndEditNodeProperty();
								}
							}
						}
						else if (thisDragObject is GameObject && data.currentGraph.graphType == VTGraph.GraphType.TerrainObject)
						{
							doShowAvailableDrop = true;

							if (isPerform)
							{
								var newTreeNode = CreateNodeAtPosition<VTGraphNodeTreeLayerOutput>(mousePosition);
								if (newTreeNode != null)
								{
									newTreeNode.InvokeBeginEditNodeProperty();
									(newTreeNode as VTGraphNodeTreeLayerOutput).treePrototype = thisDragObject as GameObject;
									newTreeNode.InvokeEndEditNodeProperty();
								}
							}
						}
						else if (thisDragObject is VTSubGraphAsset && data.currentGraph.graphType != VTGraph.GraphType.SubGraph)
						{
							doShowAvailableDrop = true;

							if(isPerform)
							{
								var newSubGraphNode = CreateNodeAtPosition<VTGraphNodeSubGraph>(mousePosition);
								if (newSubGraphNode != null)
								{
									newSubGraphNode.InvokeBeginEditNodeProperty();
									(newSubGraphNode as VTGraphNodeSubGraph).subGraphAsset = thisDragObject as VTSubGraphAsset;
									newSubGraphNode.InvokeEndEditNodeProperty();

									(newSubGraphNode as VTGraphNodeSubGraph).RefreshConnections();

									TryUpdateNodePreviewImage(newSubGraphNode);
								}
							}
						}
						else if (thisDragObject is Texture2D)
						{
							doShowAvailableDrop = true;

							if (isPerform)
							{
								var newTextureNode = CreateNodeAtPosition<VTGraphNodeTexture>(mousePosition);
								if (newTextureNode != null)
								{
									newTextureNode.InvokeBeginEditNodeProperty();
									(newTextureNode as VTGraphNodeTexture).inputTexture = thisDragObject as Texture2D;
									newTextureNode.InvokeEndEditNodeProperty();

									TryUpdateNodePreviewImage(newTextureNode);
								}
							}
						}
					}

					DragAndDrop.visualMode = doShowAvailableDrop ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
					return true;
			}

			return false;
		}

		void ProcessContextMenu(Vector2 mousePosition, VTGraphNode overNode)
		{
			GenericMenu genericMenu = new GenericMenu();
			if (data.currentGraph != null)
			{
				GenericMenuAddNodeCreationItems(genericMenu, mousePosition);

				genericMenu.AddSeparator("");
				if(overNode != null)
				{
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
				else
				{
					genericMenu.AddItem(new GUIContent("Update All Previews"), false, () => RegenerateAllNodePreviewImages(true));
				}
			}
			if (genericMenu.GetItemCount() > 0)
			{
				genericMenu.ShowAsContext();
			}
		}

		void GenericMenuAddNodeCreationItems(GenericMenu menu, Vector2 mousePosition)
		{
			//Test
			/*
			menu.AddItem(new GUIContent("Add Test Node/Test Node"), false, () => CreateNodeAtPosition<VTGraphNodeTest>(mousePosition));
			*/

			//Input
			menu.AddItem(new GUIContent("Add Input Node/Simple Value"), false, () => CreateNodeAtPosition<VTGraphNodeSimpleValue>(mousePosition));
			menu.AddItem(new GUIContent("Add Input Node/Simple Shape"), false, () => CreateNodeAtPosition<VTGraphNodeSimpleShape>(mousePosition));
			menu.AddItem(new GUIContent("Add Input Node/Group Shape"), false, () => CreateNodeAtPosition<VTGraphNodeGroupShape>(mousePosition));
			menu.AddItem(new GUIContent("Add Input Node/Simple Noise"), false, () => CreateNodeAtPosition<VTGraphNodeSimpleNoise>(mousePosition));
			menu.AddItem(new GUIContent("Add Input Node/Octave Noise"), false, () => CreateNodeAtPosition<VTGraphNodeOctaveNoise>(mousePosition));
			menu.AddItem(new GUIContent("Add Input Node/Texture"), false, () => CreateNodeAtPosition<VTGraphNodeTexture>(mousePosition));
			if(data.currentGraph.graphType != VTGraph.GraphType.Height && data.currentGraph.graphType != VTGraph.GraphType.SubGraph)
			{
				menu.AddItem(new GUIContent("Add Input Node/Sample Heightmap"), false, () => CreateNodeAtPosition<VTGraphNodeSampleHeight>(mousePosition));
			}
			if(data.currentGraph.graphType == VTGraph.GraphType.TerrainObject || data.currentGraph.graphType == VTGraph.GraphType.CustomObject)
			{
				menu.AddItem(new GUIContent("Add Input Node/Sample Splatmap"), false, () => CreateNodeAtPosition<VTGraphNodeSampleSplatmap>(mousePosition));
			}
			if (data.currentGraph.graphType == VTGraph.GraphType.SubGraph)
			{
				menu.AddItem(new GUIContent("Add Input Node/Sub Graph Input"), false, () => CreateNodeAtPosition<VTGraphNodeSGInValue>(mousePosition));
			}

			//Math
			menu.AddItem(new GUIContent("Add Math Node/Arithmetic"), false, () => CreateNodeAtPosition<VTGraphNodeArithmetic>(mousePosition));
			menu.AddItem(new GUIContent("Add Math Node/Overlay"), false, () => CreateNodeAtPosition<VTGraphNodeOverlay>(mousePosition));
			menu.AddItem(new GUIContent("Add Math Node/Remap"), false, () => CreateNodeAtPosition<VTGraphNodeRemap>(mousePosition));
			menu.AddItem(new GUIContent("Add Math Node/Translation"), false, () => CreateNodeAtPosition<VTGraphNodeTranslation>(mousePosition));

			//Mask
			menu.AddItem(new GUIContent("Add Mask Node/Combine Mask"), false, () => CreateNodeAtPosition<VTGraphNodeCombineMask>(mousePosition));
			menu.AddItem(new GUIContent("Add Mask Node/Angle Mask"), false, () => CreateNodeAtPosition<VTGraphNodeAngleMask>(mousePosition));
			menu.AddItem(new GUIContent("Add Mask Node/Range Mask"), false, () => CreateNodeAtPosition<VTGraphNodeRangeMask>(mousePosition));

			//Filter
			menu.AddItem(new GUIContent("Add Filter Node/Box Blur"), false, () => CreateNodeAtPosition<VTGraphNodeBlur>(mousePosition));
			menu.AddItem(new GUIContent("Add Filter Node/Step"), false, () => CreateNodeAtPosition<VTGraphNodeStep>(mousePosition));
			menu.AddItem(new GUIContent("Add Filter Node/Sharpen"), false, () => CreateNodeAtPosition<VTGraphNodeSharpen>(mousePosition));

			//Processing
			menu.AddItem(new GUIContent("Add Processing Node/Erosion"), false, () => CreateNodeAtPosition<VTGraphNodeErosion>(mousePosition));
			if (data.currentGraph.graphType != VTGraph.GraphType.SubGraph)
			{
				menu.AddItem(new GUIContent("Add Processing Node/Sub Graph"), false, () => CreateNodeAtPosition<VTGraphNodeSubGraph>(mousePosition));
			}

			//Output
			if(data.currentGraph.graphType == VTGraph.GraphType.Height)
			{
				menu.AddItem(new GUIContent("Add Output Node/Height Output"), false, () => CreateNodeAtPosition<VTGraphNodeHeightOutput>(mousePosition));
			}
			if(data.currentGraph.graphType == VTGraph.GraphType.Texture)
			{
				menu.AddItem(new GUIContent("Add Output Node/Splat Layer Output"), false, () => CreateNodeAtPosition<VTGraphNodeSplatLayerOutput>(mousePosition));
			}
			if(data.currentGraph.graphType == VTGraph.GraphType.TerrainObject)
			{
				menu.AddItem(new GUIContent("Add Output Node/Tree Layer Output"), false, () => CreateNodeAtPosition<VTGraphNodeTreeLayerOutput>(mousePosition));
			}
			if(data.currentGraph.graphType == VTGraph.GraphType.SubGraph)
			{
				menu.AddItem(new GUIContent("Add Output Node/Sub Graph Output"), false, () => CreateNodeAtPosition<VTGraphNodeSGOutValue>(mousePosition));
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

		public void DrawGraphView(Rect guiWindowRect, Rect graphViewRect)
		{
			//Cache the render rect for later
			data.currentRenderRect = graphViewRect;

			//Create needed GUIStyles
			CreateGUIStyles();
			//Update text size for current scale
			var viewScale = GetCurrentViewScale();
			styles.nodeTextStyle.fontSize = Mathf.RoundToInt(11f * viewScale);
			styles.nodeTitleStyle.fontSize = Mathf.RoundToInt(12f * viewScale);

			//Then draw background grid
			DrawBackgroundColor(graphViewRect);
			float largeGridOpacity = 0.15f;
			if(GetCurrentViewScale() >= stopSmallGridAtScale)
			{
				largeGridOpacity = 0.25f;
				DrawGrid(styles.grid1Spacing, 0.1f, Color.black, styles.gridLineWidth, guiWindowRect, graphViewRect);
			}
			DrawGrid(styles.grid2Spacing, largeGridOpacity, Color.black, styles.gridLineWidth, guiWindowRect, graphViewRect);

			//Draw nodes
			DrawGraphConnections();
			DrawGraphNodes();
			if(data.draggingConnectionSlot != null)
			{
				var halfConnectionSize = Vector2.one * GetCurrentHalfConnectionPointSize();
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
			var viewScale = GetCurrentViewScale();

			var finalNodeStyle = styles.defaultNodeStyle;
			if(data.selectedGraphNodes.Contains(node))
			{
				finalNodeStyle = styles.selectedNodeStyle;
			}

			//Render the node box
			GUI.Box(nodeRect, "", finalNodeStyle);

			//Render label
			var titleBGRect = new Rect(nodeRect.position + (new Vector2(5f, 4f) * viewScale), new Vector2(nodeRect.size.x - (10f * viewScale), 16f * viewScale));

			var bgColor = GUI.backgroundColor;
			GUI.backgroundColor = styles.nodeLabelColor;
			GUI.Box(titleBGRect, "", GUI.skin.box);
			GUI.backgroundColor = bgColor;

			var titleRect = new Rect(nodeRect.position, nodeRect.size);
			float scaledOffset = styles.nodeTitleOffset;

			if (viewScale < 0.4f)
			{
				scaledOffset = styles.nodeTitleOffset * 1.9f;
			}
			else if (viewScale < 0.6f)
			{
				scaledOffset = styles.nodeTitleOffset * 1.7f;
			}
			else if(viewScale < 0.8f)
			{
				scaledOffset = styles.nodeTitleOffset * 1.6f;
			}
			else if(viewScale < 1.0f)
			{
				scaledOffset = styles.nodeTitleOffset * 1.2f;
			}
			titleRect.position -= new Vector2(0.0f, scaledOffset);

			string finalTitle = node.NodeTitle;
			if(node.CustomName != "")
			{
				finalTitle = node.CustomName;
			}
			GUI.Label(titleRect, finalTitle, styles.nodeTitleStyle);

			//Draw disable button
			if(node.HasDisableButton)
			{
				var content = new GUIContent(node.IsDisabled ? styles.nodeDisabledTexture : styles.nodeEnabledTexture, "Toggle node disabled.");

				var disableButtonDefaultBGColor = GUI.backgroundColor;
				GUI.backgroundColor = node.IsDisabled ? styles.disableButtonBGDisabledColor : styles.disableButtonBGEnabledColor;

				var disableButtonRect = GetNodeTopButtonButtonRenderRect(nodeRect);
				//GUI.DrawTexture(disableButtonRect, node.IsDisabled ? styles.nodeDisabledTexture : styles.nodeEnabledTexture);
				if(GUI.Button(disableButtonRect, content))
				{
					mainPanel.RegisterAssetStructureUndo("Toggled Node Disabled");
					node.IsDisabled = !node.IsDisabled;
					mainPanel.EditedAsset();
				}

				GUI.backgroundColor = disableButtonDefaultBGColor;
			}
			//Draw refresh button for nodes affected by scene objects
			if(node.SceneDependent)
			{
				var content = new GUIContent(styles.nodeUpdateTexture, "Update node preview image to reflect scene changes.");

				var disableButtonDefaultBGColor = GUI.backgroundColor;
				GUI.backgroundColor = styles.disableButtonBGEnabledColor;

				var sceneUpdateRect = GetNodeTopButtonButtonRenderRect(nodeRect);
				if(GUI.Button(sceneUpdateRect, content))
				{
					TryUpdateOutputConnectedPreviews(node);
				}

				GUI.backgroundColor = disableButtonDefaultBGColor;
			}

			//Draw output value preview

			if(node.IsExpanded)
			{
				if(data.nodePreviewImageMap.ContainsKey(node))
				{
					if (data.nodePreviewImageMap[node] != null)
					{
						float scaledPreviewHeight = (styles.nodePreviewImageHeight * viewScale);
						var textureRelativePos = new Vector2(styles.nodePreviewImagePadding * viewScale, nodeRect.size.y - scaledPreviewHeight - (styles.nodePreviewImagePadding * viewScale));
						var textureSize = new Vector2(nodeRect.size.x - (styles.nodePreviewImagePadding * viewScale * 2.0f), scaledPreviewHeight);

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
			float viewScale = GetCurrentViewScale();

			//Draw the connection dot
			var defaultColor = GUI.color;

			GUI.color = Color.gray;

			var slotRect = GetConnectionSlotRenderRect(slot);
			GUI.Box(slotRect, "", GUI.skin.button);

			GUI.color = defaultColor;

			//Draw the connection slot name label
			if(slot.connectionSlotName != "")
			{
				if(slot.connectionSlotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
				{
					var slotLabelPos = slotRect.position + (styles.connectionLabelInputOffset * viewScale);
					var slotLabelRect = new Rect(slotLabelPos, new Vector2(styles.nodeBaseSize.x, slotRect.size.y * 2.0f));
					GUI.Label(slotLabelRect, slot.connectionSlotName, styles.nodeTextStyle);
				}
			}
		}

		void DrawGraphConnectionLine(Vector3 startPosition, Vector3 endPosition, bool inProgressLine, VTGraphConnection optionalConnection = null)
		{
			var viewScale = GetCurrentViewScale();
			Vector3 startTangent = startPosition + ((Vector3.left * 40f) * viewScale);
			Vector3 endTangent = endPosition - ((Vector3.left * 40f) * viewScale);

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
				Vector3 removeButtonPosition = (startPosition + endPosition) * 0.5f;
				if (Handles.Button(removeButtonPosition, Quaternion.identity, 4, 8, Handles.CircleHandleCap))
				{
					if(data.currentRenderRect.Contains(removeButtonPosition))
					{
						//Make sure we're not blocked by setup view
						var closureConnection = optionalConnection;
						DeleteNodeConnection(optionalConnection);
					}
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

			var halfConnectionSize = Vector2.one * GetCurrentHalfConnectionPointSize();
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

		/// <summary>
		/// Get the current view offset.
		/// Note: this can't be used for math of objects in the graph
		/// because their positions must be added before calculating scale.
		/// </summary>
		/// <returns></returns>
		Vector2 GetCurrentViewOffset()
		{
			if(data.currentGraph != null)
			{
				//The relative screen position is the stored position * stored scale
				return data.currentGraph.displayData.ViewOffset * data.currentGraph.displayData.ViewScale;
			}

			return data.noGraphViewOffset * data.noGraphViewScale;
		}

		/// <summary>
		/// Transform a relative pos into screen pos with view scaling.
		/// </summary>
		/// <param name="relativePos"></param>
		/// <returns></returns>
		Vector2 GetRelativeViewOffset(Vector2 relativePos)
		{
			if (data.currentGraph != null)
			{
				//The relative screen position is the stored position * stored scale
				return (data.currentGraph.displayData.ViewOffset + relativePos) * data.currentGraph.displayData.ViewScale;
			}

			return (data.noGraphViewOffset + relativePos) * data.noGraphViewScale;
		}

		/// <summary>
		/// Get the current view scaling multiplier for rendering.
		/// </summary>
		/// <returns></returns>
		float GetCurrentViewScale()
		{
			if(data.currentGraph != null)
			{
				data.currentGraph.displayData.ViewScale = Mathf.Clamp(data.currentGraph.displayData.ViewScale, viewScaleRange.x, viewScaleRange.y);
				return data.currentGraph.displayData.ViewScale;
			}

			data.noGraphViewScale = Mathf.Clamp(data.noGraphViewScale, viewScaleRange.x, viewScaleRange.y);
			return data.noGraphViewScale;
		}

		/// <summary>
		/// Returns a ViewOffset in the center of the screen
		/// before scaling so that the zoom operation can
		/// center on it.
		/// </summary>
		/// <returns></returns>
		Vector2 GetOffsetZoomCenterPoint()
		{
			var halfScreenCenter = (data.currentRenderRect.size / GetCurrentViewScale()) * 0.5f;
			if(data.currentGraph != null)
			{
				return data.currentGraph.displayData.ViewOffset + halfScreenCenter;
			}

			return data.noGraphViewOffset + halfScreenCenter;
		}

		Rect GetNodeRenderRect(VTGraphNode node)
		{
			if(node == null)
			{
				VTLog.LogError("Node was null in GetNodeRenderRect!");
				return new Rect();
			}
			var finalNodePosition = GetRelativeViewOffset(node.NodePosition);
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
			var finalNodeSize = nodeSize * GetCurrentViewScale();

			var nodeRect = new Rect(finalNodePosition, finalNodeSize);

			return nodeRect;
		}

		Rect GetNodeTopButtonButtonRenderRect(Rect nodeRect)
		{
			var viewScale = GetCurrentViewScale();

			return new Rect(
				nodeRect.position.x + nodeRect.width * 0.7f,
				nodeRect.position.y + (21 * viewScale),
				nodeRect.width * 0.272f,
				(18 * viewScale)
			);
		}

		float GetCurrentConnectionPointSize()
		{
			return styles.connectionPointSize * GetCurrentViewScale();
		}

		float GetCurrentHalfConnectionPointSize()
		{
			return styles.halfConnectionPointSize * GetCurrentViewScale();
		}

		Vector2 GetRelativeConnectionSlotPosition(VTGraphConnectionSlot.NodeConnectionSlotType slotType, Rect parentNodeRect, int slotIndex)
		{
			float halfConnectionSize = GetCurrentConnectionPointSize() * 0.5f;
			float yOffset = (styles.connectionPointInitialVertical + (styles.connectionPointVerticalSpacing * slotIndex)) * GetCurrentViewScale();
			if (slotType == VTGraphConnectionSlot.NodeConnectionSlotType.Input)
			{
				return new Vector2(-halfConnectionSize, yOffset);
			}

			return new Vector2(parentNodeRect.width - (halfConnectionSize * 1.2f), yOffset);
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
			return new Rect(slotPosition, Vector2.one * GetCurrentConnectionPointSize());
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

		private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor, float lineWidth, Rect guiWindowRect, Rect graphViewRect)
		{
			Handles.BeginGUI();
			Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			var viewScaleAmount = GetCurrentViewScale();
			float finalSpacingAmount = gridSpacing * viewScaleAmount;

			//Number of divisions is based on width/height of parent window
			//since we're drawing outside of the graphview bounds (underneath setup view)
			float extraDivsMultiplier = 1.1f;
			int widthDivs = Mathf.CeilToInt((guiWindowRect.width / finalSpacingAmount) * extraDivsMultiplier);
			int heightDivs = Mathf.CeilToInt((guiWindowRect.height / finalSpacingAmount) * extraDivsMultiplier);

			var viewOffsetPos = GetCurrentViewOffset();
			float gridStartOffsetX = viewOffsetPos.x % finalSpacingAmount;
			float gridStartOffsetY = viewOffsetPos.y % finalSpacingAmount;

			float extraLengthMultiplier = 1.1f;

			for (int i = 0; i < widthDivs; i++)
			{
				//Draw lines on every finalSpacingAmount from top to bottom of the rect
				float xPos = gridStartOffsetX + (finalSpacingAmount * i);
				var startPos = new Vector3(xPos, 0f, 0f);
				var endPos = new Vector3(xPos, guiWindowRect.height * extraLengthMultiplier, 0f);

				Handles.DrawLine(startPos, endPos, lineWidth);
			}

			for (int j = 0; j < heightDivs; j++)
			{
				//Draw lines on every finalSpacingAmount from left to right of the rect
				float yPos = gridStartOffsetY + (finalSpacingAmount * j);
				var startPos = new Vector3(0, yPos, 0);
				var endPos = new Vector3(guiWindowRect.width * extraLengthMultiplier, yPos, 0f);

				Handles.DrawLine(startPos, endPos, lineWidth);
			}

			Handles.color = Color.white;
			Handles.EndGUI();
		}
	}
}

#endif