using UnityEngine;
using UnityEngine.SceneManagement;
using RecipeAboutLife.Managers;
using RecipeAboutLife.Dialogue;
using RecipeAboutLife.Cooking;

/// <summary>
/// [디버그] 스테이지 스킵 버튼 - Day 강제 완료
/// GamePlayScene 진입 시 자동 생성됨 (오브젝트에 수동 추가 불필요)
/// 클릭 시: 목표 달성 상태로 설정 → 결산 화면 → 스토리 → 로비 이동
/// </summary>
public class DebugStageSkipButton : MonoBehaviour
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GamePlayScene")
        {
            var go = new GameObject("[Debug] StageSkipButton");
            go.AddComponent<DebugStageSkipButton>();
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 200, 250, 60), "Day 스킵 (성공)"))
        {
            ForceCompleteStage(true);
        }

        // 스토리 진행 중일 때 스킵 버튼 표시
        if (StageStoryController.Instance != null && StageStoryController.Instance.IsStoryPlaying)
        {
            if (GUI.Button(new Rect(10, 270, 250, 60), "스토리 스킵"))
            {
                StageStoryController.Instance.SkipStoryDialogue();
            }
        }

        // 환경 효과 토글 버튼
        string envLabel = DayEnvironmentEffect.Instance != null && DayEnvironmentEffect.Instance.isEnabled
            ? "환경효과 끄기" : "환경효과 켜기";
        if (GUI.Button(new Rect(10, 340, 250, 60), envLabel))
        {
            DayEnvironmentEffect.Instance?.Toggle();
        }
    }

    private void ForceCompleteStage(bool success)
    {
        Debug.Log($"[DebugSkip] 스테이지 강제 완료 시작 (success: {success})");

        // 1. GameManager 상태를 목표 달성으로 설정
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DebugForceCompleteDay();
        }
        else
        {
            Debug.LogError("[DebugSkip] GameManager.Instance가 null입니다!");
            return;
        }

        // 2. ScoreManager의 OnStageCompleted 이벤트 직접 발생
        //    → StageStoryController가 구독 중이므로 결산 흐름 시작
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnStageCompleted?.Invoke(success);
            Debug.Log("[DebugSkip] ScoreManager.OnStageCompleted 발생 완료");
        }
        else
        {
            Debug.LogError("[DebugSkip] ScoreManager.Instance가 null입니다!");
        }
    }
#endif
}
