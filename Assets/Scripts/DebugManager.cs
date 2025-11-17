using UnityEngine;

/// <summary>
///     Centralized debug logging manager.
///     Toggle enableDebugLogs to turn all debug messages on/off throughout the project.
/// </summary>
public static class DebugManager
{
    // Set this to false to disable all debug logs
    public static bool enableDebugLogs = false;

    public static void Log(object message)
    {
        if (enableDebugLogs)
            Debug.Log(message);
    }

    public static void Log(object message, Object context)
    {
        if (enableDebugLogs)
            Debug.Log(message, context);
    }

    public static void LogWarning(object message)
    {
        if (enableDebugLogs)
            Debug.LogWarning(message);
    }

    public static void LogWarning(object message, Object context)
    {
        if (enableDebugLogs)
            Debug.LogWarning(message, context);
    }

    public static void LogError(object message)
    {
        if (enableDebugLogs)
            Debug.LogError(message);
    }

    public static void LogError(object message, Object context)
    {
        if (enableDebugLogs)
            Debug.LogError(message, context);
    }
}
