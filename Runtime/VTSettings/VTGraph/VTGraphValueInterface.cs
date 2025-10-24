using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphValueInterface
	{
		/// <summary>
		/// Get the heightmap output value from a graph
		/// using the quality settings dictated by the asset.
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static Texture2D GetAssetHeightmapTexture(VTSettingsAsset asset)
		{
			var processingSettings = new VTGraphProcessingSettings(
				textureGenResolution: VTGraphProcessingSettings.TextureGenerationResolution.Full,
				textureOutputResolution: VTGraphProcessingSettings.TextureOutputResolution.Full,
				textureGenResolutionNumber: 1024
			);
			return GetGraphHeightmapTexture(asset.generationData.heightmapGraph, processingSettings);
		}

		/// <summary>
		/// Get the heightmap output value from a graph using the
		/// given processing settings.
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		public static Texture2D GetGraphHeightmapTexture(VTGraph graph, VTGraphProcessingSettings settings)
		{
			var allNodes = graph.nodeList;
			VTGraphNodeHeightOutput outputNode = null;
			foreach(VTGraphNode node in allNodes)
			{
				if(node is VTGraphNodeHeightOutput)
				{
					outputNode = node as VTGraphNodeHeightOutput;
				}
			}

			if(outputNode == null)
			{
				return null;
			}

			//Calculate the final value to use from this node
			graph.ProcessNode(outputNode, settings);

			//Return the final heightmap texture
			return outputNode.GetOutputConnection().GetTextureValue();
		}
	}
}