#if UNITY_EDITOR

using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTEditorGraphView
	{
		public class GraphViewStyles
		{
			//UI Values
			public readonly float connectionPointSize = 11f;
			public readonly float connectionPointVerticalSpacing = 24f;
			public readonly Vector2 nodeBaseSize = new Vector2(200f, 45f);

			public readonly float grid1Spacing = 20f;
			public readonly float grid2Spacing = 80f;

			//UI Styles

			public GUIStyle windowBgStyle;
			public Color windowBGColor = new Color(0.4f, 0.4f, 0.4f);

			public GUIStyle defaultNodeStyle;
			public GUIStyle selectedNodeStyle;

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

				//Background solid color texture
				windowBgStyle = new GUIStyle(GUI.skin.box);

				//windowBgStyle.normal.background = EditorGUIUtility.Load("transparent") as Texture2D;
			}
		}

		private GraphViewStyles styles;

		private class GraphViewData
		{
			public VTGraph currentGraph;

			public VTGraphNode selectedGraphNode;
			public VTGraphNode draggingNode;
			public VTGraphNode startClickOnNode;

			/// <summary>
			/// When we don't have a currentGraph loaded, we still let the
			/// user move the graph view around and use this to track the offset
			/// </summary>
			public Vector2 noGraphViewOffset = Vector2.zero;
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
			ClearSelectedGraphNode();
		}

		//NODE/GRAPH INTERACTIONS

		void CreateNodeAtPosition<T>(Vector2 position) where T : VTGraphNode, new()
		{
			if(data.currentGraph == null)
			{
				return;
			}
			var positionMinusOffset = position - GetCurrentViewOffset();

			parentWindow.RegisterAssetUndo("Created New Node");
			data.currentGraph.CreateNode<T>(positionMinusOffset);
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
				parentWindow.RegisterAssetUndo("Changed Node Order");
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

			parentWindow.RegisterAssetUndo("Dragged Node");
			data.currentGraph.SetNodePosition(node, node.NodePosition + delta);
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
					if (e.button == 1)
					{
						ProcessContextMenu(e.mousePosition);
					}
					else if (e.button == 0)
					{
						var overNode = GetTopNodeAtPosition(e.mousePosition);
						if(overNode != null)
						{
							data.draggingNode = overNode;
							data.startClickOnNode = overNode;
						}
					}
					/*
					if (e.button == 0)
					{
						ClearConnectionSelection();
					}
					*/
					break;
				case EventType.MouseDrag:
					if ((e.button == 0 && e.alt) || e.button == 2)
					{
						//Left alt + left click or middle click will drag the view
						DragView(e.delta);
						GUI.changed = true;
						return true;
					}
					else if (e.button == 0)
					{
						//Regular left click can drag a node
						if(data.draggingNode != null && data.currentGraph != null)
						{
							SetSelectedGraphNode(data.draggingNode);

							DragNode(data.draggingNode, e.delta);
							GUI.changed = true;
							return true;
						}
					}
					break;
				case EventType.MouseUp:
					if(e.button == 0)
					{
						data.draggingNode = null;
					}
					if (e.button == 0 && !e.alt)
					{
						if (data.currentGraph != null)
						{
							bool selectedANode = false;
							var nodeAtMousePosition = GetTopNodeAtPosition(e.mousePosition);
							if(nodeAtMousePosition != null && data.startClickOnNode == nodeAtMousePosition)
							{
								SetSelectedGraphNode(nodeAtMousePosition);
								selectedANode = true;
								GUI.changed = true;
							}

							if(!selectedANode)
							{
								ClearSelectedGraphNode();
								GUI.changed = true;
							}
						}

						data.startClickOnNode = null;

						/*
						if (data.draggingConnection)
						{
							ClearConnectionSelection();
						}
						*/
					}
					break;
				case EventType.ScrollWheel:
					//OnScroll(e.delta, e.mousePosition);
					break;
				case EventType.KeyDown:

					break;
			}

			return false;
		}

		void ProcessContextMenu(Vector2 mousePosition)
		{
			GenericMenu genericMenu = new GenericMenu();
			if (data.currentGraph != null)
			{
				genericMenu.AddItem(new GUIContent("Add test node"), false, () => CreateNodeAtPosition<VTGraphNodeTest>(mousePosition));
			}
			if (genericMenu.GetItemCount() > 0)
			{
				genericMenu.ShowAsContext();
			}
		}

		void ClearSelectedGraphNode()
		{
			SetSelectedGraphNode(null);
		}

		void SetSelectedGraphNode(VTGraphNode v)
		{
			if(data.selectedGraphNode == v)
			{
				return;
			}

			data.selectedGraphNode = v;

			if(data.selectedGraphNode != null)
			{
				BringNodeToFront(data.selectedGraphNode);
			}
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
			DrawGraphNodes();
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

		void DrawGraphNode(VTGraphNode node)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			var nodeRect = GetNodeRenderRect(node);

			var finalNodeStyle = styles.defaultNodeStyle;
			if(node == data.selectedGraphNode)
			{
				finalNodeStyle = styles.selectedNodeStyle;
			}

			GUI.Box(nodeRect, node.NodeTitle, finalNodeStyle);

			float halfConnectionSize = styles.connectionPointSize * 0.5f;
			Vector2 relativeInputSlotPosition = new Vector2(-halfConnectionSize, 20f);
			Vector2 relativeOutputSlotPosition = new Vector2(nodeRect.width - (halfConnectionSize * 1.8f), 20f);

			foreach (VTGraphConnectionSlot slot in node.inputConnections)
			{
				DrawGraphConnectionSlot(slot, relativeInputSlotPosition, nodeRect.position);
				relativeInputSlotPosition.y -= styles.connectionPointVerticalSpacing;
			}
			foreach (VTGraphConnectionSlot slot in node.outputConnections)
			{
				DrawGraphConnectionSlot(slot, relativeOutputSlotPosition, nodeRect.position);
				relativeOutputSlotPosition.y -= styles.connectionPointVerticalSpacing;
			}
		}

		void DrawGraphConnectionSlot(VTGraphConnectionSlot slot, Vector2 slotPosition, Vector2 finalNodePosition)
		{
			var defaultColor = GUI.color;

			GUI.color = Color.gray;
			var slotDot = new Rect(finalNodePosition + slotPosition, Vector2.one * styles.connectionPointSize);
			GUI.Box(slotDot, "", EditorStyles.radioButton);

			GUI.color = defaultColor;
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

		Rect GetNodeRenderRect(VTGraphNode node)
		{
			var finalNodePosition = node.NodePosition + GetCurrentViewOffset();
			var nodeSize = styles.nodeBaseSize;

			var nodeRect = new Rect(finalNodePosition, nodeSize);

			return nodeRect;
		}

		Vector2 GetCurrentViewOffset()
		{
			if(data.currentGraph != null)
			{
				return data.currentGraph.displayData.ViewOffset;
			}

			return data.noGraphViewOffset;
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
			int widthDivs = Mathf.CeilToInt(graphViewRect.width / gridSpacing) + 6;
			int heightDivs = Mathf.CeilToInt(graphViewRect.height / gridSpacing) + 6;

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