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


			public SetupViewStyles()
			{

			}
		}

		private SetupViewStyles styles;

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

			

			GUILayout.EndArea();
		}

		void LayoutDrawTerrainProperties(VTSetupTerrain terrainSetup)
		{

			EditorGUILayout.HelpBox("Hi", MessageType.Info);
		}
	}
}

#endif