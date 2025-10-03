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
		public VTObjectInterface objectInterface;

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
			if (objectInterface == null)
			{
				objectInterface = new VTObjectInterface(this);
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
			objectInterface.CreateNeededObjectHolders();
		}
	}
}