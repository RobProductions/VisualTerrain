using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Modules;

namespace RobProductions.VisualTerrain
{
	public class VTGeneratorWindow : EditorWindow
	{
		public class GeneratorWindowData
		{
			public Vector2 viewOffsetPos = Vector2.zero;
			public Vector2 userInputDrag = Vector2.zero;
			public float zoomLevel = 1.0f;

			public bool draggingConnection = false;

			[Header("User Data")]
			public bool useDragAttachPoint = true;
		}

		public GeneratorWindowData data = new GeneratorWindowData();

		public class GeneratorWindowStyles
		{
			public GUIStyle windowBgStyle;

			public GUIStyle defaultNodeStyle;
			public GUIStyle selectedNodeStyle;

			public GeneratorWindowStyles()
			{
				defaultNodeStyle = new GUIStyle();
				//defaultNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
				defaultNodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
				defaultNodeStyle.border = new RectOffset(10, 10, 10, 10);

				selectedNodeStyle = new GUIStyle();
				//selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
				selectedNodeStyle.normal.background = EditorGUIUtility.Load("node0 on") as Texture2D;
				selectedNodeStyle.border = new RectOffset(10, 10, 10, 10);

				windowBgStyle = new GUIStyle();
				//windowBgStyle.normal.background = EditorGUIUtility.Load("darkviewbackground") as Texture2D;
				
				var bgTex = new Texture2D(1, 1);
				bgTex.name = "VTBackgroundTex";
				bgTex.SetPixel(0, 0, new Color(.3f, .3f, .3f, .8f));
				bgTex.wrapMode = TextureWrapMode.Repeat;
				bgTex.Apply();
				windowBgStyle.normal.background = bgTex;
				
			}
		}

		private GeneratorWindowStyles styles;


		private VTGeneratorNodeAttachPoint selectedInPoint;
		private VTGeneratorNodeAttachPoint selectedOutPoint;

		private List<VTGeneratorWindowNode> nodeList = new List<VTGeneratorWindowNode>();
		private List<VTGeneratorNodeConnection> connections = new List<VTGeneratorNodeConnection>();

		private VTGeneratorAsset asset;


		[MenuItem("Window/Visual Terrain/Terrain Generator Editor")]
		private static void OpenWindow()
		{
			VTGeneratorWindow window = GetWindow<VTGeneratorWindow>();
			window.titleContent = new GUIContent("Visual Terrain Editor");
		}

		private void OnEnable()
		{
			styles = new GeneratorWindowStyles();
		}

		[UnityEditor.Callbacks.OnOpenAsset(1)]
		public static bool OnOpenAsset(int instanceID, int line)
		{
			string assetPath = AssetDatabase.GetAssetPath(instanceID);
			
			VTGeneratorAsset scriptableObject = AssetDatabase.LoadAssetAtPath<VTGeneratorAsset>(assetPath);
			if (scriptableObject != null)
			{
				VTGeneratorWindow window = (VTGeneratorWindow)GetWindow(typeof(VTGeneratorWindow));
				window.SetGeneratorAsset(scriptableObject);
				window.Show();
				return true;
			}
			//Let Unity open instead
			return false;
		}

		public void SetGeneratorAsset(VTGeneratorAsset v)
		{
			asset = v;
			ClearWindowReference();
			LoadAssetReference(v);
		}

		void ClearWindowReference()
		{
			nodeList.Clear();
			connections.Clear();
			selectedInPoint = null;
			selectedOutPoint = null;
			data.draggingConnection = false;
		}

		void LoadAssetReference(VTGeneratorAsset v)
		{
			if(v != null)
			{
				for (int i = 0; i < v.nodeData.nodeReferences.Count; i++)
				{
					var thisNode = v.nodeData.nodeReferences[i];
					var newNode = AddNode(Vector2.zero);
					newNode.SetNodeReference(thisNode);
				}
			}
		}

		//RENDERING

		private void OnGUI()
		{
			DrawBackgroundColor();
			DrawGrid(20, 0.1f, Color.black);
			DrawGrid(80, 0.25f, Color.black);

			DrawNodes();
			DrawConnections();

			DrawConnectionLine(Event.current);

			DrawToolbar();
			GUILayout.FlexibleSpace();
			DrawBottomContents();

			ProcessNodeEvents(Event.current);
			ProcessEvents(Event.current);

			if (GUI.changed)
			{
				Repaint();
			}
		}

