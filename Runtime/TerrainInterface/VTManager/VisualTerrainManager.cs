using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RobProductions.VisualTerrain.Runtime
{
	[ExecuteInEditMode]
	public class VisualTerrainManager : MonoBehaviour
	{
		/// <summary>
		/// The main settings asset to use for terrain generation.
		/// </summary>
		public VTSettingsAsset settingsAsset;

		[System.Serializable]
		public class VTManagerProperties
		{
			[Header("Containers")]
			public string terrainObjectName = "TerrainObject";
		}

		/// <summary>
		/// Setup properties for the manager and generated objects.
		/// </summary>
		public VTManagerProperties properties = new VTManagerProperties();

		/// <summary>
		/// An interface to manage object holders and terrain.
		/// </summary>
		public VTContainerInterface containerInterface;
		/// <summary>
		/// An interface to manage generation and modification
		/// of terrain GameObjects.
		/// </summary>
		public VTTerrainInterface terrainInterface;

		//LIFECYCLE

		private void Awake()
		{
			CheckInit();
		}

		private void OnEnable()
		{
			CheckInit();
		}

		private void OnDisable()
		{
			
		}

		void CheckInit()
		{
			if (containerInterface == null)
			{
				containerInterface = new VTContainerInterface(this);
			}
			if(terrainInterface == null)
			{
				terrainInterface = new VTTerrainInterface(this);
			}
		}

		//INPUT

		[ContextMenu("Generate Visual Terrain")]
		public void UserGenerateTerrain()
		{
			if(settingsAsset == null)
			{
				return;
			}
			
			GenerateTerrain();
		}

		//GENERATION

		public void GenerateTerrain()
		{
			if(settingsAsset == null)
			{
				return;
			}
#if UNITY_EDITOR
			//Register all changes to the Undo system in Editor mode
			Undo.RegisterCompleteObjectUndo(gameObject, "Generated Visual Terrain");
#endif

			//Make sure data is initialized
			CheckInit();

			//Create object holders within the manager transform
			//to hold terrain meshes and placed objects
			containerInterface.CreateNeededObjectHolders();
			//Create the actual terrain objects if needed
			terrainInterface.GenerateTerrain();

			
		}
	}
}