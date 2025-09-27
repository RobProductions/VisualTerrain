
#if UNITY_EDITOR

using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTEditorWindow : EditorWindow
	{
		private const string windowName = "Visual Terrain Editor";

		public enum VTGraphScreen
		{
			Heightmap = 0,
			Texture = 2,
		}

		private class VTEditorWindowData
		{
			public VTGraphScreen currentGraphScreen = VTGraphScreen.Heightmap;

			public VTEditorGraphView graphView;

			public VTSettingsAsset currentAsset;
			public bool windowActive = false;
		}

		private VTEditorWindowData data = new VTEditorWindowData();


		[MenuItem("Window/Visual Terrain/Visual Terrain Editor")]
		private static void OpenWindow()
		{
			VTEditorWindow window = GetWindow<VTEditorWindow>();
			window.titleContent = new GUIContent(windowName);
			window.OnEnable();
		}

		private void OnEnable()
		{
			data.graphView = new VTEditorGraphView(this);
			data.graphView.OnEnable();

			//We reloaded or enabled for the first time
			//so set the asset and refresh all
			SetVTSettingsAsset(data.currentAsset);

			data.windowActive = true;
		}

		private void OnDisable()
		{
			data.graphView.OnDisable();

			data.windowActive = false;
		}

		[UnityEditor.Callbacks.OnOpenAsset(1)]
		public static bool OnOpenAsset(int instanceID, int line)
		{
			string assetPath = AssetDatabase.GetAssetPath(instanceID);

			VTSettingsAsset settingsAssetObject = AssetDatabase.LoadAssetAtPath<VTSettingsAsset>(assetPath);
			if (settingsAssetObject != null)
			{
				VTEditorWindow window = (VTEditorWindow)GetWindow(typeof(VTEditorWindow));
				window.titleContent = new GUIContent(windowName);
				window.SetVTSettingsAsset(settingsAssetObject);
				window.Show();
				return true;
			}
			//Let Unity open instead
			return false;
		}

		public void SetVTSettingsAsset(VTSettingsAsset newAsset)
		{
			data.currentAsset = newAsset;
			RefreshGraphScreen();
		}

		// Update is called once per frame
		void Update()
		{

		}

		//SCREENS

		void RefreshGraphScreen()
		{
			VTGraph finalDisplayGraph = null;

			if(data.currentAsset != null)
			{
				if (data.currentGraphScreen == VTGraphScreen.Heightmap)
				{
					finalDisplayGraph = data.currentAsset.generationData.heightmapGraph;
				}
				
			}

			data.graphView.SetTargetGraph(finalDisplayGraph);
		}

		//RENDERING

		private void OnGUI()
		{
			//First, draw the graph view underneath everything
			data.graphView.DrawGraphView();

			//Draw the top and bottom toolbars
			DrawToolbar();
			GUILayout.FlexibleSpace();
			DrawBottomContents();

			//Process input
			data.graphView.ProcessEvents(Event.current);

			//Check for GUI change
			if (GUI.changed)
			{
				if (data.windowActive)
				{
					//SetAssetReference();
					if(data.currentAsset != null)
					{
						SetVTSettingsAsset(data.currentAsset);
					}
				}
				Repaint();
			}

			/*
			DrawNodes();
			DrawConnections();

			DrawConnectionLine(Event.current);

			//Reset scale to draw non-scaled elements
			//GUI.matrix = oldMatrix;

			*/
		}

		private void DrawToolbar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				GUILayout.FlexibleSpace();
				if(data.currentAsset != null)
				{
					GUILayout.BeginVertical();
					GUILayout.Space(3.5f);
					if (GUILayout.Toggle(false, "Auto", EditorStyles.toggle))
					{

					}
					GUILayout.EndVertical();
					if (GUILayout.Button("Generate", EditorStyles.miniButton))
					{

					}
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
				if (data.currentAsset)
				{
					assetName = data.currentAsset.name;
				}
				GUILayout.Label(assetName, EditorStyles.miniButton);
			}
			GUILayout.EndHorizontal();
		}
	}
}

#endif