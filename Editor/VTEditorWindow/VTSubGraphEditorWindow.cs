#if UNITY_EDITOR

using RobProductions.VisualTerrain.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

		private class VTSubGraphEditorWindowData
		{
			public VTEditorMainPanel mainPanel;

			public bool displayPropertiesPanel = false;

			public VTSubGraphAsset currentAsset = null;
			public bool windowActive = false;
		}

		private VTSubGraphEditorWindowData data = new VTSubGraphEditorWindowData();

		private const float defaultSetupPanelWidth = 250.0f;

		private const string storeSubGraphAssetKey = "RobProductions.VisualTerrain.VTSubGraphAsset";
		private const string storeSubGraphDisplayPropertiesKey = "RobProductions.VisualTerrain.SubGraphDisplayProperties";

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
				window.SetVTSubGraphAsset(settingsAssetObject);
				window.Show();
				return true;
			}
			//Let Unity open instead
			return false;
		}

		private void OnEnable()
		{
			data.mainPanel = new VTEditorMainPanel();
			data.mainPanel.OnEnable();

			data.mainPanel.events.onEditedAssetEvent += EditedAsset;
			data.mainPanel.events.onRegisterAssetStructureUndoEvent += RegisterAssetStructureUndo;
			data.mainPanel.events.onRegisterAssetDataUndoEvent += RegisterAssetDataUndo;

			Undo.undoRedoPerformed += UndoPerformed;
			EditorSceneManager.activeSceneChanged += ActiveRuntimeSceneChanged;
			EditorSceneManager.activeSceneChangedInEditMode += ActiveEditorSceneChanged;

			//We reloaded or enabled for the first time
			//so check if we stored an asset path and load it into currentAsset
			CheckLoadStoredAsset();
			//Preload stored EditorWindow data
			CheckStoredDisplayProperties();
			//Then set the asset to refresh all associated data based on EditorWindow data
			SetVTSubGraphAsset(data.currentAsset);

			data.windowActive = true;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoPerformed;
			EditorSceneManager.activeSceneChanged -= ActiveRuntimeSceneChanged;
			EditorSceneManager.activeSceneChangedInEditMode -= ActiveEditorSceneChanged;

			data.mainPanel.events.onEditedAssetEvent -= EditedAsset;
			data.mainPanel.events.onRegisterAssetStructureUndoEvent -= RegisterAssetStructureUndo;
			data.mainPanel.events.onRegisterAssetDataUndoEvent -= RegisterAssetDataUndo;

			data.mainPanel.OnDisable();

			data.windowActive = false;
		}

		//CALLBACKS

		void ActiveRuntimeSceneChanged(Scene lastScene, Scene newScene)
		{
			if (data.currentAsset != null)
			{
				//Nothing for now
			}
		}

		void ActiveEditorSceneChanged(Scene lastScene, Scene newScene)
		{
			if (data.currentAsset != null)
			{
				RefreshGraphScreen();

				data.mainPanel.RegenerateGraphPreviewImages();
			}
		}

		//ASSET MANAGEMENT

		public VTSubGraphAsset GetCurrentAsset()
		{
			return data.currentAsset;
		}

		public void ClearVTSubGraphAsset()
		{
			SetVTSubGraphAsset(null);
		}

		public void SetVTSubGraphAsset(VTSubGraphAsset newAsset)
		{
			//Store the sub graph asset for later
			SetStoredAsset(newAsset);

			//Set the sub graph asset
			data.currentAsset = newAsset;
			data.mainPanel.SetVTHasVTAsset(newAsset != null);

			//Refresh screen data and values
			RefreshGraphScreen();

			if (newAsset == null)
			{
				SetDisplayProperties(false);
			}
			Repaint();
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
			if (data.currentAsset == null)
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
			if (data.currentAsset == null)
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
			if (data.currentAsset == null)
			{
				return;
			}

			EditorUtility.SetDirty(data.currentAsset);
		}

		void CheckLoadStoredAsset()
		{
			if (EditorPrefs.HasKey(storeSubGraphAssetKey))
			{
				var retrievedPath = EditorPrefs.GetString(storeSubGraphAssetKey);
				if (retrievedPath != null && retrievedPath != "")
				{
					data.currentAsset = AssetDatabase.LoadAssetAtPath<VTSubGraphAsset>(retrievedPath);
				}
			}
		}

		void SetStoredAsset(VTSubGraphAsset storedAsset)
		{
			var finalPath = "";
			if (storedAsset != null)
			{
				finalPath = AssetDatabase.GetAssetPath(storedAsset);
			}
			EditorPrefs.SetString(storeSubGraphAssetKey, finalPath);
		}

		void CheckStoredDisplayProperties()
		{
			if (EditorPrefs.HasKey(storeSubGraphDisplayPropertiesKey))
			{
				if (data.currentAsset != null)
				{
					SetDisplayProperties(EditorPrefs.GetBool(storeSubGraphDisplayPropertiesKey, false));
				}
			}
		}

		void SetStoredDisplayProperties(bool v)
		{
			EditorPrefs.SetBool(storeSubGraphDisplayPropertiesKey, v);
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

			if (data.currentAsset != null)
			{
				//Ensure graph type is set
				data.currentAsset.subGraph.SetGraphType(VTGraph.GraphType.SubGraph);
				finalDisplayGraph = data.currentAsset.subGraph;
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
			if (styles == null)
			{
				styles = new SubGraphEditorWindowStyles();
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
			DrawSubGraphToolbar();

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

			if (currentEvent.isMouse)
			{
				//For mouse events, we want to know if the pointer is
				//inside the bounds of each view
				if (mainPanelRect.Contains(currentEvent.mousePosition))
				{
					if (setupPanelRect.Contains(currentEvent.mousePosition))
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
			if (doGraphEvent)
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
			VTSubGraphEditorWindow parentWindow;

			public MoreOptionsPopup(VTSubGraphEditorWindow parentWindow)
			{
				this.parentWindow = parentWindow;
			}

			public override void OnGUI(Rect rect)
			{
				if (GUILayout.Button("Refresh UI"))
				{
					parentWindow.Repaint();
				}

				if (parentWindow.data.currentAsset != null)
				{
					if (GUILayout.Button("Clear Asset"))
					{
						parentWindow.ClearVTSubGraphAsset();
					}
				}
			}
		}

		private void DrawSubGraphToolbar()
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
					if (newDisplayPropertiesBool != data.displayPropertiesPanel)
					{
						SetDisplayProperties(newDisplayPropertiesBool);
						SetStoredDisplayProperties(data.displayPropertiesPanel);
					}

					GUILayout.Space(6f);

					/*
					var newGraphScreen = (VTGraphScreen)EditorGUILayout.EnumPopup(data.currentGraphScreen, EditorStyles.toolbarDropDown);
					if (newGraphScreen != data.currentGraphScreen)
					{
						data.currentGraphScreen = newGraphScreen;
						SetStoredCurrentGraphScreen(data.currentGraphScreen);
						RefreshGraphScreen();
					}
					*/
				}

				//Middle space
				GUILayout.FlexibleSpace();

				//Right hand side
				if (data.currentAsset != null)
				{
					/*
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
					*/
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