using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTGraphValueInterface
	{
		/// <summary>
		/// Get the heightmap output value from a graph
		/// in full quality.
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static Texture2D GetGraphHeightmapTexture(VTGraph graph)
		{
			return GetGraphHeightmapTexture(graph, new VTGraphProcessingSettings());
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
			VTGraphNodeTextureOutput outputNode = null;
			foreach(VTGraphNode node in allNodes)
			{
				if(node is VTGraphNodeTextureOutput)
				{
					outputNode = node as VTGraphNodeTextureOutput;
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