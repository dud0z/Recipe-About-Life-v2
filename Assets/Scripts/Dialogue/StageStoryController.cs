using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RecipeAboutLife.Managers;
using RecipeAboutLife.NPC;

namespace RecipeAboutLife.Dialogue
{
    /// <summary>
    /// 스테이지 스토리 대화 컨트롤러
    ///
    /// 역할:
    /// 1. 모든 NPC 주문 완료 후 재화 조건 확인
    /// 2. 조건 달성 시 마지막 NPC(스토리 NPC)의 StoryAfterSummary 대화 트리거
    /// 3. 결산 UI가 추가되면 해당 위치에서 호출 (현재는 UI 없이 동작)
    ///
    /// ※ 기존 시스템 수정 없이 이벤트 구독으로만 동작
    /// </summary>
    public class StageStoryController : MonoBehaviour
    {
        // ==========================================
        // Singleton Pattern
        // ==========================================

        private static StageStoryController _instance;
        public static StageStoryController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<StageStoryController>();
                }
                return _instance;
            }
        }

        // ==========================================
        // Configuration
        // ==========================================

        [Header("스토리 NPC 설정")]
        [SerializeField]
        [Tooltip("스토리 NPC 정보 (ScriptableObject)")]
        private StoryNPCConfig storyNPCConfig;

        [Header("스테이지 대화 데이터")]
        [SerializeField]
        [Tooltip("스테이지별 대화 데이터 (AfterStory 포함)")]
        private List<StageDialogueData> stageDialogueDataList = new List<StageDialogueData>();

        [Header("현재 스테이지")]
        [SerializeField]
        [Tooltip("현재 스테이지 번호 (1, 2, 3...) - Start()에서 자동 로드")]
        private int currentStageID = 1;

        [Header("대화 설정")]
        [SerializeField]
        [Tooltip("StoryAfterSummary 대화 시작 전 대기 시간 (초)")]
        private float delayBeforeStoryDialogue = 1f;


        // ==========================================
        // State
        // ==========================================

        [Header("현재 상태 (Debug)")]
        [SerializeField]
        private bool isStoryDialogueTriggered = false;

        private Dialogue.NPCDialogueSet currentDialogueSet; // 현재 스토리 DialogueSet
        private Sprite currentNPCSprite; // 현재 스토리 NPC 스프라이트
        private Coroutine currentDialogueCoroutine; // 대화 코루틴

        // 입력 처리
        private bool waitingForInput = false;
        private bool inputReceived = false;

        // AfterStory 전용 NPC 진행 상태
        private bool isPlayingAfterStoryOnly = false;

        // 게임 클리어 상태 (Day3 완료 시 true)
        private bool isGameCleared = false;

        [Header("대화 표시 설정")]
        [SerializeField]
        [Tooltip("각 대화 라인 표시 시간 (초)")]
        private float lineDisplayTime = 3f;

        [SerializeField]
        [Tooltip("대화 라인 간 간격 (초)")]
        private float linePauseDuration = 0.5f;

        [Header("페이드 효과 설정")]
        [SerializeField]
        [Tooltip("페이드 효과 시간 (초)")]
        private float fadeDuration = 1f;

        [SerializeField]
        [Tooltip("'잠시만요' 텍스트 표시 시간 (초)")]
        private float waitTextDuration = 2f;

        // ==========================================
        // Events
        // ==========================================

        /// <summary>
        /// StoryAfterSummary 대화가 시작되었을 때
        /// </summary>
        public System.Action OnStoryDialogueStarted;

        /// <summary>
        /// StoryAfterSummary 대화가 종료되었을 때
        /// </summary>
        public System.Action OnStoryDialogueEnded;

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
        }

        private void Start()
        {
            // 로비에서 선택한 스테이지 인덱스 로드
            currentStageID = PlayerPrefs.GetInt("SelectedStageIndex", 1);
            DebugLogger.Log($"[StageStoryController] 선택된 스테이지 로드: {currentStageID}");
        }

        private void OnEnable()
        {
            // ScoreManager의 스테이지 완료 이벤트 구독
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnStageCompleted += OnStageCompleted;
            }
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnStageCompleted -= OnStageCompleted;
            }
        }

        private void Update()
        {
            // 대화 진행 중이고 입력 대기 중일 때만 체크
            if (!waitingForInput || inputReceived)
                return;

            // 모바일 터치 입력 감지
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                inputReceived = true;
            }
            // PC 마우스 클릭 입력 감지 (테스트용)
            else if (Input.GetMouseButtonDown(0))
            {
                inputReceived = true;
            }
        }

        // ==========================================
        // Stage Completion Handler
        // ==========================================

        /// <summary>
        /// 스테이지 완료 이벤트 처리
        /// ScoreManager에서 모든 NPC 완료 시 호출됨
        /// </summary>
        /// <param name="success">재화 목표 달성 여부</param>
        private void OnStageCompleted(bool success)
        {
            // GameManager의 데이터를 기준으로 성공 여부 재판정 (ScoreManager와 데이터 불일치 방지)
            if (GameManager.Instance != null)
            {
                success = GameManager.Instance.IsDayGoalAchieved();
            }

            DebugLogger.Log($"[StageStoryController] 스테이지 완료! 성공: {success}, Stage ID: {currentStageID}");

            // 페이드 인 후 결산 UI 표시
            if (UI.FadeUI.Instance != null)
            {
                UI.FadeUI.Instance.FadeIn(fadeDuration, () =>
                {
                    // 페이드 인 완료 후 결산 UI 표시
                    ShowResultUI(success);
                });
            }
            else
            {
                // FadeUI 없으면 바로 결산 UI 표시
                DebugLogger.LogWarning("[StageStoryController] FadeUI를 찾을 수 없습니다. 페이드 효과 없이 진행");
                ShowResultUI(success);
            }
        }

        /// <summary>
        /// 결산 UI 표시
        /// </summary>
        private void ShowResultUI(bool success)
        {
            UI.ResultUIController resultUI = UI.ResultUIController.Instance;
            if (resultUI != null)
            {
                // 결산 페이지 표시
                resultUI.Show(success);

                // FadePanel의 raycast 차단 해제 (결산 버튼 클릭 가능하도록)
                if (UI.FadeUI.Instance != null)
                    UI.FadeUI.Instance.SetBlocksRaycasts(false);

                // 확인 버튼 클릭 이벤트 구독
                resultUI.OnConfirmClicked += OnResultUIConfirmed;

                DebugLogger.Log("[StageStoryController] 결산 UI 표시");
            }
            else
            {
                // 결산 UI 없으면 바로 페이드 전환 시작 (StartNextDay 미호출이므로 현재 Day 그대로 사용)
                int currentDay = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
                DebugLogger.LogWarning("[StageStoryController] ResultUIController를 찾을 수 없습니다. 페이드 효과 없이 스토리 대화 진행");
                StartCoroutine(TransitionToStoryDialogue(success, currentDay));
            }
        }

        /// <summary>
        /// 결산 UI 확인 버튼 클릭 시 호출
        /// </summary>
        /// <param name="success">재화 목표 달성 여부</param>
        private void OnResultUIConfirmed(bool success)
        {
            DebugLogger.Log($"[StageStoryController] 결산 UI 확인 버튼 클릭 - 성공: {success}");

            // FadePanel의 raycast 차단 복원
            if (UI.FadeUI.Instance != null)
                UI.FadeUI.Instance.SetBlocksRaycasts(true);

            // 이벤트 구독 해제
            UI.ResultUIController resultUI = UI.ResultUIController.Instance;
            if (resultUI != null)
            {
                resultUI.OnConfirmClicked -= OnResultUIConfirmed;
            }

            // 실패 시: Day 재시작 + 로비 이동
            if (!success)
            {
                DebugLogger.Log("[StageStoryController] 목표 미달성! Day 재시작 후 로비로 이동");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartDay();
                }
                StartCoroutine(TransitionToLobbyOnFail());
                return;
            }

            // 성공 시: 클리어한 Day 저장 후 다음 Day로 진행
            int clearedDay = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentDay < GameManager.Instance.maxDay)
                {
                    GameManager.Instance.StartNextDay();
                    isGameCleared = false;
                    DebugLogger.Log($"[StageStoryController] 다음 Day로 진행: Day {GameManager.Instance.CurrentDay}");
                }
                else
                {
                    isGameCleared = true;
                    DebugLogger.Log("[StageStoryController] 모든 Day 완료! 게임 클리어!");
                }
            }

            // 성공 시 검은 화면 유지한 채로 GuideText → 페이드 아웃 → 스토리 대화 시작
            StartCoroutine(TransitionToStoryDialogue(success, clearedDay));
        }

        /// <summary>
        /// 실패 시 로비로 이동 (페이드 아웃 없이 바로)
        /// </summary>
        private System.Collections.IEnumerator TransitionToLobbyOnFail()
        {
            UI.FadeUI fadeUI = UI.FadeUI.Instance;

            // 결산 UI 숨김
            UI.ResultUIController resultUI = UI.ResultUIController.Instance;
            if (resultUI != null)
            {
                resultUI.Hide();
            }

            // 잠시 대기 (검은 화면 상태)
            yield return new WaitForSeconds(0.5f);

            // "다시 도전하세요!" 텍스트 표시
            if (fadeUI != null)
            {
                fadeUI.ShowText("다시 도전하세요!");
                yield return new WaitForSeconds(1.5f);
                fadeUI.HideText();
                yield return new WaitForSeconds(0.5f);
            }

            // 로비 씬 로드
            DebugLogger.Log("[StageStoryController] 실패 → 로비 씬 로드");
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        /// <summary>
        /// 스토리 대화로 전환 (검은 화면 상태에서 GuideText 표시 후 페이드 아웃)
        /// </summary>
        private System.Collections.IEnumerator TransitionToStoryDialogue(bool success, int clearedDay)
        {
            UI.FadeUI fadeUI = UI.FadeUI.Instance;

            if (fadeUI != null)
            {
                DebugLogger.Log("[StageStoryController] === 스토리 대화 전환 시작 (검은 화면 상태) ===");

                // 1. 결산 UI 숨김 (검은 화면 유지)
                UI.ResultUIController resultUI = UI.ResultUIController.Instance;
                if (resultUI != null)
                {
                    resultUI.Hide();
                    DebugLogger.Log("[StageStoryController] 1. 결산 UI 숨김 (검은 화면 유지)");
                }

                // 1초 대기
                yield return new WaitForSeconds(1f);
                DebugLogger.Log("[StageStoryController] 결산 UI 숨김 후 1초 대기 완료");

                // 2. "잠시만요" 텍스트 표시 (검은 화면에서)
                DebugLogger.Log($"[StageStoryController] 2. GuideText 표시 시작 ('{waitTextDuration}'초)");
                fadeUI.ShowText("잠시만요");
                yield return new WaitForSeconds(waitTextDuration);

                // 3. 텍스트 숨김
                fadeUI.HideText();
                DebugLogger.Log("[StageStoryController] 3. GuideText 숨김");

                // 텍스트 표시 후 1초 대기
                yield return new WaitForSeconds(1f);
                DebugLogger.Log("[StageStoryController] GuideText 후 1초 대기 완료");

                // 4. FramePanel, NPCDialoguePanel, PlayerDialoguePanel 표시
                // (아직 검은 화면 상태이므로 안 보임)
                if (UI.FramePanelUI.Instance != null)
                {
                    UI.FramePanelUI.Instance.Show(clearedDay);
                    DebugLogger.Log($"[StageStoryController] 4. FramePanel 표시 - Day {clearedDay} 배경 (검은 화면에서)");
                }

                // 대화 데이터 준비
                PrepareStoryDialogueData(success);

                // 대화 패널을 빈 텍스트로 미리 표시 (페이드 아웃 전)
                if (UI.NPCDialogueUI.Instance != null && currentNPCSprite != null)
                {
                    string npcName = currentDialogueSet != null ? currentDialogueSet.npcDisplayName : null;
                    UI.NPCDialogueUI.Instance.Show("", npcName, currentNPCSprite);
                    DebugLogger.Log("[StageStoryController] NPC 대화 패널 표시 (빈 텍스트)");
                }

                if (UI.PlayerDialogueUI.Instance != null)
                {
                    UI.PlayerDialogueUI.Instance.Show("");
                    DebugLogger.Log("[StageStoryController] Player 대화 패널 표시 (빈 텍스트)");
                }

                // 5. 페이드 아웃 (화면 밝아지면서 Panel들이 보임)
                DebugLogger.Log("[StageStoryController] 5. 페이드 아웃 시작");
                bool fadeOutComplete = false;
                fadeUI.FadeOut(fadeDuration, () => fadeOutComplete = true);

                // 페이드 아웃 완료 대기
                while (!fadeOutComplete)
                {
                    yield return null;
                }
                DebugLogger.Log("[StageStoryController] 페이드 아웃 완료");

                // 페이드 완료 후 1초 대기
                yield return new WaitForSeconds(1f);
                DebugLogger.Log("[StageStoryController] 페이드 아웃 후 1초 대기 완료");
            }
            else
            {
                // FadeUI 없으면 바로 결산 UI 숨김
                DebugLogger.LogWarning("[StageStoryController] FadeUI를 찾을 수 없습니다. 페이드 효과 없이 진행");
                UI.ResultUIController resultUI = UI.ResultUIController.Instance;
                if (resultUI != null)
                {
                    resultUI.Hide();
                }

                // FramePanel 표시
                if (UI.FramePanelUI.Instance != null)
                {
                    UI.FramePanelUI.Instance.Show();
                }

                PrepareStoryDialogueData(success);
            }

            // 6. 스토리 대화 시작
            DebugLogger.Log("[StageStoryController] 6. 스토리 대화 시작");
            StartStoryDialogue();
        }

        /// <summary>
        /// 스토리 대화 데이터 준비
        /// </summary>
        /// <param name="success">재화 목표 달성 여부</param>
        /// <param name="useAfterStoryOnly">AfterStory 전용 NPC 사용 여부 (기본: false)</param>
        private void PrepareStoryDialogueData(bool success, bool useAfterStoryOnly = false)
        {
            // StoryNPCConfig 확인
            if (storyNPCConfig == null)
            {
                Debug.LogError("[StageStoryController] StoryNPCConfig가 설정되지 않았습니다!");
                return;
            }

            // 재화 조건 달성 확인
            if (!success)
            {
                DebugLogger.Log("[StageStoryController] 재화 목표 미달성. StoryAfterSummary 대화를 실행하지 않습니다.");
                return;
            }

            if (useAfterStoryOnly)
            {
                // AfterStory 전용 NPC 사용 (Stage 3의 Ajeossi)
                if (!storyNPCConfig.HasStoryNPCForStage(currentStageID, afterStoryOnly: true))
                {
                    DebugLogger.LogWarning($"[StageStoryController] Stage {currentStageID}에 AfterStory 전용 NPC가 없습니다!");
                    return;
                }

                currentDialogueSet = storyNPCConfig.GetDialogueSetForStage(currentStageID, afterStoryOnly: true);
                currentNPCSprite = storyNPCConfig.GetSpriteForStage(currentStageID, afterStoryOnly: true);
                DebugLogger.Log($"[StageStoryController] ✅ AfterStory 전용 NPC 사용 (추가 AfterStory)");
            }
            else
            {
                // 일반 스토리 NPC 사용 (5번째 손님)
                if (!storyNPCConfig.HasStoryNPCForStage(currentStageID, afterStoryOnly: false))
                {
                    DebugLogger.LogWarning($"[StageStoryController] Stage {currentStageID}에 스토리 NPC가 설정되지 않았습니다!");
                    return;
                }

                currentDialogueSet = storyNPCConfig.GetDialogueSetForStage(currentStageID, afterStoryOnly: false);
                currentNPCSprite = storyNPCConfig.GetSpriteForStage(currentStageID, afterStoryOnly: false);
                DebugLogger.Log($"[StageStoryController] ✅ 일반 스토리 NPC 사용 (5번째 손님)");
            }

            // DialogueSet 확인
            if (currentDialogueSet == null)
            {
                Debug.LogError($"[StageStoryController] Stage {currentStageID}의 DialogueSet을 찾을 수 없습니다!");
                return;
            }

            // Sprite 확인
            if (currentNPCSprite == null)
            {
                Debug.LogError($"[StageStoryController] Stage {currentStageID}의 NPC Sprite를 찾을 수 없습니다!");
                return;
            }

            DebugLogger.Log($"[StageStoryController] ✅ 스토리 대화 데이터 준비 완료");
            DebugLogger.Log($"  - Stage: {currentStageID}");
            DebugLogger.Log($"  - NPC: {currentDialogueSet.npcID}");
            DebugLogger.Log($"  - AfterStory 전용: {useAfterStoryOnly}");
            DebugLogger.Log($"  - 재화 달성: {success}");

            isStoryDialogueTriggered = true;
        }

        /// <summary>
        /// 스토리 대화 시작 (데이터 준비 완료 후)
        /// </summary>
        private void StartStoryDialogue()
        {
            // DialogueSet 확인
            if (currentDialogueSet == null)
            {
                Debug.LogError("[StageStoryController] DialogueSet이 없습니다!");
                return;
            }

            // StoryAfterSummary 대화 가져오기
            DialogueLine[] dialogueLines = currentDialogueSet.GetDialogueLines(DialogueType.StoryAfterSummary);
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                DebugLogger.LogWarning($"[StageStoryController] {currentDialogueSet.npcID}에 StoryAfterSummary 대화가 없습니다!");
                isStoryDialogueTriggered = false;
                return;
            }

            DebugLogger.Log($"[StageStoryController] StoryAfterSummary 대화 시작! NPC: {currentDialogueSet.npcID}");

            // 이벤트 발생
            OnStoryDialogueStarted?.Invoke();

            // 대화 코루틴 시작
            currentDialogueCoroutine = StartCoroutine(PlayStoryDialogueCoroutine(dialogueLines));
        }


        /// <summary>
        /// 스토리 대화 재생 코루틴
        /// </summary>
        private System.Collections.IEnumerator PlayStoryDialogueCoroutine(DialogueLine[] lines)
        {
            // 각 대화 라인 순회
            for (int i = 0; i < lines.Length; i++)
            {
                DialogueLine line = lines[i];

                // 대화 라인 표시
                DisplayStoryDialogueLine(line);

                // 터치 입력 대기 (최대 표시 시간까지)
                float displayTime = line.displayDuration > 0 ? line.displayDuration : lineDisplayTime;
                yield return WaitForInputOrTime(displayTime);

                // 마지막 라인이 아니면 짧은 간격
                if (i < lines.Length - 1)
                {
                    yield return new WaitForSeconds(linePauseDuration);
                }
            }

            // 대화 종료
            OnStoryDialogueCompleted();
        }

        /// <summary>
        /// 터치 입력 또는 시간 대기
        /// </summary>
        private System.Collections.IEnumerator WaitForInputOrTime(float maxTime)
        {
            waitingForInput = true;
            inputReceived = false;

            float elapsedTime = 0f;

            while (elapsedTime < maxTime && !inputReceived)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            waitingForInput = false;
            inputReceived = false;
        }

        /// <summary>
        /// 스토리 대화 라인 표시
        /// </summary>
        private void DisplayStoryDialogueLine(DialogueLine line)
        {
            // Speaker에 따라 다른 UI 사용
            switch (line.speaker)
            {
                case SpeakerType.NPC:
                    DisplayNPCDialogue(line.text);
                    break;

                case SpeakerType.Player:
                    DisplayPlayerDialogue(line.text);
                    break;

                case SpeakerType.System:
                    DisplayNPCDialogue(line.text);
                    break;
            }

            DebugLogger.Log($"[StageStoryController] [{line.speaker}] {line.text}");
        }

        /// <summary>
        /// NPC 대화 표시
        /// </summary>
        private void DisplayNPCDialogue(string text)
        {
            // NPC 대화 UI 표시
            if (UI.NPCDialogueUI.Instance != null)
            {
                string npcName = currentDialogueSet != null ? currentDialogueSet.npcDisplayName : null;
                UI.NPCDialogueUI.Instance.Show(text, npcName, currentNPCSprite);
            }
            else
            {
                DebugLogger.LogWarning("[StageStoryController] NPCDialogueUI를 찾을 수 없습니다!");
            }

            // Player 대화 UI는 항상 빈 텍스트로 표시
            if (UI.PlayerDialogueUI.Instance != null)
            {
                UI.PlayerDialogueUI.Instance.Show("");
            }
        }

        /// <summary>
        /// Player 대화 표시
        /// </summary>
        private void DisplayPlayerDialogue(string text)
        {
            // Player 대화 UI 표시
            if (UI.PlayerDialogueUI.Instance != null)
            {
                UI.PlayerDialogueUI.Instance.Show(text);
            }
            else
            {
                DebugLogger.LogWarning("[StageStoryController] PlayerDialogueUI를 찾을 수 없습니다!");
            }

            // NPC 대화 UI는 빈 텍스트로 표시 (NPC 이름과 이미지는 유지)
            if (UI.NPCDialogueUI.Instance != null)
            {
                string npcName = currentDialogueSet != null ? currentDialogueSet.npcDisplayName : null;
                UI.NPCDialogueUI.Instance.Show("", npcName, currentNPCSprite);
            }
        }

        /// <summary>
        /// 스토리 대화 완료 처리
        /// </summary>
        private void OnStoryDialogueCompleted()
        {
            DebugLogger.Log("[StageStoryController] StoryAfterSummary 대화 종료!");

            // 데이터 정리
            currentDialogueSet = null;
            currentNPCSprite = null;
            currentDialogueCoroutine = null;

            // 이벤트 발생
            OnStoryDialogueEnded?.Invoke();

            // AfterStory 전용 NPC를 이미 진행했는지 확인
            if (isPlayingAfterStoryOnly)
            {
                // Ajeossi AfterStory 완료 → 편지+독백 연출 확인
                DebugLogger.Log("[StageStoryController] AfterStory 전용 NPC 완료");
                isPlayingAfterStoryOnly = false;

                // StageDialogueData에 AfterStory 연출이 있는지 확인
                StageDialogueData stageData = GetCurrentStageDialogueData();
                if (stageData != null && stageData.HasAfterStory())
                {
                    // 편지 + 독백 연출 시작
                    DebugLogger.Log("[StageStoryController] ✅ AfterStory 연출 (편지+독백) 시작");
                    StartCoroutine(PlayAfterStorySequence());
                }
                else
                {
                    // AfterStory 연출 없음 → 바로 로비 이동
                    DebugLogger.Log("[StageStoryController] AfterStory 연출 없음. 로비로 이동");
                    StartCoroutine(TransitionToLobbyCoroutine());
                }
                return;
            }

            // AfterStory 전용 NPC가 있는지 확인 (Stage 3의 Ajeossi)
            bool hasAfterStoryOnlyNPC = storyNPCConfig != null &&
                                         storyNPCConfig.HasStoryNPCForStage(currentStageID, afterStoryOnly: true);

            if (hasAfterStoryOnlyNPC)
            {
                // 추가 AfterStory 진행
                DebugLogger.Log("[StageStoryController] ✅ AfterStory 전용 NPC가 있어서 추가 대화 진행");
                isPlayingAfterStoryOnly = true;
                StartCoroutine(TransitionToAdditionalAfterStoryCoroutine());
            }
            else
            {
                // AfterStory 전용 NPC 없음 → 편지+독백 연출 확인
                StageDialogueData stageData = GetCurrentStageDialogueData();
                if (stageData != null && stageData.HasAfterStory())
                {
                    // 편지 + 독백 연출 시작
                    DebugLogger.Log("[StageStoryController] ✅ AfterStory 연출 (편지+독백) 시작");
                    StartCoroutine(PlayAfterStorySequence());
                }
                else
                {
                    // 로비 이동 시퀀스 시작 (패널은 페이드 인이 완료될 때까지 유지)
                    DebugLogger.Log("[StageStoryController] AfterStory 전용 NPC 없음. 로비로 이동");
                    StartCoroutine(TransitionToLobbyCoroutine());
                }
            }
        }

        /// <summary>
        /// 추가 AfterStory로 전환 (Stage 3의 Ajeossi)
        /// </summary>
        private System.Collections.IEnumerator TransitionToAdditionalAfterStoryCoroutine()
        {
            UI.FadeUI fadeUI = UI.FadeUI.Instance;

            if (fadeUI != null)
            {
                DebugLogger.Log("[StageStoryController] === 추가 AfterStory 전환 시작 ===");

                // 1. 페이드 인 (검은 화면)
                DebugLogger.Log("[StageStoryController] 1. 페이드 인 시작");
                bool fadeInComplete = false;
                fadeUI.FadeIn(fadeDuration, () => fadeInComplete = true);

                while (!fadeInComplete)
                {
                    yield return null;
                }
                DebugLogger.Log("[StageStoryController] 페이드 인 완료");

                yield return new WaitForSeconds(1f);

                // 2. 기존 패널 숨기기
                if (UI.NPCDialogueUI.Instance != null)
                {
                    UI.NPCDialogueUI.Instance.Hide();
                }
                if (UI.PlayerDialogueUI.Instance != null)
                {
                    UI.PlayerDialogueUI.Instance.Hide();
                }

                // 3. "오랜만이다!" 텍스트 표시
                DebugLogger.Log("[StageStoryController] 2. GuideText '오랜만이다!' 표시");
                fadeUI.ShowText("오랜만이다!");
                yield return new WaitForSeconds(waitTextDuration);

                // 4. 텍스트 숨김
                fadeUI.HideText();
                DebugLogger.Log("[StageStoryController] 3. GuideText 숨김");

                yield return new WaitForSeconds(1f);

                // 5. AfterStory 전용 NPC 데이터 준비
                PrepareStoryDialogueData(true, useAfterStoryOnly: true);

                // 6. 대화 패널을 빈 텍스트로 미리 표시
                if (UI.NPCDialogueUI.Instance != null && currentNPCSprite != null)
                {
                    string npcName = currentDialogueSet != null ? currentDialogueSet.npcDisplayName : null;
                    UI.NPCDialogueUI.Instance.Show("", npcName, currentNPCSprite);
                }

                if (UI.PlayerDialogueUI.Instance != null)
                {
                    UI.PlayerDialogueUI.Instance.Show("");
                }

                // 7. 페이드 아웃
                DebugLogger.Log("[StageStoryController] 4. 페이드 아웃 시작");
                bool fadeOutComplete = false;
                fadeUI.FadeOut(fadeDuration, () => fadeOutComplete = true);

                while (!fadeOutComplete)
                {
                    yield return null;
                }
                DebugLogger.Log("[StageStoryController] 페이드 아웃 완료");

                yield return new WaitForSeconds(1f);
            }
            else
            {
                PrepareStoryDialogueData(true, useAfterStoryOnly: true);
            }

            // 8. 추가 AfterStory 대화 시작
            DebugLogger.Log("[StageStoryController] 5. 추가 AfterStory 대화 시작");
            StartStoryDialogue();
        }

        /// <summary>
        /// 로비로 이동 (페이드 인 → 텍스트 표시 → 터치 대기 → 로비 이동)
        /// </summary>
        private System.Collections.IEnumerator TransitionToLobbyCoroutine()
        {
            UI.FadeUI fadeUI = UI.FadeUI.Instance;

            if (fadeUI != null)
            {
                DebugLogger.Log("[StageStoryController] === 로비 이동 시퀀스 시작 ===");

                // 1. 페이드 인 (검은 화면)
                DebugLogger.Log("[StageStoryController] 1. 페이드 인 시작");
                bool fadeInComplete = false;
                fadeUI.FadeIn(fadeDuration, () => fadeInComplete = true);

                // 페이드 인 완료 대기
                while (!fadeInComplete)
                {
                    yield return null;
                }
                DebugLogger.Log("[StageStoryController] 페이드 인 완료");

                // 페이드 완료 후 1초 대기
                yield return new WaitForSeconds(1f);
                DebugLogger.Log("[StageStoryController] 페이드 인 후 1초 대기 완료");

                // 패널들 숨기기 (페이드 인이 완료되었으므로)
                if (UI.NPCDialogueUI.Instance != null)
                {
                    UI.NPCDialogueUI.Instance.Hide();
                    DebugLogger.Log("[StageStoryController] NPC 대화 UI 숨김");
                }
                if (UI.PlayerDialogueUI.Instance != null)
                {
                    UI.PlayerDialogueUI.Instance.Hide();
                    DebugLogger.Log("[StageStoryController] Player 대화 UI 숨김");
                }
                if (UI.FramePanelUI.Instance != null)
                {
                    UI.FramePanelUI.Instance.Hide();
                    DebugLogger.Log("[StageStoryController] FramePanel 숨김");
                }

                // 2. Day 정보 또는 "To be continued.." 텍스트 표시
                string transitionText = "로비로";
                if (GameManager.Instance != null)
                {
                    if (isGameCleared)
                    {
                        transitionText = "To be continued..";
                    }
                    else
                    {
                        transitionText = $"Day {GameManager.Instance.CurrentDay}";
                    }
                }
                DebugLogger.Log($"[StageStoryController] 2. '{transitionText}' 텍스트 표시");
                fadeUI.ShowText(transitionText);

                // 3. 3초 대기 (클릭 대기 대신)
                DebugLogger.Log("[StageStoryController] 3. 3초 대기 중...");
                yield return new WaitForSeconds(3f);
                DebugLogger.Log("[StageStoryController] 3초 대기 완료!");

                // 4. 텍스트 숨김
                fadeUI.HideText();

                // 추가 0.5초 대기
                yield return new WaitForSeconds(0.5f);

                // 5. 씬 로드
                if (isGameCleared)
                {
                    DebugLogger.Log("[StageStoryController] 4. 메인 메뉴 씬 로드 (게임 클리어)");
                    LoadMainMenuScene();
                }
                else
                {
                    DebugLogger.Log("[StageStoryController] 4. 로비 씬 로드");
                    LoadLobbyScene();
                }
            }
            else
            {
                DebugLogger.LogWarning("[StageStoryController] FadeUI를 찾을 수 없습니다. 바로 이동");
                if (isGameCleared)
                    LoadMainMenuScene();
                else
                    LoadLobbyScene();
            }
        }

        /// <summary>
        /// 로비 씬 로드
        /// </summary>
        private void LoadLobbyScene()
        {
            DebugLogger.Log("[StageStoryController] 로비 씬으로 이동!");
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        /// <summary>
        /// 메인 메뉴 씬 로드 (Day3 클리어 시)
        /// </summary>
        private void LoadMainMenuScene()
        {
            DebugLogger.Log("[StageStoryController] 메인 메뉴 씬으로 이동!");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }

        /// <summary>
        /// 현재 스테이지의 StageDialogueData 가져오기
        /// </summary>
        private StageDialogueData GetCurrentStageDialogueData()
        {
            if (stageDialogueDataList == null || stageDialogueDataList.Count == 0)
            {
                return null;
            }

            return stageDialogueDataList.FirstOrDefault(s => s.stageID == currentStageID);
        }

        // ==========================================
        // AfterStory 연출 (편지 + 독백)
        // ==========================================

        /// <summary>
        /// AfterStory 연출 시작 (편지 이미지 → 독백)
        /// </summary>
        private System.Collections.IEnumerator PlayAfterStorySequence()
        {
            StageDialogueData stageData = GetCurrentStageDialogueData();
            if (stageData == null || !stageData.HasAfterStory())
            {
                DebugLogger.LogWarning("[StageStoryController] AfterStory 데이터가 없습니다!");
                yield break;
            }

            UI.FadeUI fadeUI = UI.FadeUI.Instance;
            if (fadeUI == null)
            {
                Debug.LogError("[StageStoryController] FadeUI를 찾을 수 없습니다!");
                yield break;
            }

            DebugLogger.Log("[StageStoryController] === AfterStory 연출 시작 ===");

            // 1. 페이드 인 (검은 화면)
            DebugLogger.Log("[StageStoryController] 1. 페이드 인 시작");
            bool fadeInComplete = false;
            fadeUI.FadeIn(fadeDuration, () => fadeInComplete = true);

            while (!fadeInComplete)
            {
                yield return null;
            }
            DebugLogger.Log("[StageStoryController] 페이드 인 완료");

            yield return new WaitForSeconds(0.5f);

            // 대화 패널 숨기기
            if (UI.NPCDialogueUI.Instance != null)
            {
                UI.NPCDialogueUI.Instance.Hide();
            }
            if (UI.PlayerDialogueUI.Instance != null)
            {
                UI.PlayerDialogueUI.Instance.Hide();
            }

            // 2. 편지 이미지 표시
            DebugLogger.Log("[StageStoryController] 2. 편지 이미지 표시");
            Sprite letterImage = stageData.GetAfterStoryImage();
            fadeUI.ShowImage(letterImage);

            // 3. 3초 대기 (클릭 대기 대신)
            DebugLogger.Log("[StageStoryController] 3. 3초 대기 중...");
            yield return new WaitForSeconds(3f);
            DebugLogger.Log("[StageStoryController] 3초 대기 완료!");

            // 4. 편지 이미지 숨김
            fadeUI.HideImage();
            DebugLogger.Log("[StageStoryController] 4. 편지 이미지 숨김");

            yield return new WaitForSeconds(0.5f);

            // 5. 독백 대화가 있으면 진행
            if (stageData.HasAfterStoryDialogue())
            {
                DebugLogger.Log("[StageStoryController] 5. 독백 대화 시작");

                // Player 대화 패널 표시 (빈 텍스트)
                if (UI.PlayerDialogueUI.Instance != null)
                {
                    UI.PlayerDialogueUI.Instance.Show("");
                }

                // 6. 페이드 아웃 (화면 밝아짐)
                DebugLogger.Log("[StageStoryController] 6. 페이드 아웃 시작");
                bool fadeOutComplete = false;
                fadeUI.FadeOut(fadeDuration, () => fadeOutComplete = true);

                while (!fadeOutComplete)
                {
                    yield return null;
                }
                DebugLogger.Log("[StageStoryController] 페이드 아웃 완료");

                yield return new WaitForSeconds(0.5f);

                // 7. Player 독백 재생
                DebugLogger.Log("[StageStoryController] 7. Player 독백 재생");
                List<DialogueLine> monologue = stageData.GetAfterStoryDialogue();
                yield return PlayMonologueCoroutine(monologue);
            }

            // 8. 페이드 인 → 로비 이동
            DebugLogger.Log("[StageStoryController] 8. 로비 이동 시퀀스 시작");
            yield return TransitionToLobbyCoroutine();
        }

        /// <summary>
        /// Player 독백 재생 코루틴
        /// </summary>
        private System.Collections.IEnumerator PlayMonologueCoroutine(List<DialogueLine> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                DebugLogger.LogWarning("[StageStoryController] 독백 대화가 없습니다!");
                yield break;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                DialogueLine line = lines[i];

                // Player 대화만 표시 (독백이므로)
                if (UI.PlayerDialogueUI.Instance != null)
                {
                    UI.PlayerDialogueUI.Instance.Show(line.text);
                }

                DebugLogger.Log($"[StageStoryController] [독백] {line.text}");

                // 터치 입력 대기
                float displayTime = line.displayDuration > 0 ? line.displayDuration : lineDisplayTime;
                yield return WaitForInputOrTime(displayTime);

                // 다음 라인 전 짧은 간격
                if (i < lines.Count - 1)
                {
                    yield return new WaitForSeconds(linePauseDuration);
                }
            }

            // 독백 완료 후 패널 숨기기
            if (UI.PlayerDialogueUI.Instance != null)
            {
                UI.PlayerDialogueUI.Instance.Hide();
            }

            DebugLogger.Log("[StageStoryController] 독백 재생 완료");
        }


        // ==========================================
        // Public API
        // ==========================================

        /// <summary>
        /// 현재 스테이지 ID 설정
        /// </summary>
        /// <param name="stageID">스테이지 번호</param>
        public void SetCurrentStageID(int stageID)
        {
            currentStageID = stageID;
            DebugLogger.Log($"[StageStoryController] 현재 스테이지: {currentStageID}");
        }

        /// <summary>
        /// 현재 스테이지 ID 가져오기
        /// </summary>
        /// <returns>스테이지 번호</returns>
        public int GetCurrentStageID()
        {
            return currentStageID;
        }

        /// <summary>
        /// 스토리 대화 트리거 여부 확인
        /// </summary>
        /// <returns>트리거되었으면 true</returns>
        public bool IsStoryDialogueTriggered()
        {
            return isStoryDialogueTriggered;
        }

        /// <summary>
        /// 스테이지 초기화 (재시작 시 호출)
        /// </summary>
        public void ResetStage()
        {
            isStoryDialogueTriggered = false;
            isPlayingAfterStoryOnly = false;

            // 진행 중인 대화 코루틴 중지
            if (currentDialogueCoroutine != null)
            {
                StopCoroutine(currentDialogueCoroutine);
                currentDialogueCoroutine = null;
            }

            // 데이터 정리
            currentDialogueSet = null;
            currentNPCSprite = null;

            DebugLogger.Log("[StageStoryController] 스테이지 초기화");
        }

        // ==========================================
        // Debug
        // ==========================================

