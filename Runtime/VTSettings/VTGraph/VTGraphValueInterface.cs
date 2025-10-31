using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphValueInterface
	{
		//HEIGHTMAP 

		/// <summary>
		/// Get the heightmap output value from a graph
		/// using the quality settings dictated by the asset.
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static Texture2D GetAssetHeightmapTexture(VTSettingsAsset asset, bool previewMode)
		{
			VTGraphProcessingSettings processingSettings = GenerateAssetProcessingSettings(asset, previewMode);
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

		//TEXTURE

		public struct SplatmapLayerContainer
		{
			public TerrainLayer layerData;
			public Texture2D layerSplatmap;
		}

		public static List<SplatmapLayerContainer> GetAssetSplatmapLayers(VTSettingsAsset asset, bool previewMode)
		{
			var processingSettings = GenerateAssetProcessingSettings(asset, previewMode);
			return GetGraphSplatmapLayers(asset.generationData.textureGraph, processingSettings);
		}

		public static List<SplatmapLayerContainer> GetGraphSplatmapLayers(VTGraph graph, VTGraphProcessingSettings settings)
		{
			var ret = new List<SplatmapLayerContainer>();
			var splatOutputNodes = new List<VTGraphNodeSplatLayerOutput>();
			foreach(VTGraphNode node in graph.nodeList)
			{
				if(node is VTGraphNodeSplatLayerOutput)
				{
					splatOutputNodes.Add(node as VTGraphNodeSplatLayerOutput);
				}
			}

			//Order the splat output nodes by y position and then by user value
			splatOutputNodes = splatOutputNodes.OrderBy(item => item.NodePosition.y).OrderBy(item => item.layerOrder).ToList();

			for(int i = 0; i < splatOutputNodes.Count; i++)
			{
				var thisSplatNode = splatOutputNodes[i];
				if(thisSplatNode.terrainLayer != null)
				{
					var splatLayer = new SplatmapLayerContainer();
					splatLayer.layerData = thisSplatNode.GetNodeTerrainLayer();

					graph.ProcessNode(thisSplatNode, settings);
					splatLayer.layerSplatmap = thisSplatNode.GetOutputConnection().GetTextureValue();

					ret.Add(splatLayer);
				}
			}

			return ret;
		}

		//UTILITY

		static VTGraphProcessingSettings GenerateAssetProcessingSettings(VTSettingsAsset asset, bool previewMode)
		{
			VTGraphProcessingSettings processingSettings;
			if (previewMode)
			{
				processingSettings = new VTGraphProcessingSettings(
					textureGenResolutionNumber: asset.setupData.processingSetup.preview.previewTextureGenResolution,
					thumbnailMode: false
				);
			}
			else
			{
				processingSettings = new VTGraphProcessingSettings(
					textureGenResolutionNumber: asset.setupData.processingSetup.texture.textureGenResolution,
					thumbnailMode: false
				);
			}

			return processingSettings;
		}
	}
}