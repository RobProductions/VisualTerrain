
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

		private class EditorWindowStyles
		{
			public GUIStyle propertiesButtonStyle;
			public GUIStyle moreOptionsButtonStyle;
			public GUIStyle previewButtonStyle;

			public GUIContent moreOptionsContent;
			public GUIContent displayPropertiesContent;
			public GUIContent previewToggleContent;

			public EditorWindowStyles()
			{
				moreOptionsContent = EditorGUIUtility.IconContent("_Menu@2x");
				moreOptionsContent.tooltip = "Show additional window options.";
				displayPropertiesContent = new GUIContent("Properties");
				displayPropertiesContent.tooltip = "Toggle properties panel display.";
				previewToggleContent = EditorGUIUtility.IconContent("ViewToolOrbit@2x");
				previewToggleContent.tooltip = "Toggle preview mode.";

				propertiesButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
				moreOptionsButtonStyle = new GUIStyle(EditorStyles.toolbarSearchField);
				previewButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
				previewButtonStyle.padding = new RectOffset(8, 8, 2, 2);
			}
		}

		private EditorWindowStyles styles;

		public enum VTGraphScreen
		{
			Heightmap = 0,
			Texture = 1,
			TerrainObject = 2,
		}

		private class VTEditorWindowData
		{
			public VTGraphScreen currentGraphScreen = VTGraphScreen.Heightmap;

			public bool displayPropertiesPanel = false;

			public VTEditorMainPanel mainPanel;

			public VTSettingsAsset currentAsset = null;
			public bool windowActive = false;
		}

		private VTEditorWindowData data = new VTEditorWindowData();

		private const float defaultSetupPanelWidth = 250.0f;

		private const string storeAssetKey = "RobProductions.VisualTerrain.VTSettingsAsset";
		private const string storeDisplayPropertiesKey = "RobProductions.VisualTerrain.DisplayProperties";
		private const string storeGraphScreenKey = "RobProductions.VisualTerrain.CurrentGraphScreen";

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

			data.mainPanel = new VTEditorMainPanel();
			data.mainPanel.OnEnable();

			data.mainPanel.events.onEditedAssetEvent += EditedAsset;
			data.mainPanel.events.onRegisterAssetStructureUndoEvent += RegisterAssetStructureUndo;
			data.mainPanel.events.onRegisterAssetDataUndoEvent += RegisterAssetDataUndo;

			//We reloaded or enabled for the first time
			//so check if we stored an asset path and load it into currentAsset
			CheckLoadStoredAsset();
			//Preload stored EditorWindow data
			CheckStoredDisplayProperties();
			CheckStoredCurrentGraphScreen();
			//Then set the asset to refresh all associated data based on EditorWindow data
			SetVTSettingsAsset(data.currentAsset);

			data.windowActive = true;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoPerformed;

			data.mainPanel.events.onEditedAssetEvent -= EditedAsset;
			data.mainPanel.events.onRegisterAssetStructureUndoEvent -= RegisterAssetStructureUndo;
			data.mainPanel.events.onRegisterAssetDataUndoEvent -= RegisterAssetDataUndo;

			data.mainPanel.OnDisable();

			data.windowActive = false;
		}

		//ASSET MANAGEMENT

		public VTSettingsAsset GetCurrentAsset()
		{
			return data.currentAsset;
		}

		public void ClearVTSettingsAsset()
		{
			SetVTSettingsAsset(null);
		}

		public void SetVTSettingsAsset(VTSettingsAsset newAsset)
		{
			//Set our current stored asset so that it can be read later
			SetStoredAsset(newAsset);

			//Set the current settings asset
			data.currentAsset = newAsset;
			data.mainPanel.SetMainSettingsAsset(newAsset);
			data.mainPanel.SetVTHasVTAsset(newAsset != null);

			//Refresh the target graph
			RefreshGraphScreen();

			//Reset values and redraw the screen
			if(newAsset == null)
			{
				SetDisplayProperties(false);
			}
			Repaint();
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

		void CheckStoredDisplayProperties()
		{
			if(EditorPrefs.HasKey(storeDisplayPropertiesKey))
			{
				if(data.currentAsset != null)
				{
					SetDisplayProperties(EditorPrefs.GetBool(storeDisplayPropertiesKey, false));
				}
			}
		}

		void SetStoredDisplayProperties(bool v)
		{
			EditorPrefs.SetBool(storeDisplayPropertiesKey, v);
		}

		void CheckStoredCurrentGraphScreen()
		{
			if(EditorPrefs.HasKey(storeGraphScreenKey))
			{
				if(data.currentAsset != null)
				{
					data.currentGraphScreen = (VTGraphScreen)EditorPrefs.GetInt(storeGraphScreenKey, 0);
				}
			}
		}

		void SetStoredCurrentGraphScreen(VTGraphScreen screen)
		{
			EditorPrefs.SetInt(storeGraphScreenKey, (int)screen);
		}

		/// <summary>
		/// Called before editing the scriptableobject data so that
		/// the Undo handler can be used if undoing the next change.
		/// Note that this will not work when adding/deleting references
		/// within the object, for that use RegisterAssetStructureUndo.
		/// </summary>
		/// <param name="description"></param>
		public void RegisterAssetDataUndo(string description)
		{
			if(data.currentAsset == null)
			{
				return;
			}

			Undo.RecordObject(data.currentAsset, description);
		}

		/// <summary>
		/// Called before editing the scriptableobject references
		/// so that Undo handler can register the change.
		/// This will store a complete reference in undo buffer
		/// including adding/deleting references.
		/// </summary>
		/// <param name="description"></param>
		public void RegisterAssetStructureUndo(string description)
		{
			if(data.currentAsset == null)
			{
				return;
			}

			Undo.RegisterCompleteObjectUndo(data.currentAsset, description);
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

		/// <summary>
		/// Callback for when the user has performed an undo action
		/// so that we can regenerate preview images and handle the
		/// rendering correctly.
		/// </summary>
		void UndoPerformed()
		{
			RegenerateGraphPreviewImages();
			Repaint();
		}

		//SCREENS

		void RefreshGraphScreen()
		{
			VTGraph finalDisplayGraph = null;

			if(data.currentAsset != null)
			{
				//Ensure graph types are set
				data.currentAsset.generationData.heightmapGraph.SetGraphType(VTGraph.GraphType.Height);
				data.currentAsset.generationData.textureGraph.SetGraphType(VTGraph.GraphType.Texture);
				data.currentAsset.generationData.terrainObjectGraph.SetGraphType(VTGraph.GraphType.TerrainObject);

				//Get the final display graph
				if (data.currentGraphScreen == VTGraphScreen.Heightmap)
				{
					finalDisplayGraph = data.currentAsset.generationData.heightmapGraph;
				}
				else if (data.currentGraphScreen == VTGraphScreen.Texture)
				{
					finalDisplayGraph = data.currentAsset.generationData.textureGraph;
				}
				else if (data.currentGraphScreen == VTGraphScreen.TerrainObject)
				{
					finalDisplayGraph = data.currentAsset.generationData.terrainObjectGraph;
				}
			}

			data.mainPanel.SetTargetGraph(finalDisplayGraph);
		}

		void RegenerateGraphPreviewImages()
		{
			data.mainPanel.RegenerateGraphPreviewImages();
		}

		void SetDisplayProperties(bool v)
		{
			data.displayPropertiesPanel = v;
			data.mainPanel.SetDisplayPropertiesPanel(v);
		}

		//RENDERING

		private void OnGUI()
		{
			//Create the GUI styles
			if(styles == null)
			{
				styles = new EditorWindowStyles();
			}

			var toolbarHeight = EditorStyles.toolbar.CalcHeight(GUIContent.none, position.width);

			var mainPanelRect = new Rect(0.0f, toolbarHeight, position.width, position.height - toolbarHeight);
			var setupPanelWidth = 0.0f;
			if (data.displayPropertiesPanel)
			{
				setupPanelWidth = defaultSetupPanelWidth;
			}
			var setupPanelRect = new Rect(mainPanelRect.x, mainPanelRect.y, setupPanelWidth, mainPanelRect.height);

			//Tell main panel to draw graph view across whole screen
			data.mainPanel.DrawGraphPanel(position, mainPanelRect, setupPanelRect);

			//Draw the top toolbar
			DrawToolbar();

			//Then draw the setup view if needed
			data.mainPanel.DrawPropertiesPanel(position, mainPanelRect, setupPanelRect);

			//Expand window space to bottom
			GUILayout.FlexibleSpace();

			//Draw bottom toolbar/buttons
			DrawBottomContents();

			//Process input
			var currentEvent = Event.current;
			bool doGraphEvent = false;
			bool doSetupEvent = false;
			bool handledGraphEvent = false;
			bool handledSetupEvent = false;

			if(currentEvent.isMouse)
			{
				//For mouse events, we want to know if the pointer is
				//inside the bounds of each view
				if(mainPanelRect.Contains(currentEvent.mousePosition))
				{
					if(setupPanelRect.Contains(currentEvent.mousePosition))
					{
						doSetupEvent = true;
					}
					else
					{
						doGraphEvent = true;
					}
				}
			}
			else
			{
				doGraphEvent = true;
				doSetupEvent = true;
			}

			if (doSetupEvent)
			{
				handledSetupEvent = data.mainPanel.ProcessSetupEvents(currentEvent);
			}
			if(doGraphEvent)
			{
				handledGraphEvent = data.mainPanel.ProcessGraphEvents(currentEvent);
			}

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
		}

		private class MoreOptionsPopup : PopupWindowContent
		{
			VTEditorWindow parentWindow;

			public MoreOptionsPopup(VTEditorWindow parentWindow)
			{
				this.parentWindow = parentWindow;
			}

			public override void OnGUI(Rect rect)
			{
				if(GUILayout.Button("Refresh UI"))
				{
					parentWindow.Repaint();
				}

				if(parentWindow.data.currentAsset != null)
				{
					if(GUILayout.Button("Clear Asset"))
					{
						parentWindow.ClearVTSettingsAsset();
					}
				}
			}
		}

		private void DrawToolbar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				//Left hand side
				if (GUILayout.Button(styles.moreOptionsContent, EditorStyles.toolbarButton))
				{
					PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), new MoreOptionsPopup(this));
				}

				if (data.currentAsset != null)
				{
					var propertiesStyle = styles.propertiesButtonStyle;

					var newDisplayPropertiesBool = GUILayout.Toggle(data.displayPropertiesPanel, styles.displayPropertiesContent, propertiesStyle);
					if(newDisplayPropertiesBool != data.displayPropertiesPanel)
					{
						SetDisplayProperties(newDisplayPropertiesBool);
						SetStoredDisplayProperties(data.displayPropertiesPanel);
					}

					GUILayout.Space(6f);

					var newGraphScreen = (VTGraphScreen)EditorGUILayout.EnumPopup(data.currentGraphScreen, EditorStyles.toolbarDropDown);
					if(newGraphScreen != data.currentGraphScreen)
					{
						data.currentGraphScreen = newGraphScreen;
						SetStoredCurrentGraphScreen(data.currentGraphScreen);
						RefreshGraphScreen();
					}
				}

				//Middle space
				GUILayout.FlexibleSpace();

				//Right hand side
				if(data.currentAsset != null)
				{
					var previewValue = GUILayout.Toggle(data.currentAsset.IsPreviewMode(), styles.previewToggleContent, styles.previewButtonStyle);
					if (previewValue != data.currentAsset.IsPreviewMode())
					{
						RegisterAssetStructureUndo("Toggled Preview Mode");
						data.currentAsset.SetPreviewMode(previewValue);
						EditedAsset();
					}

					if (GUILayout.Toggle(false, "Auto", styles.previewButtonStyle))
					{

					}

					if (GUILayout.Button("Generate", EditorStyles.miniButton))
					{
						//Tell all managers with this asset to generate the terrain
						VTEditorSceneInterface.GenerateTerrainsWithAsset(data.currentAsset);
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