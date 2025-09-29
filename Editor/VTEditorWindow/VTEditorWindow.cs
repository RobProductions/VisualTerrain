
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
			Texture = 1,
		}

		private class VTEditorWindowData
		{
			public VTGraphScreen currentGraphScreen = VTGraphScreen.Heightmap;

			public VTEditorSetupView setupView;
			public VTEditorGraphView graphView;

			public VTSettingsAsset currentAsset = null;
			public bool windowActive = false;
		}

		private VTEditorWindowData data = new VTEditorWindowData();

		private const string storeAssetKey = "RobProductions.VisualTerrain.VTSettingsAsset";

		//LIFECYCLE

		[MenuItem("Window/Visual Terrain/Visual Terrain Editor")]
		private static void OpenWindow()
		{
			VTEditorWindow window = GetWindow<VTEditorWindow>();
			window.titleContent = new GUIContent(windowName);
			window.OnEnable();
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

		private void OnEnable()
		{
			Undo.undoRedoPerformed += UndoPerformed;

			data.setupView = new VTEditorSetupView();
			data.setupView.OnDisable();
			data.graphView = new VTEditorGraphView(this);
			data.graphView.OnEnable();

			//We reloaded or enabled for the first time
			//so check if we stored an asset path and load it into currentAsset
			CheckLoadStoredAsset();
			//Then set it to refresh all associated data
			SetVTSettingsAsset(data.currentAsset);

			data.windowActive = true;
			data.windowActive = true;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoPerformed;

			data.setupView.OnDisable();
			data.graphView.OnDisable();

			data.windowActive = false;
		}

		// Update is called once per frame
		void Update()
		{

		}

		//ASSET MANAGEMENT

		public void ClearVTSettingsAsset()
		{
			SetVTSettingsAsset(null);
		}

		public void SetVTSettingsAsset(VTSettingsAsset newAsset)
		{
			SetStoredAsset(newAsset);
			data.currentAsset = newAsset;
			RefreshGraphScreen();
		}

		void CheckLoadStoredAsset()
		{
			if(EditorPrefs.HasKey(storeAssetKey))
			{
				var retrievedPath = EditorPrefs.GetString(storeAssetKey);
				if(retrievedPath != null && retrievedPath != "")
				{
					data.currentAsset = AssetDatabase.LoadAssetAtPath<VTSettingsAsset>(retrievedPath);
				}
			}
		}

		void SetStoredAsset(VTSettingsAsset storedAsset)
		{
			var finalPath = "";
			if(storedAsset != null)
			{
				finalPath = AssetDatabase.GetAssetPath(storedAsset);
			}
			EditorPrefs.SetString(storeAssetKey, finalPath);
		}

		/// <summary>
		/// Called before editing the scriptableobject so that
		/// the Undo handler can be used if undoing the next change
		/// </summary>
		/// <param name="description"></param>
		public void RegisterAssetUndo(string description)
		{
			if(data.currentAsset == null)
			{
				return;
			}

			Undo.RecordObject(data.currentAsset, description);
		}

		/// <summary>
		/// Called whenever any changes are made to the scriptableobject
		/// including sub data like graphs so that Unity knows to
		/// serialize it when we close the editor and reopen it.
		/// </summary>
		public void EditedAsset()
		{
			if(data.currentAsset == null)
			{
				return;
			}

			EditorUtility.SetDirty(data.currentAsset);
		}

		void UndoPerformed()
		{
			Repaint();
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
			var toolbarHeight = EditorStyles.toolbar.CalcHeight(GUIContent.none, position.width);

			var mainScreenRect = new Rect(0.0f, toolbarHeight, position.width, position.height - toolbarHeight);

			//Draw the graph view underneath the main panel
			var settingsWidth = 80.0f;
			var graphViewRect = new Rect(
				mainScreenRect.x + settingsWidth, mainScreenRect.y, mainScreenRect.width - settingsWidth, mainScreenRect.height);
			data.graphView.DrawGraphView(graphViewRect);

			//Draw the top toolbar
			DrawToolbar();

			//Then draw the setup view if needed
			var setupViewRect = new Rect(mainScreenRect.x, mainScreenRect.y, settingsWidth, mainScreenRect.height);
			data.setupView.DrawSetupView(setupViewRect);

			//Expand window space to bottom
			GUILayout.FlexibleSpace();

			//Draw bottom toolbar/buttons
			DrawBottomContents();

			//Process input
			var currentEvent = Event.current;
			bool graphViewHandledEvent = data.graphView.ProcessEvents(currentEvent);

			//Check for GUI change
			if (GUI.changed)
			{
				if (data.windowActive)
				{
					//SetAssetReference();
					/*
					if(data.currentAsset != null)
					{
						SetVTSettingsAsset(data.currentAsset);
					}
					*/
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
				if(data.currentAsset != null)
				{
					if(GUILayout.Button("Clear Asset", EditorStyles.miniButton))
					{
						ClearVTSettingsAsset();
					}
				}
				GUILayout.FlexibleSpace();
				if(data.currentAsset != null)
				{
					GUILayout.BeginVertical();
					GUILayout.Space(3.2f);
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