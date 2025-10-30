
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
				previewButtonStyle = new GUIStyle(EditorStyles.miniButton);
				previewButtonStyle.padding = new RectOffset(8, 8, 2, 2);
			}
		}

		private EditorWindowStyles styles;

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

			public bool displayPropertiesPanel = false;

			public VTSettingsAsset currentAsset = null;
			public bool windowActive = false;
		}

		private VTEditorWindowData data = new VTEditorWindowData();

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
			SetStoredAsset(newAsset);
			data.currentAsset = newAsset;
			RefreshGraphScreen();

			if(newAsset == null)
			{
				data.displayPropertiesPanel = false;
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
					data.displayPropertiesPanel = EditorPrefs.GetBool(storeDisplayPropertiesKey, false);
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
				else if (data.currentGraphScreen == VTGraphScreen.Texture)
				{
					finalDisplayGraph = data.currentAsset.generationData.textureGraph;
				}
			}

			data.graphView.SetTargetGraph(finalDisplayGraph);
		}

		public VTEditorGraphView GetGraphView()
		{
			return data.graphView;
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

			var mainScreenRect = new Rect(0.0f, toolbarHeight, position.width, position.height - toolbarHeight);

			//Draw the graph view underneath the main panel

			var currentSetupWidth = 0.0f;
			if(data.displayPropertiesPanel)
			{
				currentSetupWidth = 250.0f;
			}

			var graphViewRect = new Rect(
				mainScreenRect.x + currentSetupWidth, mainScreenRect.y, mainScreenRect.width - currentSetupWidth, mainScreenRect.height);
			data.graphView.DrawGraphView(graphViewRect);

			//Draw the top toolbar
			DrawToolbar();

			//Then draw the setup view if needed
			var setupViewRect = new Rect(mainScreenRect.x, mainScreenRect.y, currentSetupWidth, mainScreenRect.height);
			if (data.displayPropertiesPanel)
			{
				var setupMode = VTEditorSetupView.SetupViewMode.AssetSettings;
				var currentSelectedNodes = data.graphView.GetSelectedGraphNodes();
				if(currentSelectedNodes.Count > 1)
				{
					setupMode = VTEditorSetupView.SetupViewMode.MultiNodeProperties;
				}
				else if (currentSelectedNodes.Count == 1)
				{
					setupMode = VTEditorSetupView.SetupViewMode.NodeProperties;

					data.setupView.SetEditingNode(currentSelectedNodes[0]);
				}
				data.setupView.SetSetupViewMode(setupMode);
				data.setupView.DrawSetupView(setupViewRect);
			}

			//Expand window space to bottom
			GUILayout.FlexibleSpace();

			//Draw bottom toolbar/buttons
			DrawBottomContents();

			//Process input
			var currentEvent = Event.current;
			bool doGraphEvent = false;
			bool doPropertiesEvent = false;
			bool handledGraphEvent = false;
			bool handledPropertiesEvent = false;

			if(currentEvent.isMouse)
			{
				//For mouse events, we want to know if the pointer is
				//inside the bounds of each view
				if(mainScreenRect.Contains(currentEvent.mousePosition))
				{
					if(setupViewRect.Contains(currentEvent.mousePosition))
					{
						doPropertiesEvent = true;
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
				doPropertiesEvent = true;
			}

			if (doPropertiesEvent)
			{
				handledPropertiesEvent = data.setupView.ProcessEvents(currentEvent);
			}
			if(doGraphEvent)
			{
				handledGraphEvent = data.graphView.ProcessEvents(currentEvent);
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

			/*
			//Reset scale to draw non-scaled elements
			//GUI.matrix = oldMatrix;
			*/
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
						data.displayPropertiesPanel = newDisplayPropertiesBool;
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
					GUILayout.BeginVertical();
					GUILayout.Space(3.2f);
					if (GUILayout.Toggle(false, "Auto", EditorStyles.toggle))
					{

					}
					GUILayout.EndVertical();
					if (GUILayout.Button("Generate", EditorStyles.miniButton))
					{
						//Tell all managers with this asset to generate the terrain
						VTEditorSceneInterface.GenerateTerrainsWithAsset(data.currentAsset);
					}
					var previewValue = GUILayout.Toggle(data.currentAsset.IsPreviewMode(), styles.previewToggleContent, styles.previewButtonStyle);
					if(previewValue != data.currentAsset.IsPreviewMode())
					{
						RegisterAssetStructureUndo("Toggled Preview Mode");
						data.currentAsset.SetPreviewMode(previewValue);
						EditedAsset();
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