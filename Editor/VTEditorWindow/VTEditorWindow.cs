
#if UNITY_EDITOR

using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
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

		public VTGraphScreen currentGraphScreen = VTGraphScreen.Heightmap;

		VTEditorGraphView graphView;

		private VTSettingsAsset asset;
		private bool windowActive = false;

		[MenuItem("Window/Visual Terrain/Visual Terrain Editor")]
		private static void OpenWindow()
		{
			VTEditorWindow window = GetWindow<VTEditorWindow>();
			window.titleContent = new GUIContent(windowName);
			window.OnEnable();
		}

		private void OnEnable()
		{
			graphView = new VTEditorGraphView(this);
			graphView.OnEnable();

			//We reloaded or enabled for the first time
			//so set the asset and refresh all
			SetVTSettingsAsset(asset);

			windowActive = true;
		}

		private void OnDisable()
		{
			graphView.OnDisable();

			windowActive = false;
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
			asset = newAsset;
			//ClearWindowReference();
			//LoadAssetReference(v);
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

			if(asset != null)
			{
				if (currentGraphScreen == VTGraphScreen.Heightmap)
				{
					finalDisplayGraph = asset.generationData.heightmapGraph;
				}
			}

			graphView.SetTargetGraph(finalDisplayGraph);
		}

		//RENDERING

		private void OnGUI()
		{
			//First, draw the graph view underneath everything
			graphView.DrawGraphView();

			//Draw the top and bottom toolbars
			DrawToolbar();
			GUILayout.FlexibleSpace();
			DrawBottomContents();

			//Process input
			graphView.ProcessEvents(Event.current);

			//Check for GUI change
			if (GUI.changed)
			{
				if (windowActive)
				{
					//SetAssetReference();
					if(asset != null)
					{
						SetVTSettingsAsset(asset);
					}
				}
				Repaint();
			}

			/*
			DrawBackgroundColor();
			DrawGrid(20, 0.1f, Color.black);
			DrawGrid(80, 0.25f, Color.black);

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
				if (windowActive)
				{
					SetAssetReference();
				}
				Repaint();
			}
			*/
		}

		private void DrawToolbar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				GUILayout.FlexibleSpace();
				if(asset != null)
				{
					if (GUILayout.Toggle(false, "Auto", EditorStyles.toggle))
					{

					}
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
				if (asset)
				{
					assetName = asset.name;
				}
				GUILayout.Label(assetName, EditorStyles.miniButton);
			}
			GUILayout.EndHorizontal();
		}
	}
}

#endif