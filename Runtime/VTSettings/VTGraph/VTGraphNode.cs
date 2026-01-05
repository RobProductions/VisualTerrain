using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	[System.Serializable]
	public class VTGraphNode
	{
		public virtual string NodeTitle { get => "Unnamed Node"; }
		public virtual bool HasNodeProperties { get => false; }
		public virtual bool HasDisableButton { get => false; }
		public virtual bool SubGraphNode { get => false; }

		[SerializeField, SerializeReference]
		public VTGraphConnectionSlot[] inputConnections;
		[SerializeField, SerializeReference]
		public VTGraphConnectionSlot[] outputConnections;

		[field: SerializeField]
		public string CustomName { get; set; } = "";
		[field: SerializeField]
		public Vector2 NodePosition { get; set; } = Vector2.zero;
		[field: SerializeField]
		public bool IsExpanded { get; set; } = true;
		[field: SerializeField]
		public bool IsDisabled { get; set; } = false;

		[HideInInspector, System.NonSerialized]
		protected VTGraph parentGraph = null;

		public delegate void BeginEditNodeProperty(string description);
		public BeginEditNodeProperty beginEditNodePropertyEvent;
		public delegate void EndEditNodeProperty(VTGraphNode editedOnNode);
		public EndEditNodeProperty endEditNodePropertyEvent;

		//SETUP

		public void SetParentGraph(VTGraph v)
		{
			parentGraph = v;
		}

		public void SetupEmptyInputConnections(int inputCount, VTGraphConnectionSlot.SlotValueType defaultValueType)
		{
			inputConnections = new VTGraphConnectionSlot[inputCount];
			for(int i = 0; i < inputConnections.Length; i++)
			{
				inputConnections[i] = new VTGraphConnectionSlot(VTGraphConnectionSlot.NodeConnectionSlotType.Input, this, defaultValueType, i);
			}
		}

		public void SetupEmptyOutputConnections(int outputCount, VTGraphConnectionSlot.SlotValueType valueType)
		{
			outputConnections = new VTGraphConnectionSlot[outputCount];
			for (int i = 0; i < outputConnections.Length; i++)
			{
				outputConnections[i] = new VTGraphConnectionSlot(VTGraphConnectionSlot.NodeConnectionSlotType.Output, this, valueType, i);
			}
		}

		//PROCESSING

		/// <summary>
		/// Naively processes the node output value
		/// based on the input. The input values
		/// will not search for connections or process
		/// other nodes. That is left to VTGraph.ProcessNode().
		/// </summary>
		public virtual void ProcessNode(VTGraphProcessingSettings settings)
		{
			return;
		}

		/// <summary>
		/// Render properties in the setup view for this node.
		/// </summary>
		public virtual void RenderNodeProperties()
		{
			return;
		}

		//GETTERS

		public VTGraph GetParentGraph()
		{
			if(parentGraph == null)
			{
				VTLog.LogWarning("Parent Graph was null in GetParentGraph()...");
				return null;
			}

			return parentGraph;
		}

		/// <summary>
		/// Returns the first output connection.
		/// </summary>
		/// <returns></returns>
		public VTGraphConnectionSlot GetOutputConnection()
		{
			return GetOutputConnection(0);
		}

		/// <summary>
		/// Returns the output connection at a specific index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public VTGraphConnectionSlot GetOutputConnection(int index)
		{
			if(outputConnections.Length > index)
			{
				return outputConnections[index];
			}

			return null;
		}

		/// <summary>
		/// Returns the first input connection.
		/// </summary>
		/// <returns></returns>
		public VTGraphConnectionSlot GetInputConnection()
		{
			return GetInputConnection(0);
		}

		/// <summary>
		/// Returns the input connection at a specific index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public VTGraphConnectionSlot GetInputConnection(int index)
		{
			if (inputConnections.Length > index)
			{
				return inputConnections[index];
			}

			return null;
		}

		//PROPERTIES

		/// <summary>
		/// Draws a GUILayout FloatField and potentially modifies the ref
		/// input float if the user changes the value. 
		/// Also invokes begin and end edit node property events.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="baseValue"></param>
		protected void RenderFloatProperty(string propertyName, ref float baseValue, string tooltip = "")
		{
			var content = new GUIContent(propertyName, tooltip);
			var newValue = EditorGUILayout.FloatField(content, baseValue);
			if (newValue != baseValue)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				baseValue = newValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}

		/// <summary>
		/// Draws a GUILayout FloatField and potentially modifies the ref
		/// input float if the user changes the value but only within clamp range.
		/// Also invokes begin and end edit node property events.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="baseValue"></param>
		/// <param name="clampMin"></param>
		/// <param name="clampMax"></param>
		protected void RenderFloatPropertyWithClamp(string propertyName, ref float baseValue, float clampMin, float clampMax, string tooltip = "")
		{
			var content = new GUIContent(propertyName, tooltip);
			var newValue = Mathf.Clamp(EditorGUILayout.FloatField(content, baseValue), clampMin, clampMax);
			if (newValue != baseValue)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				baseValue = newValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}

		/// <summary>
		/// Draws a GUILayout IntField and potentially modifies the ref
		/// input int if the user changes the value.
		/// Also invokes begin and end edit node property events.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="baseValue"></param>
		protected void RenderIntProperty(string propertyName, ref int baseValue, string tooltip = "")
		{

			var content = new GUIContent(propertyName, tooltip);
			var newValue = EditorGUILayout.IntField(content, baseValue);
			if (newValue != baseValue)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				baseValue = newValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}

		/// <summary>
		/// Draws a GUILayout IntField and potentially modifies the ref
		/// input int if the user changes the value but only within clamp range.
		/// Also invokes begin and end edit node property events.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="baseValue"></param>
		protected void RenderIntPropertyWithClamp(string propertyName, ref int baseValue, int clampMin, int clampMax, string tooltip = "")
		{

			var content = new GUIContent(propertyName, tooltip);
			var newValue = Mathf.Clamp(EditorGUILayout.IntField(content, baseValue), clampMin, clampMax);
			if (newValue != baseValue)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				baseValue = newValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}

		/// <summary>
		/// Draws a GUILayout Toggle and potentially modifies the ref
		/// input bool if the user changes the value.
		/// Invokes begin and edit node property events.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="baseValue"></param>
		protected void RenderBoolProperty(string propertyName, ref bool baseValue, string tooltip = "")
		{
			var content = new GUIContent(propertyName, tooltip);
			bool newValue = EditorGUILayout.Toggle(content, baseValue);
			if (newValue != baseValue)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				baseValue = newValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}

		/// <summary>
		/// Draws a GUILayout Vector2 field and potentially modifies the ref
		/// input Vec2 if the user changes the value.
		/// Invokes begin and edit node property events.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="baseValue"></param>
		protected void RenderVector2Property(string propertyName, ref Vector2 baseValue, string tooltip = "")
		{
			var content = new GUIContent(propertyName, tooltip);
			Vector2 newValue = EditorGUILayout.Vector2Field(content, baseValue);
			if (newValue != baseValue)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				baseValue = newValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}

		/// <summary>
		/// Render an object property field specifically for GameObjects.
		/// Invokes begin and end edit node property events.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="baseValue"></param>
		/// <param name="allowSceneObjects"></param>
		protected void RenderGameObjectProperty(string propertyName, ref UnityEngine.GameObject baseValue,  bool allowSceneObjects)
		{
			var newValue = (GameObject)EditorGUILayout.ObjectField(propertyName, baseValue, typeof(GameObject), allowSceneObjects);
			if (newValue != baseValue)
			{
				beginEditNodePropertyEvent?.Invoke("Edited Node Property");
				baseValue = newValue;
				endEditNodePropertyEvent?.Invoke(this);
			}
		}
	}
}