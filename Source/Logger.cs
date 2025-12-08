using System;
#if UNITY_5_3_OR_NEWER || UNITY_INCLUDE_TESTS
using UnityEngine;
#endif

namespace Disposable
{
internal static class Logger
{
#if UNITY_5_3_OR_NEWER || UNITY_INCLUDE_TESTS
	public static void LogError(string message) => Debug.LogError(message);
	public static void LogError(Exception exception)  => Debug.LogError(exception.Message);
	public static void LogWarning(string message) => Debug.LogWarning(message);
	public static void LogMessage(string message) => Debug.Log(message);
#else
	public static void LogError(string message)
	{
	}

	public static void LogError(Exception exception)
	{
	}

	public static void LogWarning(string message)
	{
	}

	public static void LogMessage(string message)
	{
	}
#endif
}
}