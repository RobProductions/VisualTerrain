#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RobProductions.VisualTerrain.Runtime;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTEditorSetupView
	{
		public class SetupViewStyles
		{
			public readonly float propertiesHoriziontalPadding = 2f;
			public readonly float labelSeparatorPreSpace = 10f;
			public readonly float tabButtonWidth = 40f;

			public Texture2D terrainPropertiesIcon;
			public Texture2D terrainObjectPropertiesIcon;
			public Texture2D customObjectPropertiesIcon;
			public Texture2D processingPropertiesIcon;

			public SetupViewStyles()
			{
				terrainPropertiesIcon = EditorGUIUtility.Load("TerrainInspector.TerrainToolAdd") as Texture2D;
				terrainObjectPropertiesIcon = EditorGUIUtility.Load("TerrainInspector.TerrainToolPlants") as Texture2D;
				customObjectPropertiesIcon = EditorGUIUtility.Load("GameObject Icon") as Texture2D;
				processingPropertiesIcon = EditorGUIUtility.Load("TerrainInspector.TerrainToolSettings") as Texture2D;
			}
		}

		private SetupViewStyles styles;

		public enum SetupViewMode
		{
			AssetSettings = 0,
			NodeProperties = 1,
			MultiNodeProperties = 2,
		}

		private SetupViewMode setupViewMode = SetupViewMode.AssetSettings;
		private VTGraphNode editingNode = null;

		public enum AssetSettingsTab
		{
			TerrainSetup,
			TerrainObjectSetup,
			CustomObjectSetup,
			ProcessingSetup
		}

		public AssetSettingsTab assetSettingsTab = AssetSettingsTab.TerrainSetup;

		private Vector2 scrollPosition = Vector2.zero;

		private VTEditorWindow parentWindow;

		public VTEditorSetupView(VTEditorWindow parentWindow)
		{
			this.parentWindow = parentWindow;
		}

		//LIFECYCLE

		public void OnEnable()
		{

		}

		public void OnDisable()
		{

		}

		/// <summary>
		/// GUI styles can only be created
		/// from OnGUI since we rely on GUI.skin,
		/// so only call this from OnGUI thread
		/// </summary>
		void CreateGUIStyles()
		{
			if (styles == null)
			{
				styles = new SetupViewStyles();
			}
		}

		//SETTERS

		public void SetSetupViewMode(SetupViewMode mode)
		{
			setupViewMode = mode;
		}

		public void SetEditingNode(VTGraphNode node)
		{
			editingNode = node;
		}

		public void SetAssetSettingsTab(AssetSettingsTab tab)
		{
			if(assetSettingsTab == tab)
			{
				return;
			}

			assetSettingsTab = tab;
		}

		//PROCESSING

		public bool ProcessEvents(Event e)
		{
			switch(e.type)
			{
				case EventType.MouseDown:
					if(e.button == 0)
					{
						GUI.FocusControl(null);
						GUI.changed = true;
						return true;
					}
					break;
			}
			return false;
		}

		//RENDERING

		public void DrawSetupView(Rect setupRect)
		{
			//First create GUI styles if not created yet
			CreateGUIStyles();

			//Draw a box to cover the setup view portion
			GUI.Box(setupRect, "", GUI.skin.box);

			//Begin a subarea so GUILayout works within just the setup box
			GUILayout.BeginArea(setupRect);

			//Begin a padded subarea
			var propertiesRect = new Rect(styles.propertiesHoriziontalPadding, 0f,
				setupRect.width - (styles.propertiesHoriziontalPadding * 2.0f), setupRect.height);
			GUILayout.BeginArea(propertiesRect);

			//Start a scrollview so that it works with any height
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

			//Check for any changed values
			EditorGUI.BeginChangeCheck();

			var currentAsset = parentWindow.GetCurrentAsset();
			if(currentAsset != null)
			{
				if(setupViewMode == SetupViewMode.AssetSettings)
				{
					LayoutDrawAssetSettings(currentAsset.setupData);
				}
				else if (setupViewMode == SetupViewMode.NodeProperties)
				{
					LayoutDrawNodeProperties(editingNode);
				}
				else if (setupViewMode == SetupViewMode.MultiNodeProperties)
				{
					LayoutDrawMultiSelectProperties();
				}
			}

			//If we changed any values, inform that we edited the asset
			if (EditorGUI.EndChangeCheck())
			{
				parentWindow.EditedAsset();
			}
			//End the scrollview
			GUILayout.EndScrollView();
			//End padded subarea
			GUILayout.EndArea();

			GUILayout.EndArea();
		}

		//TERRAIN MODE

		void LayoutDrawAssetSettings(VTSettingsAsset.SetupData setupData)
		{
			GUILayout.Space(styles.labelSeparatorPreSpace);

			//Render the tab selector buttons
			GUILayout.BeginHorizontal();

			GUILayout.FlexibleSpace();

			DrawTabButton(styles.terrainPropertiesIcon, AssetSettingsTab.TerrainSetup, EditorStyles.miniButtonLeft);
			DrawTabButton(styles.terrainObjectPropertiesIcon, AssetSettingsTab.TerrainObjectSetup, EditorStyles.miniButtonMid);
			DrawTabButton(styles.customObjectPropertiesIcon, AssetSettingsTab.CustomObjectSetup, EditorStyles.miniButtonMid);
			DrawTabButton(styles.processingPropertiesIcon, AssetSettingsTab.ProcessingSetup, EditorStyles.miniButtonRight);

			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();

			GUILayout.Space(1f);

			//Render the actual properties in this tab
			if (assetSettingsTab == AssetSettingsTab.TerrainSetup)
			{
				LayoutDrawTerrainSetup(setupData.terrainSetup);
			}
			else if (assetSettingsTab == AssetSettingsTab.ProcessingSetup)
			{
				LayoutDrawProcessingSetup();
			}
		}

		void LayoutDrawTerrainSetup(VTSetupTerrain terrainSetup)
		{
			DrawLabelSeparator("Terrain Size");
			//EditorGUILayout.HelpBox("Hi", MessageType.Info);

			var meshWidthLength = EditorGUILayout.Vector2Field("Total Mesh Size", terrainSetup.terrainSize.meshWidthLength);
			if (meshWidthLength != terrainSetup.terrainSize.meshWidthLength)
			{
				parentWindow.RegisterAssetStructureUndo("Edited Mesh Width Length");
				terrainSetup.terrainSize.meshWidthLength = meshWidthLength;
			}
			var meshHeight = EditorGUILayout.FloatField("Mesh Height", terrainSetup.terrainSize.meshHeight);
			if (meshHeight != terrainSetup.terrainSize.meshHeight)
			{
				parentWindow.RegisterAssetStructureUndo("Edited Mesh Height");
				terrainSetup.terrainSize.meshHeight = meshHeight;
			}

			DrawLabelSeparator("Terrain Resolution");

			var heightmapRes = (VTSetupTerrain.HeightmapResolution)EditorGUILayout.EnumPopup("Heightmap Resolution", terrainSetup.terrainResolution.heightmapResolution);
			if (heightmapRes != terrainSetup.terrainResolution.heightmapResolution)
			{
				parentWindow.RegisterAssetStructureUndo("Edited Heightmap Resolution");
				terrainSetup.terrainResolution.heightmapResolution = heightmapRes;
			}

			var splatmapRes = (VTSetupTerrain.SplatmapResolution)EditorGUILayout.EnumPopup("Splatmap Resolution", terrainSetup.terrainResolution.splatmapResolution);
			if (splatmapRes != terrainSetup.terrainResolution.splatmapResolution)
			{
				parentWindow.RegisterAssetStructureUndo("Edited Splatmap Resolution");
				terrainSetup.terrainResolution.splatmapResolution = splatmapRes;
			}

			var compositeRes = (VTSetupTerrain.SplatmapResolution)EditorGUILayout.EnumPopup("Composite Resolution", terrainSetup.terrainResolution.compositeSplatmapResolution);
			if (compositeRes != terrainSetup.terrainResolution.compositeSplatmapResolution)
			{
				parentWindow.RegisterAssetStructureUndo("Edited Composite Splatmap Resolution");
				terrainSetup.terrainResolution.compositeSplatmapResolution = compositeRes;
			}
		}

		void LayoutDrawProcessingSetup()
		{
			DrawLabelSeparator("Preview Settings");

			DrawLabelSeparator("???");

		}


		//NODE SELECTED MODE

		void LayoutDrawNodeProperties(VTGraphNode node)
		{
			if(node == null)
			{
				GUILayout.Label("No node available");
				return;
			}

			if(node.inputConnections.Length > 0)
			{
				//Render input settings
				DrawLabelSeparator("Input Settings");

				for(int i = 0; i < node.inputConnections.Length; i++)
				{
					GUILayout.Space(styles.labelSeparatorPreSpace);

					var thisSlot = node.inputConnections[i];
					VTGraphConnectionSlot.SlotValueType valueType = (VTGraphConnectionSlot.SlotValueType)EditorGUILayout.EnumPopup(thisSlot.connectionSlotName, thisSlot.valueType);
					if(valueType != thisSlot.valueType)
					{
						parentWindow.RegisterAssetStructureUndo("Edited Input Value Type");
						thisSlot.valueType = valueType;
						parentWindow.GetGraphView().TryUpdateOutputConnectedPreviews(node);
					}

					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Default");

					if(thisSlot.valueType == VTGraphConnectionSlot.SlotValueType.Texture)
					{
						Texture2D newTexture = (Texture2D)EditorGUILayout.ObjectField(thisSlot.defaultTextureValue, typeof(Texture2D), allowSceneObjects: false);
						if(newTexture != thisSlot.defaultTextureValue)
						{
							parentWindow.RegisterAssetStructureUndo("Edited Default Input");
							thisSlot.defaultTextureValue = newTexture;
							parentWindow.GetGraphView().TryUpdateOutputConnectedPreviews(node);
						}
					}
					else if (thisSlot.valueType == VTGraphConnectionSlot.SlotValueType.Float)
					{
						float newFloat = (float)EditorGUILayout.FloatField(thisSlot.defaultFloatValue);
						if(newFloat != thisSlot.defaultFloatValue)
						{
							parentWindow.RegisterAssetStructureUndo("Edited Default Input");
							thisSlot.defaultFloatValue = newFloat;
							parentWindow.GetGraphView().TryUpdateOutputConnectedPreviews(node);
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}

			if(node.HasNodeProperties)
			{
				//Render node properties
				DrawLabelSeparator("Node Properties");
				GUILayout.Space(styles.labelSeparatorPreSpace);

				node.beginEditNodePropertyEvent += BeginEditNodeProperty;
				node.endEditNodePropertyEvent += EndEditNodeProperty;

				node.RenderNodeProperties();

				node.beginEditNodePropertyEvent -= BeginEditNodeProperty;
				node.endEditNodePropertyEvent -= EndEditNodeProperty;
			}
		}

		void BeginEditNodeProperty(string desc)
		{
			parentWindow.RegisterAssetStructureUndo(desc);
		}

		void EndEditNodeProperty(VTGraphNode node)
		{
			parentWindow.GetGraphView().TryUpdateOutputConnectedPreviews(node);
		}

		//MULTI SELECT MODE

		void LayoutDrawMultiSelectProperties()
		{
			EditorGUILayout.LabelField("Multiple nodes selected");
		}

		//UTILITY

		void DrawTabButton(Texture2D icon, AssetSettingsTab tabType, GUIStyle baseStyle)
		{

			bool buttonSelected = assetSettingsTab == tabType;
			var content = new GUIContent(icon, tabType.ToString());

			var style = new GUIStyle(baseStyle);
			style.padding = new RectOffset(0, 0, 2, 2);

			bool newToggleValue = GUILayout.Toggle(buttonSelected, content, style, GUILayout.Width(styles.tabButtonWidth));
			if (newToggleValue)
			{
				SetAssetSettingsTab(tabType);
			}
		}

		void DrawLabelSeparator(string labelText)
		{
			GUILayout.Space(styles.labelSeparatorPreSpace);
			GUILayout.Label(labelText, EditorStyles.boldLabel);
		}
	}
}

#endif