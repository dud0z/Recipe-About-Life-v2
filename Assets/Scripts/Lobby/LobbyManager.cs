using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace RecipeAboutLife.Lobby
{
    /// <summary>
    /// 로비 매니저 - 핀 클릭 → 트럭 이동 + 페이드 인 → 씬 전환
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        [Header("트럭")]
        [SerializeField] private TruckController truck;

        [Header("스테이지 핀")]
        [SerializeField] private List<StagePin> stagePins = new List<StagePin>();

        [Header("트랜지션 설정")]
        [SerializeField] private float truckMoveDistance = 10f;
        [SerializeField] private float transitionDuration = 1f;
        [SerializeField] private string gamePlaySceneName = "GamePlayScene";

        private int selectedStage = -1;
        private bool isTransitioning = false;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);
        }

        private void Start()
        {
            // GameManager의 currentDay에 따라 핀 활성/비활성화
            if (GameManager.Instance != null)
            {
                int currentDay = GameManager.Instance.CurrentDay;
                for (int i = 0; i < stagePins.Count; i++)
                {
                    if (stagePins[i] != null)
                    {
                        stagePins[i].SetUnlocked(i + 1 <= currentDay);
                    }
                }
                Debug.Log($"[Lobby] 핀 활성화 갱신 - 현재 Day: {currentDay}");
            }

            // 씬 시작 시 페이드 아웃 (화면 밝아짐)
            StartCoroutine(FadeOutOnStart());
        }

        private IEnumerator FadeOutOnStart()
        {
            // 핀들 초기 색상을 검은색으로
            foreach (var pin in stagePins)
            {
                if (pin != null)
                {
                    pin.FadeToBlack(0f); // 즉시 검은색
                }
            }

            var fadeUI = UI.FadeUI.Instance;
            if (fadeUI != null)
            {
                fadeUI.SetBlack();
                yield return new WaitForSeconds(0.2f);

                // 화면과 핀 동시에 밝아짐
                fadeUI.FadeOut(0.5f);
                foreach (var pin in stagePins)
                {
                    if (pin != null)
                    {
                        pin.FadeToNormal(0.5f);
                    }
                }
            }
        }

        /// <summary>
        /// Button OnClick에서 호출 - 핀 클릭 시 트랜지션 시작
        /// </summary>
        public void OnPinButtonClicked(int stageIndex)
        {
            Debug.Log($"[Lobby] ★ 핀 버튼 클릭! Stage: {stageIndex}");

            if (isTransitioning)
            {
                Debug.Log("[Lobby] 이미 트랜지션 중");
                return;
            }

            selectedStage = stageIndex;
            StartCoroutine(TransitionToGamePlay());
        }

        private IEnumerator TransitionToGamePlay()
        {
            isTransitioning = true;
            Debug.Log("[Lobby] ▶ 트랜지션 시작");

            // 스테이지 저장
            PlayerPrefs.SetInt("SelectedStageIndex", selectedStage);
            PlayerPrefs.Save();

            // 트럭 이동 시작
            if (truck != null)
            {
                Debug.Log($"[Lobby] → 트럭 이동 (거리: {truckMoveDistance}, 시간: {transitionDuration}초)");
                truck.MoveRight(truckMoveDistance, transitionDuration);
            }
            else
            {
                Debug.LogWarning("[Lobby] 트럭이 연결되지 않았습니다!");
            }

            // 페이드 인 시작 (동시에)
            var fadeUI = UI.FadeUI.Instance;
            if (fadeUI != null)
            {
                Debug.Log($"[Lobby] → 페이드 인 (시간: {transitionDuration}초)");
                fadeUI.FadeIn(transitionDuration);
            }
            else
            {
                Debug.LogWarning("[Lobby] FadeUI가 없습니다!");
            }

            // 핀들을 FadeUI보다 빠르게 검은색으로 페이드 (핀이 페이드 효과 위에 보이는 문제 방지)
            foreach (var pin in stagePins)
            {
                if (pin != null)
                {
                    pin.FadeToBlack(transitionDuration * 0.5f);
                }
            }
            Debug.Log($"[Lobby] → 핀 페이드 (시간: {transitionDuration}초)");

            // 트랜지션 완료 대기
            yield return new WaitForSeconds(transitionDuration);
            yield return new WaitForSeconds(0.2f);

            // 씬 로드
            Debug.Log($"[Lobby] ✓ {gamePlaySceneName} 로드!");
            SceneManager.LoadScene(gamePlaySceneName);
        }

        public static int GetSelectedStageIndex()
        {
            return PlayerPrefs.GetInt("SelectedStageIndex", 1);
        }
    }
}
