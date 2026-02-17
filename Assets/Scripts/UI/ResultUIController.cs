using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RecipeAboutLife.Managers;

namespace RecipeAboutLife.UI
{
    /// <summary>
    /// 결산 페이지 UI 컨트롤러
    /// 5명 NPC 완료 후 재화 결과 표시
    /// </summary>
    public class ResultUIController : MonoBehaviour
    {
        // ==========================================
        // Singleton Pattern
        // ==========================================

        private static ResultUIController _instance;
        public static ResultUIController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ResultUIController>();
                }
                return _instance;
            }
        }

        // ==========================================
        // UI References
        // ==========================================

        [Header("UI 참조")]
        [SerializeField]
        [Tooltip("결산 페이지 전체 패널")]
        private GameObject resultPanel;

        [SerializeField]
        [Tooltip("획득한 재화 표시 텍스트")]
        private TextMeshProUGUI totalRewardText;

        [SerializeField]
        [Tooltip("목표 재화 표시 텍스트")]
        private TextMeshProUGUI targetRewardText;

        [SerializeField]
        [Tooltip("성공/실패 메시지 텍스트")]
        private TextMeshProUGUI resultMessageText;

        [SerializeField]
        [Tooltip("확인 버튼")]
        private Button confirmButton;

        // ==========================================
        // State
        // ==========================================

        private bool isSuccess = false;

        // ==========================================
        // Events
        // ==========================================

        /// <summary>
        /// 확인 버튼 클릭 시 발생
        /// </summary>
        public System.Action<bool> OnConfirmClicked; // (isSuccess)

        // ==========================================
        // Lifecycle
        // ==========================================

        private void Awake()
        {
            // Singleton 설정
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 버튼 이벤트 등록
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            // 초기에는 숨김
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        // ==========================================
        // Public API
        // ==========================================

        /// <summary>
        /// 결산 페이지 표시
        /// </summary>
        /// <param name="success">재화 목표 달성 여부</param>
        public void Show(bool success)
        {
            isSuccess = success;

            // GameManager를 우선 데이터 소스로 사용 (ScoreManager는 폴백)
            int totalReward = 0;
            int targetReward = 0;

            if (GameManager.Instance != null)
            {
                totalReward = GameManager.Instance.TodayEarnings;
                targetReward = GameManager.Instance.GetCurrentDayGoal();
                Debug.Log($"[ResultUIController] GameManager에서 데이터 로드: {totalReward}/{targetReward}");
            }
            else if (ScoreManager.Instance != null)
            {
                totalReward = ScoreManager.Instance.GetTotalReward();
                targetReward = ScoreManager.Instance.GetTargetReward();
                Debug.Log($"[ResultUIController] ScoreManager에서 데이터 로드 (폴백): {totalReward}/{targetReward}");
            }
            else
            {
                Debug.LogError("[ResultUIController] GameManager와 ScoreManager를 모두 찾을 수 없습니다!");
                return;
            }

            // UI 업데이트
            if (totalRewardText != null)
            {
                totalRewardText.text = $"판매 금액  {totalReward}원";
            }

            if (targetRewardText != null)
            {
                targetRewardText.text = $"목표 금액  {targetReward}원";
            }

            if (resultMessageText != null)
            {
                if (success)
                {
                    resultMessageText.text = "목표 달성!";
                    resultMessageText.color = Color.black;
                }
                else
                {
                    resultMessageText.text = "목표 미달성";
                    resultMessageText.color = Color.black;
                }
            }

            // 패널 표시
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            Debug.Log($"[ResultUIController] 결산 페이지 표시 - 성공: {success}, 재화: {totalReward}/{targetReward}");
        }

        /// <summary>
        /// 결산 페이지 숨김
        /// </summary>
        public void Hide()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }

            Debug.Log("[ResultUIController] 결산 페이지 숨김");
        }

        // ==========================================
        // Button Events
        // ==========================================

        /// <summary>
        /// 확인 버튼 클릭 처리
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            Debug.Log($"[ResultUIController] 확인 버튼 클릭 - 성공: {isSuccess}");

            // 결산 페이지 숨김
            Hide();

            // 이벤트 발생 (StageStoryController가 구독)
            OnConfirmClicked?.Invoke(isSuccess);
        }

        // ==========================================
        // Debug
        // ==========================================

#if UNITY_EDITOR
        [ContextMenu("Test: Show Success")]
        private void TestShowSuccess()
        {
            Show(true);
        }

        [ContextMenu("Test: Show Fail")]
        private void TestShowFail()
        {
            Show(false);
        }

        [ContextMenu("Test: Hide")]
        private void TestHide()
        {
            Hide();
        }
#endif
    }
}
