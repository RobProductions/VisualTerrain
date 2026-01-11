using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTSetupTerrain
	{
		public enum TerrainCountType
		{
			x1 = 1,
			x2 = 2,
			x3 = 3,
			x4 = 4,
			x5 = 5,
			x6 = 6,
			x8 = 8,
		}

		[System.Serializable]
		public class SetupTerrainSize
		{
			public Vector2 meshWidthLength = new Vector2(64f, 64f);
			public float meshHeight = 5f;
			public TerrainCountType meshTerrainCountX = TerrainCountType.x1;
			public TerrainCountType meshTerrainCountY = TerrainCountType.x1;
		}

		[SerializeField]
		public SetupTerrainSize terrainSize = new SetupTerrainSize();

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

		public enum SplatmapResolution
		{
			x16 = 0,
			x32 = 1,
			x64 = 2,
			x128 = 3,
			x256 = 4,
			x512 = 5,
			x1024 = 6,
			x2048 = 7,
			x4096 = 8,
		}

		public enum PostSmoothingIterations
		{
			x0 = 0,
			x1 = 1,
			x2 = 2,
			x3 = 3,
		}

		[System.Serializable]
		public class SetupTerrainResolution
		{
			public HeightmapResolution heightmapResolution = HeightmapResolution.x65;
			public SplatmapResolution splatmapResolution = SplatmapResolution.x256;
			public SplatmapResolution compositeSplatmapResolution = SplatmapResolution.x256;

			public PostSmoothingIterations postSmoothingIterations = PostSmoothingIterations.x1;
			public float splatStitchingPercentRadius = 0.02f;
		}

		[SerializeField]
		public SetupTerrainResolution terrainResolution = new SetupTerrainResolution();

		[System.Serializable]
		public class SetupTerrainProperties
		{
			public int lodPixelError = 5;
			public int compositeStartDistance = 1000;

			public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
			public ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

			public bool drawInstanced = true;
			public bool raytracingSupport = false;
		}

		[SerializeField]
		public SetupTerrainProperties terrainProperties = new SetupTerrainProperties();
	}
}