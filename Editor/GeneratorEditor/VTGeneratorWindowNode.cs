using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace RobProductions.VisualTerrain
{
	public class VTGeneratorWindowNode
	{
		public Rect rect;
		public string title;
		public bool isDragged;

		public GUIStyle style;

		public VTGeneratorWindowNode(Vector2 position, float width, float height, GUIStyle nodeStyle)
		{
			rect = new Rect(position.x, position.y, width, height);
			style = nodeStyle;
		}

		public void Drag(Vector2 delta)
		{
			rect.position += delta;
		}

		public void Draw()
		{
			GUI.Box(rect, title, style);
		}

		public bool ProcessEvents(Event e)
		{
			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 0)
					{
						if (rect.Contains(e.mousePosition))
						{
							isDragged = true;
							GUI.changed = true;
						}
						else
						{
							GUI.changed = true;
						}
					}
					break;
				case EventType.MouseUp:
					isDragged = false;
					break;
				case EventType.MouseDrag:
					if (e.button == 0 && isDragged)
					{
						Drag(e.delta);
						e.Use();
						return true;
					}
					break;
			}

			return false;
		}
	}
}

#endif