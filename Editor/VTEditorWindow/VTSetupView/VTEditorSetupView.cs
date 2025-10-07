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

			public SetupViewStyles()
			{

			}
		}

		private SetupViewStyles styles;

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
				LayoutDrawTerrainProperties(currentAsset.setupData.terrainSetup);
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

		void LayoutDrawTerrainProperties(VTSetupTerrain terrainSetup)
		{
			DrawLabelSeparator("Terrain Size");
			//EditorGUILayout.HelpBox("Hi", MessageType.Info);

			var meshWidthLength = EditorGUILayout.Vector2Field("Total Mesh Size", terrainSetup.terrainSize.meshWidthLength);
			if(meshWidthLength != terrainSetup.terrainSize.meshWidthLength)
			{
				parentWindow.RegisterAssetStructureUndo("Edited Mesh Width Length");
				terrainSetup.terrainSize.meshWidthLength = meshWidthLength;
			}
			var meshHeight = EditorGUILayout.FloatField("Mesh Height", terrainSetup.terrainSize.meshHeight);
			if(meshHeight != terrainSetup.terrainSize.meshHeight)
			{
				parentWindow.RegisterAssetStructureUndo("Edited Mesh Height");
				terrainSetup.terrainSize.meshHeight = meshHeight;
			}

			DrawLabelSeparator("Terrain Resolution");

			var heightmapRes = (VTSetupTerrain.HeightmapResolution)EditorGUILayout.EnumPopup("Heightmap Resolution", terrainSetup.terrainResolution.heightmapResolution);
			if(heightmapRes != terrainSetup.terrainResolution.heightmapResolution)
			{
				parentWindow.RegisterAssetStructureUndo("Edited Heightmap Resolution");
				terrainSetup.terrainResolution.heightmapResolution = heightmapRes;
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