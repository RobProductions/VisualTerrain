
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

		VTEditorGraphView graphView;

		private VTSettingsAsset asset;
		private bool windowActive = false;

		[MenuItem("Window/Visual Terrain/Visual Terrain Editor")]
		private static void OpenWindow()
		{
			VTEditorWindow window = GetWindow<VTEditorWindow>();
			window.titleContent = new GUIContent(windowName);
		}

		private void OnEnable()
		{
			graphView = new VTEditorGraphView(this);
			graphView.OnEnable();

			if (asset != null)
			{
				//LoadAssetReference(asset);
			}
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
				window.SetGeneratorAsset(settingsAssetObject);
				window.Show();
				return true;
			}
			//Let Unity open instead
			return false;
		}

		public void SetGeneratorAsset(VTSettingsAsset newAsset)
		{
			asset = newAsset;
			//ClearWindowReference();
			//LoadAssetReference(v);
		}

		// Update is called once per frame
		void Update()
		{

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

			//Check for GUI change
			if (GUI.changed)
			{
				if (windowActive)
				{
					//SetAssetReference();
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