		private void DrawNodes()
		{
			if(nodeList != null)
			{
				for (int i = 0; i < nodeList.Count; i++)
				{
					var thisNode = nodeList[i];
					thisNode.Draw();
				}
			}
		}

		private void DrawConnections()
		{
			if (connections != null)
			{
				for (int i = 0; i < connections.Count; i++)
				{
					connections[i].Draw();
				}
			}
		}

		private void DrawConnectionLine(Event e)
		{
			if (selectedInPoint != null && selectedOutPoint == null)
			{
				Handles.DrawBezier(
					selectedInPoint.rect.center,
					e.mousePosition,
					selectedInPoint.rect.center + Vector2.left * 50f,
					e.mousePosition - Vector2.left * 50f,
					Color.white,
					null,
					2f
				);

				GUI.changed = true;
			}

			if (selectedOutPoint != null && selectedInPoint == null)
			{
				Handles.DrawBezier(
					selectedOutPoint.rect.center,
					e.mousePosition,
					selectedOutPoint.rect.center - Vector2.left * 50f,
					e.mousePosition + Vector2.left * 50f,
					Color.white,
					null,
					2f
				);

				GUI.changed = true;
			}
		}

		private void DrawToolbar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Focus view", EditorStyles.miniButton))
				{

				}

			}
			GUILayout.EndHorizontal();
		}

		private void DrawBottomContents()
		{
			GUILayout.BeginHorizontal(EditorStyles.label);
			{
				GUILayout.FlexibleSpace();
				string assetName = "No Asset";
				if(asset)
				{
					assetName = asset.name;
				}
				GUILayout.Label(assetName, EditorStyles.miniButton);
			}
			GUILayout.EndHorizontal();
		}

		//EVENTS

		private void ProcessNodeEvents(Event e)
		{
			if (nodeList != null)
			{
				for (int i = nodeList.Count - 1; i >= 0; i--)
				{
					bool guiChanged = nodeList[i].ProcessEvents(e);

					if (guiChanged)
					{
						GUI.changed = true;
					}
				}
			}
		}

		private void ProcessEvents(Event e)
		{
			data.userInputDrag = Vector2.zero;

			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 0)
					{
						ClearConnectionSelection();
					}
					if (e.button == 1)
					{
						ProcessContextMenu(e.mousePosition);
					}
					break;
				case EventType.MouseDrag:
					if (e.button == 0)
					{
						OnDrag(e.delta);
					}
					break;
				case EventType.MouseUp:
					if (e.button == 0)
					{
						if(data.draggingConnection)
						{
							ClearConnectionSelection();
						}
					}
					break;
			}
		}

		private void ProcessContextMenu(Vector2 mousePosition)
		{
			GenericMenu genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition));
			genericMenu.ShowAsContext();
		}

		//USER INPUT

		private void OnDrag(Vector2 delta)
		{
			data.userInputDrag = delta;

			if (nodeList != null)
			{
				for (int i = 0; i < nodeList.Count; i++)
				{
					nodeList[i].Drag(delta);
				}
			}

			GUI.changed = true;
		}

		private void OnClickAddNode(Vector2 mousePosition)
		{
			if (nodeList == null)
			{
				nodeList = new List<VTGeneratorWindowNode>();
			}

			AddNode(mousePosition);
		}

		private VTGeneratorWindowNode AddNode(Vector2 pos)
		{
			var newNode = new VTGeneratorWindowNode(pos, 200, 50, styles.defaultNodeStyle, styles.selectedNodeStyle, OnClickRemoveNode,
				this);
			newNode.SetupAttachPoints(1, 1, OnClickInPoint, OnClickOutPoint);
			nodeList.Add(newNode);

			return newNode;
		}

		private void OnClickInPoint(VTGeneratorNodeAttachPoint inPoint)
		{
			selectedInPoint = inPoint;

			if (selectedOutPoint != null)
			{
				if (selectedOutPoint.parentNode != selectedInPoint.parentNode)
				{
					CreateConnection();
					ClearConnectionSelection();
				}
				else
				{
					ClearConnectionSelection();
				}
			}
		}

		private void OnClickOutPoint(VTGeneratorNodeAttachPoint outPoint)
		{
			selectedOutPoint = outPoint;

			if (selectedInPoint != null)
			{
				if (selectedOutPoint.parentNode != selectedInPoint.parentNode)
				{
					CreateConnection();
					ClearConnectionSelection();
				}
				else
				{
					ClearConnectionSelection();
				}
			}
		}

		public void DragConnectionPoint(VTGeneratorNodeAttachPoint startPoint)
		{
			data.draggingConnection = true;
			if(startPoint.attachType == VTGeneratorNodeAttachPoint.AttachPointType.Input)
			{
				selectedInPoint = startPoint;
				selectedOutPoint = null;
			}
			else
			{
				selectedOutPoint = startPoint;
				selectedInPoint = null;
			}
		}

		public void AttemptCompleteDragConnection(VTGeneratorNodeAttachPoint endPoint)
		{
			if(selectedInPoint == null)
			{
				var start = selectedOutPoint;
				if (start != endPoint && start.parentNode != endPoint.parentNode
					&& start.attachType != endPoint.attachType)
				{
					CreateConnection(endPoint, start);
				}
			}
			else if (selectedOutPoint == null)
			{
				var start = selectedInPoint;
				if (start != endPoint && start.parentNode != endPoint.parentNode
					&& start.attachType != endPoint.attachType)
				{
					CreateConnection(start, endPoint);
				}
			}
			ClearConnectionSelection();
		}

		private void OnClickRemoveNode(VTGeneratorWindowNode node)
		{
			if (connections != null)
			{
				List<VTGeneratorNodeConnection> connectionsToRemove = new List<VTGeneratorNodeConnection>();

				for (int i = 0; i < connections.Count; i++)
				{
					if (node.inputPoints.Contains(connections[i].inPoint) || node.outputPoints.Contains(connections[i].outPoint))
					{
						connectionsToRemove.Add(connections[i]);
					}
				}

				for (int i = 0; i < connectionsToRemove.Count; i++)
				{
					connections.Remove(connectionsToRemove[i]);
				}
			}

			nodeList.Remove(node);
		}

		private void OnClickRemoveConnection(VTGeneratorNodeConnection connection)
		{
			RemoveConnection(connection);
		}

		//CONNECTION HANDLING

		private void RemoveConnection(VTGeneratorNodeConnection connection)
		{
			connection.inPoint.SetHasConnection(false);
			connection.outPoint.SetHasConnection(false);
			connections.Remove(connection);
		}

		private void CreateConnection()
		{
			CreateConnection(selectedInPoint, selectedOutPoint);
		}

		private void CreateConnection(VTGeneratorNodeAttachPoint inPoint, VTGeneratorNodeAttachPoint outPoint)
		{
			if (connections == null)
			{
				connections = new List<VTGeneratorNodeConnection>();
			}

			data.draggingConnection = false;

			var newConnection = new VTGeneratorNodeConnection(inPoint, outPoint, OnClickRemoveConnection);
			inPoint.SetHasConnection(true);
			outPoint.SetHasConnection(true);
			connections.Add(newConnection);
		}

		private void ClearConnectionSelection()
		{
			selectedInPoint = null;
			selectedOutPoint = null;
			data.draggingConnection = false;
		}

		//BG

		private void DrawBackgroundColor()
		{
			var windowRect = new Rect(0.0f, 0.0f, maxSize.x, maxSize.y);
			GUI.Label(windowRect, "", styles.windowBgStyle);
		}

		private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
		{
			int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
			int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

			Handles.BeginGUI();
			Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			data.viewOffsetPos += data.userInputDrag * 0.5f;
			Vector3 newOffset = new Vector3(data.viewOffsetPos.x % gridSpacing, data.viewOffsetPos.y % gridSpacing, 0);

			for (int i = 0; i < widthDivs; i++)
			{
				Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
			}

			for (int j = 0; j < heightDivs; j++)
			{
				Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
			}

			Handles.color = Color.white;
			Handles.EndGUI();
		}
	}
}

#endif