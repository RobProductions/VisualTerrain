using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	/// <summary>
	/// This asset represents one entire project for managing terrain. <br></br>
	/// Data here is stored in the Assets folder and edited from the inspector or the
	/// VT Editor Window which associates itself with a VT Settings Asset. 
	/// </summary>
	[CreateAssetMenu(fileName = "VT Settings Asset", menuName = "VisualTerrain/VT Settings Asset", order = 0)]
	public class VTSettingsAsset : ScriptableObject
	{
		public const int ASSET_API_VERSION = 1;

		[System.Serializable]
		public class SetupData
		{
			/// <summary>
			/// When enabled, a lower quality version of the textures being processed
			/// and low res settings will be used to
			/// generate the terrain.
			/// </summary>
			public bool previewMode = true;
			public VTSetupTerrain terrainSetup = new VTSetupTerrain();
			public VTSetupProcessing processingSetup = new VTSetupProcessing();
		}

		[SerializeField]
		public SetupData setupData = new SetupData();

		[System.Serializable]
		public class GenerationData
		{
			public VTGraph heightmapGraph = new VTGraph();
			public VTGraph textureGraph = new VTGraph();

			[NonSerialized]
			public VTRangeGrid cachedThumbHeightmapGrid = VTRangeGrid.Empty;
			[NonSerialized]
			public VTRangeGrid cachedHeightmapGrid = VTRangeGrid.Empty;
		}

		[SerializeField]
		public GenerationData generationData = new GenerationData();

		[SerializeField]
		private int serializedAssetVersion = 0;

		//INIT

		public VTSettingsAsset()
		{
			/*
			//test data
			var testNode = new VTGraphNodeTest();
			testNode.NodePosition = new Vector2(90, 120);
			generationData.heightmapGraph.AddNode(testNode);
			generationData.heightmapGraph.AddNode(new VTGraphNodeTest());
			*/

			CreateDefaultNodes();
		}

		void CreateDefaultNodes()
		{

		}

		private void OnEnable()
		{
			//If we loaded an old version of the asset,
			//format it correctly for the new version
			if(serializedAssetVersion < ASSET_API_VERSION)
			{
				//Initializes at 0, so we can skip that "version",
				//it must be up to date already
				if (serializedAssetVersion > 0)
				{
					ValidateNewVersionFormat(serializedAssetVersion);
				}
			}
			//Then set the stored version number
			serializedAssetVersion = ASSET_API_VERSION;
		}

		//GETTERS

		public bool IsPreviewMode()
		{
			return setupData.previewMode;
		}

		//SETTERS

		public void SetPreviewMode(bool v)
		{
			setupData.previewMode = v;
		}

		/// <summary>
		/// Set the cache of an arbitrary thumbnail or real heightmap value,
		/// based on the processingSettings.
		/// </summary>
		/// <param name="heightmap"></param>
		/// <param name="processingSettings"></param>
		public void SetCachedHeightmap(VTRangeGrid heightmap, VTGraphProcessingSettings processingSettings)
		{
			if(processingSettings.thumbnailMode)
			{
				SetCachedThumbnailHeightmapTexture(heightmap);
			}
			else
			{
				SetCachedHeightmapTexture(heightmap);
			}
		}

		/// <summary>
		/// Set the cached thumbnail heightmap texture for sampling.
		/// This must also be called whenever the height output changes
		/// so that later sampling is up to date.
		/// </summary>
		/// <param name="thumbHeightmap"></param>
		public void SetCachedThumbnailHeightmapTexture(VTRangeGrid thumbHeightmap)
		{
			generationData.cachedThumbHeightmapGrid = thumbHeightmap;
		}

		/// <summary>
		/// Set the cached heightmap texture for use in heightmap sampling.
		/// The cache value should only be utilized after generating the heightmap
		/// and ensuring that no changes to height can happen before use.
		/// </summary>
		/// <param name="heightmap"></param>
		public void SetCachedHeightmapTexture(VTRangeGrid heightmap)
		{
			generationData.cachedHeightmapGrid = heightmap;
		}

		//VALIDATION

		void ValidateNewVersionFormat(int oldVersionNum)
		{
			VTLog.Log("Validating new version format from VERSION: " + oldVersionNum);
		}
	}
}
