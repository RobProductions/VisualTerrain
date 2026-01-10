#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RobProductions.VisualTerrain.Runtime;

namespace RobProductions.VisualTerrain.Editor
{
	public class VTEditorSetupView
	{
		private const int maxCustomNameLength = 25;

		public class SetupViewStyles
		{
			public readonly float propertiesHoriziontalPadding = 2f;
			public readonly float labelSeparatorPreSpace = 10f;
			public readonly float tabButtonWidth = 40f;
			public readonly float nodePropertyLabelWidth = 110f;

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

		//private VTEditorWindow parentWindow;
		private VTEditorMainPanel mainPanel;

		public VTEditorSetupView(VTEditorMainPanel mainPanel)
		{
			//this.parentWindow = parentWindow;
			this.mainPanel = mainPanel;
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

			var hasVTAsset = mainPanel.HasVTAsset();
			if(hasVTAsset)
			{
				if(setupViewMode == SetupViewMode.AssetSettings)
				{
					var mainSettingsAsset = mainPanel.GetMainSettingsAsset();
					if(mainSettingsAsset != null)
					{
						LayoutDrawAssetSettings(mainSettingsAsset.setupData);
					}
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
				mainPanel.EditedAsset();
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
			else if (assetSettingsTab == AssetSettingsTab.TerrainObjectSetup)
			{
				LayoutDrawTerrainObjectSetup(setupData.terrainObjectSetup);
			}
			else if (assetSettingsTab == AssetSettingsTab.ProcessingSetup)
			{
				LayoutDrawProcessingSetup(setupData.processingSetup);
			}
		}

		void LayoutDrawTerrainSetup(VTSetupTerrain terrainSetup)
		{
			DrawLabelSeparator("Terrain Size");
			//EditorGUILayout.HelpBox("Hi", MessageType.Info);

			var meshWidthLength = EditorGUILayout.Vector2Field("Total Mesh Size", terrainSetup.terrainSize.meshWidthLength);
			if (meshWidthLength != terrainSetup.terrainSize.meshWidthLength)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Mesh Width Length");
				terrainSetup.terrainSize.meshWidthLength = meshWidthLength;
			}
			var meshHeight = EditorGUILayout.FloatField("Mesh Height", terrainSetup.terrainSize.meshHeight);
			if (meshHeight != terrainSetup.terrainSize.meshHeight)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Mesh Height");
				terrainSetup.terrainSize.meshHeight = meshHeight;
			}
			var meshTerrainCountX = (VTSetupTerrain.TerrainCountType)EditorGUILayout.EnumPopup("Terrain Count X", terrainSetup.terrainSize.meshTerrainCountX);
			if (meshTerrainCountX != terrainSetup.terrainSize.meshTerrainCountX)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Mesh Terrain Count X");
				terrainSetup.terrainSize.meshTerrainCountX = meshTerrainCountX;
			}
			var meshTerrainCountY = (VTSetupTerrain.TerrainCountType)EditorGUILayout.EnumPopup("Terrain Count Y", terrainSetup.terrainSize.meshTerrainCountY);
			if (meshTerrainCountY != terrainSetup.terrainSize.meshTerrainCountY)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Mesh Terrain Count Y");
				terrainSetup.terrainSize.meshTerrainCountY = meshTerrainCountY;
			}

			DrawLabelSeparator("Terrain Resolution");

			var heightmapRes = (VTSetupTerrain.HeightmapResolution)EditorGUILayout.EnumPopup("Heightmap Resolution", terrainSetup.terrainResolution.heightmapResolution);
			if (heightmapRes != terrainSetup.terrainResolution.heightmapResolution)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Heightmap Resolution");
				terrainSetup.terrainResolution.heightmapResolution = heightmapRes;
			}

