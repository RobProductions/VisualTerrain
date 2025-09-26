using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobProductions.VisualTerrain.Runtime
{
	public static class VTLog
	{
		public static bool SuppressInfo { get; set; } = false;
		public static bool SuppressWarning { get; set; } = false;
		public static bool SuppressError { get; set; } = false;

		public enum LogLevel
		{
			Info,
			Warning,
			Error
		}

		public static void Log(string msg)
		{
			Log(msg, LogLevel.Info);
		}

		public static void LogWarning(string msg)
		{
			Log(msg, LogLevel.Warning);
		}

		public static void LogError(string msg)
		{
			Log(msg, LogLevel.Error);
		}

		public static void Log(string msg, LogLevel level)
		{
			if(level == LogLevel.Info && !SuppressInfo)
			{
				Debug.Log("[VT] " + msg);
			}
			else if (level == LogLevel.Warning && !SuppressWarning)
			{
				Debug.Log("[VT Warn] " + msg);
			}
			else if (level == LogLevel.Error && !SuppressError)
			{
				Debug.Log("[VT Err] " + msg);
			}
		}

	}
}