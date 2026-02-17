using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace RecipeAboutLife.Dialogue
{
    /// <summary>
    /// 스테이지 종료 대화 컨트롤러
    /// ScoreManager의 OnStageCompleted 이벤트를 받아 마지막 손님 전용 대화 출력
    /// 기존 DialogueManager와는 별개로 동작 (스테이지 종료 전용)
    /// </summary>
    public class StageDialogueController : MonoBehaviour
    {
        // ==========================================
        // Singleton Pattern
        // ==========================================

        private static StageDialogueController _instance;
        public static StageDialogueController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<StageDialogueController>();
                }
                return _instance;
            }
        }

        // ==========================================
        // Configuration
        // ==========================================

        [Header("스테이지 대화 데이터")]
        [SerializeField]
        [Tooltip("스테이지별 대화 데이터 목록")]
        private List<StageDialogueData> stageDialogues = new List<StageDialogueData>();

        [Header("표시 설정")]
        [SerializeField]
        [Tooltip("각 대화 라인 표시 시간 (초)")]
        private float lineDisplayTime = 3f;

        [SerializeField]
        [Tooltip("대화 라인 간 간격 (초)")]
        private float linePauseDuration = 0.5f;

        // ==========================================
        // State
        // ==========================================

        private bool isDialogueActive = false;
        private Coroutine currentDialogueCoroutine = null;
        private UI.DialogueBubbleUI currentDialogueBubble = null;

        // ==========================================
        // Events
        // ==========================================

        /// <summary>
        /// 스테이지 대화 시작 시
        /// </summary>
        public System.Action<int, bool> OnStageDialogueStarted; // (stageID, isSuccess)

        /// <summary>
        /// 스테이지 대화 종료 시
        /// </summary>
        public System.Action<int, bool> OnStageDialogueEnded; // (stageID, isSuccess)

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

        private void OnEnable()
        {
            // ⚠️ DEPRECATED: StageStoryController를 사용하세요.
            // StageStoryController와 이벤트 충돌을 방지하기 위해 자동 구독을 비활성화합니다.
            Debug.LogWarning("[StageDialogueController] DEPRECATED: StageStoryController를 사용하세요.");
        }

        private void OnDisable()
        {
            // DEPRECATED: 자동 구독이 비활성화되어 해제할 필요 없음
        }

        // ==========================================
        // Event Handlers
        // ==========================================

        /// <summary>
        /// 스테이지 완료 이벤트 처리
        /// </summary>
        private void OnStageCompleted(bool isSuccess)
        {
            // 로비에서 선택한 스테이지 인덱스 로드
            int currentStageID = PlayerPrefs.GetInt("SelectedStageIndex", 1);

            Debug.Log($"[StageDialogueController] 스테이지 {currentStageID} 완료! (성공: {isSuccess})");

            // 스테이지 종료 대화 시작
            StartStageFinalDialogue(currentStageID, isSuccess);
        }

        // ==========================================
        // Public API
        // ==========================================

        /// <summary>
        /// 스테이지 종료 대화 시작
        /// </summary>
        /// <param name="stageID">스테이지 번호</param>
        /// <param name="isSuccess">성공 여부</param>
        /// <returns>대화 시작 성공 여부</returns>
        public bool StartStageFinalDialogue(int stageID, bool isSuccess)
        {
            // 스테이지 대화 데이터 찾기
            StageDialogueData stageData = GetStageDialogueData(stageID);
            if (stageData == null)
            {
                Debug.LogWarning($"[StageDialogueController] 스테이지 {stageID} 대화 데이터를 찾을 수 없습니다!");
                return false;
            }

            // 성공/실패 대화 가져오기
            List<DialogueLine> lines = stageData.GetFinalDialogue(isSuccess);
            if (lines == null || lines.Count == 0)
            {
                Debug.LogWarning($"[StageDialogueController] 스테이지 {stageID} {(isSuccess ? "성공" : "실패")} 대화가 없습니다!");
                return false;
            }

            // 현재 NPC의 DialogueBubbleUI 찾기
            currentDialogueBubble = FindCurrentNPCDialogueBubble();
            if (currentDialogueBubble == null)
            {
                Debug.LogWarning("[StageDialogueController] 현재 NPC의 DialogueBubbleUI를 찾을 수 없습니다!");
                return false;
            }

            // 이미 대화 중이면 중단
            if (isDialogueActive)
            {
                Debug.LogWarning("[StageDialogueController] 이미 대화 중입니다. 기존 대화를 중단합니다.");
                StopDialogue();
            }

            // 대화 시작
            currentDialogueCoroutine = StartCoroutine(PlayStageDialogueSequence(stageID, isSuccess, lines));
            return true;
        }

        /// <summary>
        /// 대화 중단
        /// </summary>
        public void StopDialogue()
        {
            if (currentDialogueCoroutine != null)
            {
                StopCoroutine(currentDialogueCoroutine);
                currentDialogueCoroutine = null;
            }

            isDialogueActive = false;

            // DialogueBubbleUI 숨기기
            if (currentDialogueBubble != null)
            {
                currentDialogueBubble.Hide();
            }

            currentDialogueBubble = null;

            Debug.Log("[StageDialogueController] 대화 중단");
        }

        // ==========================================
        // Dialogue Playback
        // ==========================================

        /// <summary>
        /// 스테이지 대화 시퀀스 재생
        /// </summary>
        private IEnumerator PlayStageDialogueSequence(int stageID, bool isSuccess, List<DialogueLine> lines)
        {
            isDialogueActive = true;

            Debug.Log($"[StageDialogueController] 스테이지 {stageID} {(isSuccess ? "성공" : "실패")} 대화 시작 ({lines.Count} 라인)");

            // 이벤트 발생
            OnStageDialogueStarted?.Invoke(stageID, isSuccess);

            // Canvas 활성화
            if (currentDialogueBubble != null && !currentDialogueBubble.gameObject.activeSelf)
            {
                currentDialogueBubble.gameObject.SetActive(true);
            }

            // 각 대화 라인 재생
            for (int i = 0; i < lines.Count; i++)
            {
                DialogueLine line = lines[i];

                // 빈 라인은 건너뛰기
                if (line == null || line.IsEmpty())
                {
                    Debug.LogWarning($"[StageDialogueController] 빈 대화 라인을 건너뜁니다. (Index: {i})");
                    continue;
                }

                // 대화 표시
                DisplayDialogueLine(line);

                // 표시 시간만큼 대기
                float displayTime = line.displayDuration > 0 ? line.displayDuration : lineDisplayTime;
                yield return new WaitForSeconds(displayTime);

                // 마지막 라인이 아니면 짧은 간격
                if (i < lines.Count - 1)
                {
                    yield return new WaitForSeconds(linePauseDuration);
                }
            }

            // 대화 종료
            isDialogueActive = false;

            // DialogueBubbleUI 숨기기
            if (currentDialogueBubble != null)
            {
                currentDialogueBubble.Hide();
            }

            Debug.Log($"[StageDialogueController] 스테이지 {stageID} 대화 종료");

            // 이벤트 발생
            OnStageDialogueEnded?.Invoke(stageID, isSuccess);

            currentDialogueBubble = null;
            currentDialogueCoroutine = null;
        }

        /// <summary>
        /// 개별 대화 라인 표시
        /// </summary>
        private void DisplayDialogueLine(DialogueLine line)
        {
            if (currentDialogueBubble == null)
            {
                Debug.LogError("[StageDialogueController] DialogueBubbleUI가 없어서 대화를 표시할 수 없습니다!");
                return;
            }

            // DialogueBubbleUI를 통해 대화 표시
            currentDialogueBubble.ShowDialogue(line.text);

            Debug.Log($"[StageDialogueController] [{line.speaker}] {line.text}");

            // TODO: 발화자에 따른 추가 연출
            // - SpeakerType.NPC: NPC 애니메이션
            // - SpeakerType.Player: 플레이어 반응
            // - SpeakerType.System: UI 스타일 변경
        }

        /// <summary>
        /// 현재 NPC의 DialogueBubbleUI 찾기
        /// </summary>
        private UI.DialogueBubbleUI FindCurrentNPCDialogueBubble()
        {
            // NPCSpawnManager를 통해 현재 NPC 가져오기
            NPC.NPCSpawnManager spawnManager = FindFirstObjectByType<NPC.NPCSpawnManager>();
            if (spawnManager == null)
            {
                Debug.LogWarning("[StageDialogueController] NPCSpawnManager를 찾을 수 없습니다!");
                return null;
            }

            // 현재 NPC 가져오기
            GameObject currentNPC = spawnManager.GetCurrentNPC();
            if (currentNPC == null)
            {
                Debug.LogWarning("[StageDialogueController] 현재 NPC가 없습니다!");
                return null;
            }

            // DialogueBubbleUI 찾기
            UI.DialogueBubbleUI dialogueBubble = currentNPC.GetComponentInChildren<UI.DialogueBubbleUI>(true);
            if (dialogueBubble == null)
            {
                Debug.LogWarning($"[StageDialogueController] {currentNPC.name}에 DialogueBubbleUI가 없습니다!");
                return null;
            }

            Debug.Log($"[StageDialogueController] {currentNPC.name}의 DialogueBubbleUI를 찾았습니다.");
            return dialogueBubble;
        }

        // ==========================================
        // Helper Methods
        // ==========================================

        /// <summary>
        /// 스테이지 ID로 대화 데이터 찾기
        /// </summary>
        private StageDialogueData GetStageDialogueData(int stageID)
        {
            if (stageDialogues == null || stageDialogues.Count == 0)
            {
                Debug.LogWarning("[StageDialogueController] 스테이지 대화 데이터가 설정되지 않았습니다!");
                return null;
            }

            foreach (var data in stageDialogues)
            {
                if (data != null && data.stageID == stageID)
                {
                    return data;
                }
            }

            return null;
        }

        /// <summary>
        /// 대화 진행 중인지 확인
        /// </summary>
        public bool IsDialogueActive()
        {
            return isDialogueActive;
        }

        // ==========================================
        // Debug
        // ==========================================

