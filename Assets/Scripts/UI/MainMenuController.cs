using UnityEngine;
using RecipeAboutLife.Cooking;

namespace RecipeAboutLife.UI
{
    /// <summary>
    /// 메인 메뉴 버튼 핸들러
    /// DontDestroyOnLoad 싱글톤(GameManager)을 런타임에 참조하여
    /// 씬 재로드 시 버튼 참조가 끊어지는 문제를 방지
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("인트로 스토리")]
        [SerializeField]
        [Tooltip("인트로 스토리 컨트롤러 (없으면 인트로 스킵)")]
        private IntroStoryController introStoryController;

        private void Start()
        {
            // 타이틀 BGM 재생 (day3BGM 사용)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(3);
            }
        }

        /// <summary>
        /// 게임 시작 버튼 클릭 → 인트로 스토리 재생 → 로비 씬 이동
        /// </summary>
        public void OnStartButtonClicked()
        {
            // 타이틀 BGM 정지
            AudioManager.Instance?.StopBGM();

            // Inspector 미할당 시 자동 탐색 (비활성 오브젝트 포함)
            if (introStoryController == null)
            {
                introStoryController = FindObjectOfType<IntroStoryController>(true);
                Debug.Log($"[MainMenuController] introStoryController 자동 탐색: {(introStoryController != null ? "성공" : "실패")}");
            }

            if (introStoryController != null)
            {
                introStoryController.PlayIntro(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.LoadLobbyScene();
                    }
                    else
                    {
                        Debug.LogError("[MainMenuController] GameManager.Instance가 null입니다!");
                    }
                });
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoadLobbyScene();
                }
                else
                {
                    Debug.LogError("[MainMenuController] GameManager.Instance가 null입니다!");
                }
            }
        }

        /// <summary>
        /// 설정 버튼 클릭 → 설정 팝업 표시
        /// </summary>
        public void OnSettingsButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OpenSetting();
            }
            else
            {
                Debug.LogError("[MainMenuController] GameManager.Instance가 null입니다!");
            }
        }

        /// <summary>
        /// 종료 버튼 클릭 → 게임 종료
        /// </summary>
        public void OnQuitButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
            else
            {
                Debug.LogError("[MainMenuController] GameManager.Instance가 null입니다!");
            }
        }
    }
}
