using System;
using UnityEngine;

namespace Disposable
{
internal static class Logger
{
	public static void LogError(string message) => Debug.LogError(message);
	public static void LogError(Exception exception)  => Debug.LogError(exception.Message);
	public static void LogWarning(string message) => Debug.LogWarning(message);
	public static void LogMessage(string message) => Debug.Log(message);
}
}