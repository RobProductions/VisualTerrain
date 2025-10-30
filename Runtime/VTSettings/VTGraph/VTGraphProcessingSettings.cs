using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphProcessingSettings
	{
		public int textureGenResolutionNumber = 256;

		public VTGraphProcessingSettings(
			int textureGenResolutionNumber = 256)
		{
			this.textureGenResolutionNumber = textureGenResolutionNumber;
		}
	}
}