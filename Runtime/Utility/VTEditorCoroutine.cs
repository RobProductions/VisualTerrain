#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobProductions.VisualTerrain.Runtime
{
	public class VTEditorCoroutine
	{
		private readonly Stack<IEnumerator> executionStack;

		VTEditorCoroutine(IEnumerator routine)
		{
			this.executionStack = new Stack<IEnumerator>();
			this.executionStack.Push(routine);
		}

		private void Start()
		{
			EditorApplication.update += this.Update;
		}

		public void Stop()
		{
			EditorApplication.update -= this.Update;
		}

		private void Update()
		{
			if (this.executionStack.Count > 0)
			{
				IEnumerator currentCoroutine = this.executionStack.Peek();
				bool isNext = currentCoroutine.MoveNext();
				if (isNext && currentCoroutine.Current is IEnumerator)
					this.executionStack.Push(currentCoroutine.Current as IEnumerator);
				else if (!isNext)
					this.executionStack.Pop();
			}
			else
			{
				this.Stop();
			}
		}

		//STATIC INTERACTIONS

		public static VTEditorCoroutine Start(IEnumerator routine)
		{
			VTEditorCoroutine coroutine = new VTEditorCoroutine(routine);
			coroutine.Start();
			return coroutine;
		}
	}
}

#endif