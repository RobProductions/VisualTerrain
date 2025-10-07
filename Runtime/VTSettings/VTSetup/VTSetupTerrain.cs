using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTSetupTerrain
	{
		[System.Serializable]
		public class SetupTerrainSize
		{
			public Vector2 meshWidthLength = new Vector2(64f, 64f);
			public float meshHeight = 5f;
		}

		public SetupTerrainSize terrainSize;

		public enum HeightmapResolution
		{
			x33 = 0,
			x65 = 1,
			x129 = 2,
			x257 = 3,
			x513 = 4,
			x1025 = 5,
			x2049 = 6,
			x4097 = 7,
		}

		[System.Serializable]
		public class SetupTerrainResolution
		{
			public HeightmapResolution heightmapResolution = HeightmapResolution.x65;
		}

		public SetupTerrainResolution terrainResolution;
	}
}