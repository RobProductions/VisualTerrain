using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphProcessingSettings
	{
		public int textureGenResolutionNumber = 256;
		public bool thumbnailMode = false;

		public VTGraphProcessingSettings(
			int textureGenResolutionNumber = 256,
			bool thumbnailMode = false)
		{
			this.textureGenResolutionNumber = textureGenResolutionNumber;
			this.thumbnailMode = thumbnailMode;
		}
	}
}