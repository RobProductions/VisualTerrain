using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace RobProductions.VisualTerrain
{
	public class VTGeneratorWindow : EditorWindow
	{
		public class GeneratorWindowData
		{
			public Vector2 offset = Vector2.zero;
			public Vector2 drag = Vector2.zero;
		}

		private GeneratorWindowData data = new GeneratorWindowData();

		public class GeneratorWindowStyles
		{
			public GUIStyle windowBgStyle;
		}

		private GeneratorWindowStyles styles = new GeneratorWindowStyles(); 

		private List<VTGeneratorWindowNode> nodeList = new List<VTGeneratorWindowNode>();

		private GUIStyle nodeStyle;

		[MenuItem("Window/Visual Terrain/Terrain Generator Editor")]
		private static void OpenWindow()
		{
			VTGeneratorWindow window = GetWindow<VTGeneratorWindow>();
			window.titleContent = new GUIContent("Visual Terrain Editor");
		}

		private void OnEnable()
		{
			nodeStyle = new GUIStyle();
			nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
			nodeStyle.border = new RectOffset(12, 12, 12, 12);

			styles.windowBgStyle = new GUIStyle();
			var bgTex = new Texture2D(1, 1);
			bgTex.SetPixel(0, 0, new Color(.3f, .3f, .3f, .8f));
			bgTex.wrapMode = TextureWrapMode.Repeat;
			bgTex.Apply();
			styles.windowBgStyle.normal.background = bgTex;

		}

		private void OnGUI()
		{
			DrawBackgroundColor();
			DrawGrid(20, 0.1f, Color.black);
			DrawGrid(80, 0.25f, Color.black);

			DrawNodes();

			ProcessNodeEvents(Event.current);
			ProcessEvents(Event.current);

			if (GUI.changed) Repaint();
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

		private void ProcessEvents(Event e)
		{
			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 1)
					{
						ProcessContextMenu(e.mousePosition);
					}
					break;
			}
		}

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

		private void ProcessContextMenu(Vector2 mousePosition)
		{
			GenericMenu genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition));
			genericMenu.ShowAsContext();
		}

		private void OnClickAddNode(Vector2 mousePosition)
		{
			if (nodeList == null)
			{
				nodeList = new List<VTGeneratorWindowNode>();
			}

			nodeList.Add(new VTGeneratorWindowNode(mousePosition, 200, 50, nodeStyle));
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

			data.offset += data.drag * 0.5f;
			Vector3 newOffset = new Vector3(data.offset.x % gridSpacing, data.offset.y % gridSpacing, 0);

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