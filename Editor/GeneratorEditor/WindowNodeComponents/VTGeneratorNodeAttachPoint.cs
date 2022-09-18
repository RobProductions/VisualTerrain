using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace RobProductions.VisualTerrain
{
	public class VTGeneratorNodeAttachPoint
	{
		public Rect rect;

		public enum AttachPointType
		{
			Input = 0,
			Output = 1,
		}

		public AttachPointType attachType;

		public VTGeneratorWindowNode parentNode;

		[System.Serializable]
		public class AttachPointStyles
		{
			public GUIStyle currentStyle;
			public GUIStyle unconnectedStyle;
			public GUIStyle connectedStyle;

			public AttachPointStyles()
			{
				unconnectedStyle = new GUIStyle();
				unconnectedStyle.normal.background = EditorGUIUtility.Load("helpbox@2x") as Texture2D;
				unconnectedStyle.active.background = EditorGUIUtility.Load("helpbox@2x") as Texture2D;
				unconnectedStyle.border = new RectOffset(5, 5, 5, 5);

				connectedStyle = new GUIStyle();
				connectedStyle.normal.background = EditorGUIUtility.Load("RepaintDot") as Texture2D;
				connectedStyle.active.background = EditorGUIUtility.Load("RepaintDot") as Texture2D;
				connectedStyle.border = new RectOffset(5, 5, 5, 5);

				currentStyle = unconnectedStyle;
			}
		}

		public AttachPointStyles styles;

		[System.Serializable]
		public class AttachPointData
		{
			public bool hasConnection = false;
			public bool dragged = false;

			public System.Action<VTGeneratorNodeAttachPoint> OnClickConnectionPoint;
		}

		public AttachPointData data;


		public VTGeneratorNodeAttachPoint(VTGeneratorWindowNode node, AttachPointType type,
			System.Action<VTGeneratorNodeAttachPoint> OnClickConnectionPoint)
		{
			styles = new AttachPointStyles();
			data = new AttachPointData();

			this.parentNode = node;
			this.attachType = type;
			data.OnClickConnectionPoint = OnClickConnectionPoint;
			rect = new Rect(0, 0, 18f, 18f);
		}

		public void Draw()
		{
			rect.y = parentNode.rect.y + (parentNode.rect.height * 0.5f) - rect.height * 0.5f;

			switch (attachType)
			{
				case AttachPointType.Input:
					rect.x = parentNode.rect.x - rect.width + 20f;
					break;
				case AttachPointType.Output:
					rect.x = parentNode.rect.x + parentNode.rect.width - 20f;
					break;
			}

			if(parentNode.parentWindow.data.useDragAttachPoint)
			{
				GUI.Box(rect, "", styles.currentStyle);
			}
			else
			{
				if (GUI.Button(rect, "", styles.currentStyle))
				{
					if (data.OnClickConnectionPoint != null)
					{
						data.OnClickConnectionPoint(this);
					}
				}
			}
			
		}

		public bool ProcessAttachEvents(Event e)
		{
			switch (e.type)
			{
				case EventType.MouseDown:
					if(parentNode.parentWindow.data.useDragAttachPoint)
					{
						if (rect.Contains(e.mousePosition))
						{
							data.dragged = true;
							GUI.changed = true;
						}
						else
						{
							GUI.changed = true;
						}
					}
					break;
				case EventType.MouseDrag:
					if (e.button == 0 && data.dragged)
					{
						DragAttachPoint(e);
						e.Use();
						return true;
					}
					break;
				case EventType.MouseUp:
					data.dragged = false;
					if(e.button == 0 && parentNode.parentWindow.data.draggingConnection
						&& rect.Contains(e.mousePosition))
					{
						parentNode.parentWindow.AttemptCompleteDragConnection(this);
					}
					break;
			}

			return false;
		}

		void DragAttachPoint(Event e)
		{
			data.dragged = true;
			parentNode.parentWindow.DragConnectionPoint(this);
		}

		//SETTERS

		public void SetHasConnection(bool v)
		{
			data.hasConnection = v;

			styles.currentStyle = v ? styles.connectedStyle : styles.unconnectedStyle;
		}
	}
}

#endif