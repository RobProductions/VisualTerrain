#if UNITY_EDITOR

using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RobProductions.VisualTerrain.Editor
{
    public class VTEditorGraphView
    {
		public class GraphViewStyles
		{
			public GUIStyle windowBgStyle;

			public GUIStyle defaultNodeStyle;
			public GUIStyle selectedNodeStyle;

			public GraphViewStyles()
			{
				defaultNodeStyle = new GUIStyle();
				//defaultNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
				defaultNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
				defaultNodeStyle.border = new RectOffset(10, 10, 10, 10);

				selectedNodeStyle = new GUIStyle();
				//selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
				selectedNodeStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
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

		private GraphViewStyles styles = new GraphViewStyles();

		private class GraphViewData
		{
			public VTGraph currentGraph;
		}

		private GraphViewData data = new GraphViewData();

		private VTEditorWindow parentWindow;

		public VTEditorGraphView(VTEditorWindow parentWindow)
		{
			this.parentWindow = parentWindow;
		}

		// Start is called before the first frame update
		void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

		//LIFECYCLE

		public void OnEnable()
		{

		}

		public void OnDisable()
		{

		}

		//GRAPH

		public void SetTargetGraph(VTGraph newGraph)
		{

		}

		//RENDERING

		public void DrawGraphView()
		{
			DrawBackgroundColor();
			DrawGrid(20, 0.1f, Color.black);
			DrawGrid(80, 0.25f, Color.black);
		}

		//BG

		private void DrawBackgroundColor()
		{
			var windowRect = new Rect(0.0f, 0.0f, parentWindow.maxSize.x, parentWindow.maxSize.y);
			GUI.Label(windowRect, "", styles.windowBgStyle);
		}

		private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
		{
			int widthDivs = Mathf.CeilToInt(parentWindow.position.width / gridSpacing);
			int heightDivs = Mathf.CeilToInt(parentWindow.position.height / gridSpacing);

			Handles.BeginGUI();
			Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			//data.viewOffsetPos += data.userInputDrag * 0.5f;
			//Vector3 newOffset = new Vector3(data.viewOffsetPos.x % gridSpacing, data.viewOffsetPos.y % gridSpacing, 0);
			Vector3 newOffset = Vector3.zero;

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