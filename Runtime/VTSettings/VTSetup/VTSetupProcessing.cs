using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
    [System.Serializable]
    public class VTSetupProcessing
    {
		[System.Serializable]
		public class SetupPreviewSettings
		{
			public VTSetupTerrain.HeightmapResolution previewHeightmapResolution = VTSetupTerrain.HeightmapResolution.x65;
			public VTSetupTerrain.SplatmapResolution previewSplatmapResolution = VTSetupTerrain.SplatmapResolution.x64;
		}

		[SerializeField]
		public SetupPreviewSettings preview = new SetupPreviewSettings();
	}
}