			var splatmapRes = (VTSetupTerrain.SplatmapResolution)EditorGUILayout.EnumPopup("Splatmap Resolution", terrainSetup.terrainResolution.splatmapResolution);
			if (splatmapRes != terrainSetup.terrainResolution.splatmapResolution)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Splatmap Resolution");
				terrainSetup.terrainResolution.splatmapResolution = splatmapRes;
			}

			var compositeRes = (VTSetupTerrain.SplatmapResolution)EditorGUILayout.EnumPopup("Composite Resolution", terrainSetup.terrainResolution.compositeSplatmapResolution);
			if (compositeRes != terrainSetup.terrainResolution.compositeSplatmapResolution)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Composite Splatmap Resolution");
				terrainSetup.terrainResolution.compositeSplatmapResolution = compositeRes;
			}

			var smoothingIterations = (VTSetupTerrain.PostSmoothingIterations)EditorGUILayout.EnumPopup("Post Smoothing", terrainSetup.terrainResolution.postSmoothingIterations);
			if (smoothingIterations != terrainSetup.terrainResolution.postSmoothingIterations)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Post Smoothing");
				terrainSetup.terrainResolution.postSmoothingIterations = smoothingIterations;
			}

			terrainSetup.terrainResolution.splatStitchingPercentRadius = 
				DrawSetupFloatSliderField("Splat Stitching Radius", terrainSetup.terrainResolution.splatStitchingPercentRadius, 0.0f, 0.1f,
				"The percent that each terrain object's splatmap blends into the next object when using multiple terrains. Higher value = smoother blend but ruins edge detail.");

			DrawLabelSeparator("Terrain Properties");

			terrainSetup.terrainProperties.lodPixelError = DrawSetupIntSliderField("LOD Pixel Error", terrainSetup.terrainProperties.lodPixelError, 1, 200,
				"Controls when lower res LOD chunks are shown based on size on the screen. Higher value = lower LOD bias. A good default is < 5.");

			terrainSetup.terrainProperties.compositeStartDistance =
				DrawSetupIntSliderField("Composite Start Distance", terrainSetup.terrainProperties.compositeStartDistance, 0, 20000,
				"The range at which the low-res composite terrain texture will be drawn in place of the high-res splatmaps.");

			var raytracingBool = EditorGUILayout.Toggle("Raytracing Support", terrainSetup.terrainProperties.raytracingSupport);
			if(raytracingBool != terrainSetup.terrainProperties.raytracingSupport)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Raytracing Support");
				terrainSetup.terrainProperties.raytracingSupport = raytracingBool;
			}

		}

		void LayoutDrawTerrainObjectSetup(VTSetupTerrainObject terrainObjectSetup)
		{
			DrawLabelSeparator("Object Placement");

			terrainObjectSetup.objectPlacement.placeObjectRandomSeed = DrawSetupIntField("Place Object Seed", terrainObjectSetup.objectPlacement.placeObjectRandomSeed,
				"The random number seed used for determining whether an object is placed and its position.");
			terrainObjectSetup.objectPlacement.instancePropertyRandomSeed = DrawSetupIntField("Instance Property Seed", terrainObjectSetup.objectPlacement.instancePropertyRandomSeed,
				"The random number seed used for determining a property of a placed object based on given ranges.");

			terrainObjectSetup.objectPlacement.placeObjectValueCutoff = DrawSetupFloatSliderField("Place Object Value Cutoff", terrainObjectSetup.objectPlacement.placeObjectValueCutoff,
				0.0f, 1.0f, "Map sample points below this value will be completely skipped for object placement.");
			terrainObjectSetup.objectPlacement.revalidatePositionMultiplier = DrawSetupFloatSliderField("Validate Position Multiplier", terrainObjectSetup.objectPlacement.revalidatePositionMultiplier,
				0.0f, 2.0f, "When objects check for revalidation of a sample point after random jitter, the value is multiplied by this. Higher multiplier = higher chance to pass validation.");
		}

		void LayoutDrawProcessingSetup(VTSetupProcessing processing)
		{
			DrawLabelSeparator("Output Settings");

			processing.texture.textureGenResolution = DrawSetupIntField("Output Resolution", processing.texture.textureGenResolution,
				"The resolution of all final graph output RangeGrids generated by Visual Terrain which are applied to Heightmap, Splatmaps, etc.");

			var multipleTerrainType = (VTSetupProcessing.MultipleTerrainTextureType)EditorGUILayout.EnumPopup("Multiple Terrains", processing.texture.textureMultipleTerrainHandling);
			if (multipleTerrainType != processing.texture.textureMultipleTerrainHandling)
			{
				mainPanel.RegisterAssetStructureUndo("Edited Multiple Terrain Handling");
				processing.texture.textureMultipleTerrainHandling = multipleTerrainType;
			}

			DrawLabelSeparator("Preview Settings");

			processing.preview.previewTextureGenResolution = DrawSetupIntField("Preview Resolution", processing.preview.previewTextureGenResolution,
				"The resolution of the output RangeGrids used for Preview Mode, activated by the Eye icon.");


		}


		//NODE SELECTED MODE

		void LayoutDrawNodeProperties(VTGraphNode node)
		{
			if(node == null)
			{
				GUILayout.Label("No node available");
				return;
			}

			var defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = styles.nodePropertyLabelWidth;

			DrawLabelSeparator(node.NodeTitle);
			GUILayout.Space(styles.labelSeparatorPreSpace * 0.5f);

			node.CustomName = DrawSetupStringField("Display Name", node.CustomName, false);
			if(node.CustomName.Length > maxCustomNameLength)
			{
				node.CustomName = node.CustomName.Substring(0, maxCustomNameLength);
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
						mainPanel.RegisterAssetStructureUndo("Edited Input Value Type");
						thisSlot.valueType = valueType;
						mainPanel.GetGraphView().TryUpdateOutputConnectedPreviews(node);
					}

					if(thisSlot.valueType != VTGraphConnectionSlot.SlotValueType.RangeGrid)
					{
						EditorGUILayout.BeginHorizontal();
						//GUILayout.Label("Default");

						if (thisSlot.valueType == VTGraphConnectionSlot.SlotValueType.RangeGrid)
						{
							/*
							Texture2D newTexture = (Texture2D)EditorGUILayout.ObjectField(thisSlot.defaultRangeGridValue, typeof(Texture2D), allowSceneObjects: false);
							if(newTexture != thisSlot.defaultRangeGridValue)
							{
								parentWindow.RegisterAssetStructureUndo("Edited Default Input");
								thisSlot.defaultRangeGridValue = newTexture;
								parentWindow.GetGraphView().TryUpdateOutputConnectedPreviews(node);
							}
							*/
						}
						else if (thisSlot.valueType == VTGraphConnectionSlot.SlotValueType.Float)
						{
							float newFloat = (float)EditorGUILayout.FloatField("Default", thisSlot.defaultFloatValue);
							if (newFloat != thisSlot.defaultFloatValue)
							{
								mainPanel.RegisterAssetStructureUndo("Edited Default Input");
								thisSlot.defaultFloatValue = newFloat;
								mainPanel.GetGraphView().TryUpdateOutputConnectedPreviews(node);
							}
						}
						EditorGUILayout.EndHorizontal();
					}
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

			EditorGUIUtility.labelWidth = defaultLabelWidth;
		}

		void BeginEditNodeProperty(string desc)
		{
			mainPanel.RegisterAssetStructureUndo(desc);
		}

		void EndEditNodeProperty(VTGraphNode node)
		{
			mainPanel.GetGraphView().TryUpdateOutputConnectedPreviews(node);
		}

		//MULTI SELECT MODE

		void LayoutDrawMultiSelectProperties()
		{
			EditorGUILayout.LabelField("Multiple nodes selected");
		}

		//UTILITY

		float DrawSetupFloatSliderField(string labelName, float value, float startValue, float endValue, string tooltip = "")
		{
			var content = new GUIContent(labelName, tooltip);

			GUILayout.Space(5f);
			GUILayout.Label(content);

			var sliderValue = EditorGUILayout.Slider("", value, startValue, endValue);
			if (sliderValue != value)
			{
				mainPanel.RegisterAssetStructureUndo("Edited " + labelName);
			}
			return sliderValue;
		}

		int DrawSetupIntSliderField(string labelName, int value, int startValue, int endValue, string tooltip = "")
		{
			var content = new GUIContent(labelName, tooltip);

			GUILayout.Space(5f);
			GUILayout.Label(content);

			var sliderValue = EditorGUILayout.IntSlider("", value, startValue, endValue);
			if (sliderValue != value)
			{
				mainPanel.RegisterAssetStructureUndo("Edited " + labelName);
			}
			return sliderValue;
		}

		string DrawSetupStringField(string labelName, string value, bool emptyLabel)
		{
			var changedString = EditorGUILayout.TextField(emptyLabel ? "" : labelName, value);
			if(changedString != value)
			{
				mainPanel.RegisterAssetStructureUndo("Edited " + labelName);
			}
			return changedString;
		}

		int DrawSetupIntField(string labelName, int value, string tooltip = "")
		{
			var content = new GUIContent(labelName, tooltip);

			var changedInt = EditorGUILayout.IntField(content, value);
			if (changedInt != value)
			{
				mainPanel.RegisterAssetStructureUndo("Edited " + labelName);
			}
			return changedInt;
		}

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