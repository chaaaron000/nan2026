using System;
using System.Diagnostics;

public static class DebugConsole
{
    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    public static void Log(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.Log(message, context);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.LogWarning(message, context);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.LogError(message, context);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogException(Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogException(Exception exception, UnityEngine.Object context)
    {
        UnityEngine.Debug.LogException(exception, context);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    public static void LogFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(format, args);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    public static void LogWarningFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogWarningFormat(format, args);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogErrorFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogErrorFormat(format, args);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    public static void Assert(bool condition)
    {
        UnityEngine.Debug.Assert(condition);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("UNITY_EDITOR")]
    public static void Assert(bool condition, object message)
    {
        UnityEngine.Debug.Assert(condition, message);
    }
}
