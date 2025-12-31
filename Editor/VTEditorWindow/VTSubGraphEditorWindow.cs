



#if UNITY_EDITOR


using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTSubGraphEditorWindow : EditorWindow
	{
		private const string windowName = "VT Sub Graph Editor";

		private class SubGraphEditorWindowStyles
		{
			public GUIStyle propertiesButtonStyle;
			public GUIStyle moreOptionsButtonStyle;

			public GUIContent moreOptionsContent;
			public GUIContent displayPropertiesContent;

			public SubGraphEditorWindowStyles()
			{
				moreOptionsContent = EditorGUIUtility.IconContent("_Menu@2x");
				moreOptionsContent.tooltip = "Show additional window options.";
				displayPropertiesContent = new GUIContent("Properties");
				displayPropertiesContent.tooltip = "Toggle properties panel display.";

				propertiesButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
				moreOptionsButtonStyle = new GUIStyle(EditorStyles.toolbarSearchField);
			}
		}

		private SubGraphEditorWindowStyles styles;

		public enum VTGraphScreen
		{
			Heightmap = 0,
			Texture = 1,
		}

		private class VTSubGraphEditorWindowData
		{
			public VTGraphScreen currentGraphScreen = VTGraphScreen.Heightmap;

			public VTEditorSetupView setupView;
			public VTEditorGraphView graphView;

			public bool displayPropertiesPanel = false;

			public VTSubGraphAsset currentAsset = null;
			public bool windowActive = false;
		}

		private VTSubGraphEditorWindowData data = new VTSubGraphEditorWindowData();

		/*
		private const string storeAssetKey = "RobProductions.VisualTerrain.VTSettingsAsset";
		private const string storeDisplayPropertiesKey = "RobProductions.VisualTerrain.DisplayProperties";
		private const string storeGraphScreenKey = "RobProductions.VisualTerrain.CurrentGraphScreen";
		*/

		//LIFECYCLE

		[MenuItem("Window/Visual Terrain/VT Sub Graph Editor")]
		private static void OpenWindow()
		{
			VTSubGraphEditorWindow window = GetWindow<VTSubGraphEditorWindow>();
			window.titleContent = new GUIContent(windowName);
			window.OnEnable();
		}

		[UnityEditor.Callbacks.OnOpenAsset(1)]
		public static bool OnOpenAsset(int instanceID, int line)
		{
			string assetPath = AssetDatabase.GetAssetPath(instanceID);

			VTSubGraphAsset settingsAssetObject = AssetDatabase.LoadAssetAtPath<VTSubGraphAsset>(assetPath);
			if (settingsAssetObject != null)
			{
				VTSubGraphEditorWindow window = (VTSubGraphEditorWindow)GetWindow(typeof(VTSubGraphEditorWindow));
				window.titleContent = new GUIContent(windowName);
				window.SetVTSettingsAsset(settingsAssetObject);
				window.Show();
				return true;
			}
			//Let Unity open instead
			return false;
		}

		private void OnEnable()
		{
			/*
			Undo.undoRedoPerformed += UndoPerformed;

			data.setupView = new VTEditorSetupView(this);
			data.setupView.OnEnable();
			data.graphView = new VTEditorGraphView(this);
			data.graphView.OnEnable();

			//We reloaded or enabled for the first time
			//so check if we stored an asset path and load it into currentAsset
			CheckLoadStoredAsset();
			//Preload stored EditorWindow data
			CheckStoredDisplayProperties();
			CheckStoredCurrentGraphScreen();
			//Then set the asset to refresh all associated data based on EditorWindow data
			SetVTSettingsAsset(data.currentAsset);

			data.windowActive = true;
			data.windowActive = true;
			*/
		}

		private void OnDisable()
		{
			/*
			Undo.undoRedoPerformed -= UndoPerformed;

			data.setupView.OnDisable();
			data.graphView.OnDisable();

			data.windowActive = false;
			*/
		}


		//ASSET MANAGEMENT

		public VTSubGraphAsset GetCurrentAsset()
		{
			return data.currentAsset;
		}

		public void ClearVTSettingsAsset()
		{
			SetVTSettingsAsset(null);
		}

		public void SetVTSettingsAsset(VTSubGraphAsset newAsset)
		{
			//SetStoredAsset(newAsset);
			data.currentAsset = newAsset;
			//RefreshGraphScreen();

			if (newAsset == null)
			{
				data.displayPropertiesPanel = false;
			}
			Repaint();
		}


	}
}
#endif