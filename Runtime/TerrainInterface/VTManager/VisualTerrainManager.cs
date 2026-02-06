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
			/// <summary>
			/// When enabled, force preview mode to true for this
			/// specific manager instance.
			/// </summary>
			[Header("Generation")]
			public bool forcePreviewMode = false;

			/// <summary>
			/// When enabled, this manager instance will no longer
			/// generate or change existing terrains with VT. They can
			/// still be edited manually for further customization.
			/// </summary>
			public bool lockGeneration = false;

			/// <summary>
			/// The base name of generated terrain objects.
			/// </summary>
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

		public class VTManagerEvents
		{
			public delegate void OnStartGenerateTerrain(VisualTerrainManager manager);
			public OnStartGenerateTerrain onStartGenerateTerrainEvent;
			public delegate void OnFinishGenerateTerrain(VisualTerrainManager manager);
			public OnFinishGenerateTerrain onFinishGenerateTerrainEvent;
		}

		public VTManagerEvents events = new VTManagerEvents();

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

		//GETTERS

		public bool IsPreviewMode()
		{
			if(settingsAsset != null)
			{
				if(settingsAsset.IsPreviewMode())
				{
					return true;
				}
			}
			return properties.forcePreviewMode;
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
			if(properties.lockGeneration)
			{
				return;
			}

			//We can attempt to generate
			events.onStartGenerateTerrainEvent?.Invoke(this);

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

			//We finished all the generation steps
			events.onFinishGenerateTerrainEvent?.Invoke(this);

			//TODO: At some point this could work?

			/*
			if (settingsAsset.setupData.processingSetup.algorithm.threadingType == VTSetupProcessing.AlgorithmThreadingType.DynamicCoroutine)
			{
				//Run Coroutine call
#if UNITY_EDITOR
				VTEditorCoroutine.Start(InternalGenerateTerrain(true));
#else
				StartCoroutine(InternalGenerateTerrain(true));
#endif
			}
			else
			{
				//Run blocking call
				InternalGenerateTerrain(false);
			}
			*/
		}

		IEnumerator InternalGenerateTerrain(bool coroutineYield)
		{
			//Create object holders within the manager transform
			//to hold terrain meshes and placed objects
			containerInterface.CreateNeededObjectHolders();

			if (coroutineYield)
			{
				yield return null;
			}

			//Create the actual terrain objects if needed
			terrainInterface.GenerateTerrain();

			if(coroutineYield)
			{
				yield return null;
			}

			//We finished all the generation steps
			events.onFinishGenerateTerrainEvent?.Invoke(this);
		}
	}
}