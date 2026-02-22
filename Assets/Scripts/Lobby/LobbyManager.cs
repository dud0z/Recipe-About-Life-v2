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
            // stagePins가 Inspector에서 설정되지 않은 경우 씬에서 자동 탐색
            if (stagePins == null || stagePins.Count == 0)
            {
                var rootObjects = gameObject.scene.GetRootGameObjects();
                List<StagePin> foundPins = new List<StagePin>();
                foreach (var root in rootObjects)
                {
                    foundPins.AddRange(root.GetComponentsInChildren<StagePin>(true));
                }
                stagePins = foundPins;
                stagePins.Sort((a, b) => a.StageIndex.CompareTo(b.StageIndex));
                Debug.Log($"[Lobby] StagePin 자동 탐색: {stagePins.Count}개 발견");
                foreach (var pin in stagePins)
                {
                    Debug.Log($"[Lobby]   - {pin.name} (StageIndex: {pin.StageIndex}, Active: {pin.gameObject.activeSelf})");
                }
            }

            // GameManager의 currentDay에 따라 핀 활성/비활성화
            if (GameManager.Instance != null)
            {
                int currentDay = GameManager.Instance.CurrentDay;
                for (int i = 0; i < stagePins.Count; i++)
                {
                    if (stagePins[i] != null)
                    {
                        bool unlocked = (stagePins[i].StageIndex <= currentDay);
                        stagePins[i].gameObject.SetActive(unlocked);
                        stagePins[i].SetUnlocked(unlocked);
                    }
                }
                Debug.Log($"[Lobby] 핀 활성화 갱신 - 현재 Day: {currentDay}");
            }

            // 씬 시작 시 페이드 아웃 (화면 밝아짐)
            StartCoroutine(FadeOutOnStart());
        }

        private IEnumerator FadeOutOnStart()
        {
            // 핀들 초기 색상을 검은색으로 (활성 핀만)
            foreach (var pin in stagePins)
            {
                if (pin != null && pin.gameObject.activeSelf)
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
                    if (pin != null && pin.gameObject.activeSelf)
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

            // 선택한 스테이지에 맞게 Day 설정 (목표 금액, 배경, 진행상황 초기화)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetDay(stageIndex);
            }

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
                if (pin != null && pin.gameObject.activeSelf)
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
