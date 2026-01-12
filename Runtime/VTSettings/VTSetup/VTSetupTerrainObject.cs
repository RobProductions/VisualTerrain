using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTSetupTerrainObject
	{
		[System.Serializable]
		public class TerrainObjectProperties
		{
			public bool bakeTreeLightProbes = true;
			public bool removeLightProbeRinging = true;
			public bool preservePrototypeLayers = true;
		}

		public TerrainObjectProperties objectProperties = new TerrainObjectProperties();

		[System.Serializable]
		public class TerrainObjectPlacement
		{
			public int defaultPlacementSeed = 100;
			public int defaultPropertySeed = 200;

			public float placeObjectValueCutoff = 0.1f;
			public float revalidatePositionMultiplier = 1.2f;
		}

		[SerializeField]
		public TerrainObjectPlacement objectPlacement = new TerrainObjectPlacement();
	}
}