#if UNITY_EDITOR
        [ContextMenu("Test: Trigger Story Dialogue")]
        private void TestTriggerStoryDialogue()
        {
            PrepareStoryDialogueData(true);
            StartStoryDialogue();
        }

        [ContextMenu("Log Current State")]
        private void LogCurrentState()
        {
            Debug.Log("=== StageStoryController State ===");
            Debug.Log($"Current Stage ID: {currentStageID}");
            Debug.Log($"Story Dialogue Triggered: {isStoryDialogueTriggered}");

            if (storyNPCConfig != null)
            {
                bool hasStoryNPC = storyNPCConfig.HasStoryNPCForStage(currentStageID);
                Debug.Log($"Has Story NPC for Stage {currentStageID}: {hasStoryNPC}");
            }
            else
            {
                Debug.LogWarning("StoryNPCConfig is not assigned!");
            }

            if (ScoreManager.Instance != null)
            {
                int currentReward = ScoreManager.Instance.GetTotalReward();
                int targetReward = ScoreManager.Instance.GetTargetReward();
                bool isUnlocked = ScoreManager.Instance.IsDialogueUnlocked();
                Debug.Log($"Reward: {currentReward} / {targetReward}");
                Debug.Log($"Dialogue Unlocked: {isUnlocked}");
            }
        }
#endif
    }
}
