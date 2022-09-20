using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RobProductions.VisualTerrain.Runtime;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Modules;
#endif

namespace RobProductions.VisualTerrain.Editor
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

		private const string windowName = "Visual Terrain Editor";


		private VTGeneratorNodeAttachPoint selectedInPoint;
		private VTGeneratorNodeAttachPoint selectedOutPoint;

		private List<VTGeneratorWindowNode> nodeList = new List<VTGeneratorWindowNode>();
		private List<VTGeneratorNodeConnection> connections = new List<VTGeneratorNodeConnection>();

		private VTGeneratorAsset asset;
		private bool windowActive = false;


		[MenuItem("Window/Visual Terrain/Terrain Generator Editor")]
		private static void OpenWindow()
		{
			VTGeneratorWindow window = GetWindow<VTGeneratorWindow>();
			window.titleContent = new GUIContent(windowName);
		}

		private void OnEnable()
		{
			styles = new GeneratorWindowStyles();
			if(asset != null)
			{
				LoadAssetReference(asset);
			}
			windowActive = true;
		}

		private void OnDisable()
		{
			windowActive = false;
		}

		[UnityEditor.Callbacks.OnOpenAsset(1)]
		public static bool OnOpenAsset(int instanceID, int line)
		{
			string assetPath = AssetDatabase.GetAssetPath(instanceID);
			
			VTGeneratorAsset scriptableObject = AssetDatabase.LoadAssetAtPath<VTGeneratorAsset>(assetPath);
			if (scriptableObject != null)
			{
				VTGeneratorWindow window = (VTGeneratorWindow)GetWindow(typeof(VTGeneratorWindow));
				window.titleContent = new GUIContent(windowName);
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

		//EDITOR REFERENCE

		void ClearWindowReference()
		{
			nodeList.Clear();
			connections.Clear();
			selectedInPoint = null;
			selectedOutPoint = null;
			data.draggingConnection = false;

			data.viewOffsetPos = Vector2.zero;
			data.userInputDrag = Vector2.zero;
		}

		void LoadAssetReference(VTGeneratorAsset v)
		{
			if(v != null)
			{
				data.viewOffsetPos = v.viewportData.offsetPos;

				for (int i = 0; i < v.nodeData.nodeReferences.Count; i++)
				{
					var thisNode = v.nodeData.nodeReferences[i];
					var newNode = AddNode(Vector2.zero);
					newNode.SetNodeReference(thisNode);
				}
			}
		}

		void SetAssetReference()
		{
			if(asset != null)
			{
				asset.nodeData.nodeReferences.Clear();
				for(int i = 0; i < nodeList.Count; i++)
				{
					asset.nodeData.nodeReferences.Add(nodeList[i].GetNodeReference());
				}

				asset.viewportData.offsetPos = data.viewOffsetPos;
			}
		}

		//RENDERING

		private void OnGUI()
		{
			DrawBackgroundColor();
			DrawGrid(20, 0.1f, Color.black);
			DrawGrid(80, 0.25f, Color.black);

			//Scale the contents inside the BG
			/*
			var oldMatrix = GUI.matrix;
			var pivotPoint = new Vector2(Screen.width / 2, Screen.height / 2);
			EditorGUIUtility.ScaleAroundPivot(new Vector2(data.zoomLevel, data.zoomLevel), pivotPoint);
			*/

			DrawNodes();
			DrawConnections();

			DrawConnectionLine(Event.current);

			//Reset scale to draw non-scaled elements
			//GUI.matrix = oldMatrix;

			DrawToolbar();
			GUILayout.FlexibleSpace();
			DrawBottomContents();

			ProcessNodeEvents(Event.current);
			ProcessEvents(Event.current);

			if (GUI.changed)
			{
				if(windowActive)
				{
					SetAssetReference();
				}
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
					if (e.button == 0 && e.alt)
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
				case EventType.ScrollWheel:
					//OnScroll(e.delta, e.mousePosition);
					break;
			}
		}

		private void ProcessContextMenu(Vector2 mousePosition)
		{
			GenericMenu genericMenu = new GenericMenu();
			if(asset != null)
			{
				genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition));
			}
			if(genericMenu.GetItemCount() > 0)
			{
				genericMenu.ShowAsContext();
			}
		}

		//USER INPUT

		private void OnDrag(Vector2 delta)
		{
			data.userInputDrag = delta;

			for (int i = 0; i < nodeList.Count; i++)
			{
				nodeList[i].Drag(delta);
			}

			GUI.changed = true;
		}

		/*
		private void OnScroll(Vector2 scrollVector, Vector2 zoomCenterPoint)
		{
			float zoomAmt = -.05f * scrollVector.y;
			data.zoomLevel += zoomAmt;
			data.zoomLevel = Mathf.Clamp(data.zoomLevel, 0.5f, 1.8f);

			for (int i = 0; i < nodeList.Count; i++)
			{
				nodeList[i].Zoom(data.zoomLevel, zoomAmt, zoomCenterPoint);
			}

			GUI.changed = true;
		}
		*/

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
			//newNode.Zoom(data.zoomLevel, 0.0f, Vector2.zero);
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
					if (node.data.inputPoints.Contains(connections[i].inPoint) || node.data.outputPoints.Contains(connections[i].outPoint))
					{
						connectionsToRemove.Add(connections[i]);
					}
				}

				for (int i = 0; i < connectionsToRemove.Count; i++)
				{
					if(connections.Contains(connectionsToRemove[i]))
					{
						connections.Remove(connectionsToRemove[i]);
					}
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