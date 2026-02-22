using System.Diagnostics;

/// <summary>
/// 릴리스 빌드에서 자동 제거되는 디버그 로거
/// [Conditional] 어트리뷰트로 UNITY_EDITOR/DEVELOPMENT_BUILD 외 빌드에서 호출 자체가 제거됨
/// </summary>
public static class DebugLogger
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        UnityEngine.Debug.LogWarning(message);
    }
}
