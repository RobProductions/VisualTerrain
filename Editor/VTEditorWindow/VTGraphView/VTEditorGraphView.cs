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
		public class GraphViewStyles
		{
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
			if(data.currentGraph != null)
			{

			}

			data.currentGraph = newGraph;


		}

		//EVENTS

		public void ProcessEvents(Event e)
		{
			//data.userInputDrag = Vector2.zero;

			switch (e.type)
			{
				case EventType.MouseDown:
					
					/*
					if (e.button == 0)
					{
						ClearConnectionSelection();
					}
					if (e.button == 1)
					{
						ProcessContextMenu(e.mousePosition);
					}
					*/
					break;
				case EventType.MouseDrag:
					if (e.button == 0 && e.alt)
					{
						OnDrag(e.delta);
					}
					break;
				case EventType.MouseUp:
					if (e.button == 0 && !e.alt)
					{
						if (data.currentGraph != null)
						{
							bool selectedANode = false;
							foreach (var node in data.currentGraph.nodeList)
							{
								var nodeRect = GetNodeRenderRect(node);
								if(nodeRect.Contains(e.mousePosition))
								{
									SetSelectedGraphNode(node);
									selectedANode = true;
									GUI.changed = true;
								}
							}

							if(!selectedANode)
							{
								SetSelectedGraphNode(null);
								GUI.changed = true;
							}
						}

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
			}
		}

		void SetSelectedGraphNode(VTGraphNode v)
		{
			data.selectedGraphNode = v;
		}

		void OnDrag(Vector2 delta)
		{
			if(data.currentGraph == null)
			{
				return;
			}

			data.currentGraph.displayData.ViewOffset += delta;

			GUI.changed = true;

		}

		//RENDERING

		public void DrawGraphView()
		{
			//First created needed GUIStyles
			CreateGUIStyles();

			//Then draw background grid
			DrawBackgroundColor();
			DrawGrid(20, 0.1f, Color.black);
			DrawGrid(80, 0.25f, Color.black);

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

			Vector2 relativeInputSlotPosition = new Vector2(-8, 20f);
			Vector2 relativeOutputSlotPosition = new Vector2(nodeRect.width - 8f, 20f);

			foreach (VTGraphConnectionSlot slot in node.inputConnections)
			{
				DrawGraphConnectionSlot(slot, relativeInputSlotPosition, nodeRect.position);
				relativeInputSlotPosition.y -= 25f;
			}
			foreach (VTGraphConnectionSlot slot in node.outputConnections)
			{
				DrawGraphConnectionSlot(slot, relativeOutputSlotPosition, nodeRect.position);
				relativeOutputSlotPosition.y -= 25f;
			}
		}

		void DrawGraphConnectionSlot(VTGraphConnectionSlot slot, Vector2 slotPosition, Vector2 finalNodePosition)
		{
			var defaultColor = GUI.color;

			GUI.color = Color.gray;
			var slotDot = new Rect(finalNodePosition + slotPosition, Vector2.one * 11f);
			GUI.Box(slotDot, "", EditorStyles.radioButton);

			GUI.color = defaultColor;
		}

		//NODE UTIL

		Rect GetNodeRenderRect(VTGraphNode node)
		{
			var finalNodePosition = node.NodePosition + new Vector2(data.currentGraph.displayData.ViewOffset.x, data.currentGraph.displayData.ViewOffset.y);
			var nodeRect = new Rect(finalNodePosition, new Vector2(200, 50));

			return nodeRect;
		}

		//BG

		private void DrawBackgroundColor()
		{
			if(styles == null)
			{
				VTLog.LogError("Error: Styles was null in DrawBackgroundColor");
				return;
			}

			var defaultColor = GUI.color;

			var windowRect = new Rect(0.0f, 0.0f, parentWindow.maxSize.x, parentWindow.maxSize.y);
			GUI.color = styles.windowBGColor;
			GUI.Box(windowRect, "", styles.windowBgStyle);

			GUI.color = defaultColor;
		}

		private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
		{
			int widthDivs = Mathf.CeilToInt(parentWindow.position.width / gridSpacing);
			int heightDivs = Mathf.CeilToInt(parentWindow.position.height / gridSpacing);

			Handles.BeginGUI();
			Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			var viewOffsetPos = Vector2.zero;
			if(data.currentGraph != null)
			{
				viewOffsetPos = data.currentGraph.displayData.ViewOffset;
			}
			Vector3 newOffset = new Vector3(viewOffsetPos.x % gridSpacing, viewOffsetPos.y % gridSpacing, 0);

			for (int i = 0; i < widthDivs; i++)
			{
				Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, parentWindow.position.height, 0f) + newOffset);
			}

			for (int j = 0; j < heightDivs; j++)
			{
				Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(parentWindow.position.width, gridSpacing * j, 0f) + newOffset);
			}

			Handles.color = Color.white;
			Handles.EndGUI();
		}
	}
}

#endif