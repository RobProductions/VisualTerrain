using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphProcessingSettings
	{
		public enum TextureGenerationResolution
		{
			Full = 0,
			Half = 1,
			Quarter = 2,
			RestrictToSize = 3,
		}

		public TextureGenerationResolution textureGenResolution = TextureGenerationResolution.Full;

		public enum TextureOutputResolution
		{
			Full = 0,
			RestrictToSize = 1,
		}

		public TextureOutputResolution textureOutputResolution = TextureOutputResolution.Full;

		public VTGraphProcessingSettings(
			TextureGenerationResolution textureGenResolution = TextureGenerationResolution.Full, 
			TextureOutputResolution textureOutputResolution = TextureOutputResolution.Full)
		{
			this.textureGenResolution = textureGenResolution;
			this.textureOutputResolution = textureOutputResolution;
		}
	}
}