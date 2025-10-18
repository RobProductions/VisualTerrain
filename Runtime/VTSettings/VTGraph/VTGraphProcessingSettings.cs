using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphProcessingSettings
	{
		public enum TextureGenResolution
		{
			Full = 0,
			Half = 1,
			Quarter = 2,
			RestrictToSize = 3,
		}

		public TextureGenResolution textureGenResolution = TextureGenResolution.Full;
	}
}