#if UNITY_EDITOR
        [ContextMenu("Test: Play Success Dialogue")]
        private void TestPlaySuccessDialogue()
        {
            StartStageFinalDialogue(1, true);
        }

        [ContextMenu("Test: Play Fail Dialogue")]
        private void TestPlayFailDialogue()
        {
            StartStageFinalDialogue(1, false);
        }

        [ContextMenu("Test: Stop Dialogue")]
        private void TestStopDialogue()
        {
            StopDialogue();
        }
#endif
    }

    // ==========================================
    // 기존 시스템과의 연결 예시 (주석)
    // ==========================================

    /*
     * === 스테이지 종료 대화 트리거 예시 ===
     *
     * 1. ScoreManager.OnAllNPCsServed()에서 자동 호출
     *    - StageDialogueController가 자동으로 OnStageCompleted 이벤트를 구독하여 처리
     *    - 추가 코드 필요 없음!
     *
     * 2. 수동으로 호출하는 경우 (테스트 또는 특수 상황)
     *    - 예시:
     *      StageDialogueController controller = StageDialogueController.Instance;
     *      if (controller != null)
     *      {
     *          controller.StartStageFinalDialogue(currentStageID, isSuccess);
     *      }
     *
     * === UI 설정 (NPC World Space Canvas 사용) ===
     *
     * NPC Prefab
     * └─ Canvas (DialogueCanvas, World Space)
     *    └─ DialogueBubbleUI
     *       └─ Text (TMP)
     *
     * - StageDialogueController는 NPCSpawnManager를 통해 현재 NPC를 찾습니다
     * - 현재 NPC의 DialogueBubbleUI를 자동으로 사용합니다
     * - Screen Space UI 설정 불필요!
     *
     * === ScriptableObject 설정 ===
     *
     * StageDialogueController Inspector:
     * - stageDialogues: Stage1_Dialogue, Stage2_Dialogue... 추가
     * - lineDisplayTime: 3
     * - linePauseDuration: 0.5
     *
     * === 작동 흐름 ===
     *
     * 1. 모든 NPC 서빙 완료
     * 2. ScoreManager.OnStageCompleted 이벤트 발생
     * 3. StageDialogueController.OnStageCompleted() 자동 호출
     * 4. NPCSpawnManager.GetCurrentNPC()로 마지막 NPC 찾기
     * 5. 해당 NPC의 DialogueBubbleUI 사용
     * 6. 스테이지 성공/실패 대화 순차 재생
     * 7. 대화 종료 후 DialogueBubbleUI.Hide()
     *
     * === TODO: 향후 구현 ===
     * 1. 다음 대화로 넘기는 버튼 (현재는 자동 진행만 지원)
     * 2. 대화 중 게임 입력 차단
     * 3. 효과음, 배경음악 연출
     * 4. 대화 타이핑 효과 (한 글자씩 표시)
     * 5. 스킵 기능
     */
}
