using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[ExecuteInEditMode]
	public class VisualTerrainManager : MonoBehaviour
	{
		/// <summary>
		/// The main settings asset to use for terrain generation.
		/// </summary>
		public VTSettingsAsset settingsAsset;

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

		void GenerateTerrain()
		{
			if(settingsAsset == null)
			{
				return;
			}
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