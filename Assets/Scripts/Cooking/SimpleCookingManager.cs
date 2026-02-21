using UnityEngine;
using System;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// 간소화된 요리 관리자
    /// 6단계 요리 프로세스 관리
    /// </summary>
    public class SimpleCookingManager : MonoBehaviour
    {
        public static SimpleCookingManager Instance { get; private set; }

        [Header("=== Stations (Inspector 할당) ===")]
        public Transform stickStation;      // 꼬치통
        public Transform cuttingBoard;      // 도마
        public Transform batterZone;        // 반죽통 영역 (Collider만)
        public Transform fryingStation;     // 튀김기
        public Transform coolingRack;       // 식힘망/트레이

        [Header("=== Prefabs ===")]
        public GameObject stickPrefab;      // 꼬치 프리팹

        [Header("=== Main Screen Ingredient Prefabs ===")]
        public GameObject mainSausagePrefab;     // 메인 화면용 소시지 프리팹
        public GameObject mainCheesePrefab;      // 메인 화면용 치즈 프리팹

        [Header("=== Main Screen Ingredient Offsets (스틱 기준 상대 위치) ===")]
        public Vector3 mainIngredient1Offset = new Vector3(-0.3f, 0f, 0f);  // 첫번째 재료 오프셋
        public Vector3 mainIngredient2Offset = new Vector3(0.3f, 0f, 0f);   // 두번째 재료 오프셋

        [Header("=== Batter Images (3단계) ===")]
        public Sprite batterStage1;
        public Sprite batterStage2;
        public Sprite batterStage3;

        [Header("=== Frying Images (5단계) ===")]
        public Sprite fryingRaw;            // 0-3초
        public Sprite fryingYellow;         // 3-7초
        public Sprite fryingGolden;         // 7-9초 ✅
        public Sprite fryingBrown;          // 9-11초
        public Sprite fryingBurnt;          // 11초+

        [Header("=== Frying Position Offsets ===")]
        public Vector3 fryingHotdogOffset = new Vector3(0f, 0f, 0f);    // 튀김기 내 핫도그 위치 오프셋
        public Vector3 fryingEffectOffset = new Vector3(0f, -0.5f, 0f); // 보글보글 이펙트 위치 오프셋

        [Header("=== Frying Effect ===")]
        public GameObject fryingBubblePrefab; // 보글보글 이펙트

        [Header("=== Hotdog Scale Settings ===")]
        public Vector3 ingredientCompleteScale = new Vector3(1f, 1f, 1f);  // 도마 위 재료 끼운 꼬치
        public Vector3 batterScale = new Vector3(1f, 1f, 1f);              // 반죽 공통
        public Vector3 fryingScale = new Vector3(1f, 1f, 1f);              // 튀김 공통

        [Header("=== Topping Settings ===")]
        public Sprite sugarOverlaySprite;      // 설탕 오버레이 스프라이트
        public Vector3 sugarPositionOffset = new Vector3(0f, 0f, 0f);  // 설탕 위치 오프셋
        public Vector3 sugarScale = new Vector3(1f, 1f, 1f);           // 설탕 스케일
        public Sprite ketchupDotSprite;        // 케첩 점 스프라이트
        public Sprite mustardDotSprite;        // 머스타드 점 스프라이트
        public float sauceScaleRatio = 0.5f;   // 팝업→메인 소스 스케일 비율
        public Vector3 saucePositionOffset = new Vector3(0f, 0f, 0f); // 메인 화면 소스 위치 오프셋

        [Header("=== Popups ===")]
        public GameObject ingredientPopup;  // 재료 선택 팝업
        public GameObject toppingPopup;     // 토핑 팝업

        [Header("=== Serving Settings ===")]
        public SimpleDropZone servingWindow;  // 창문 드롭존
        public AudioClip servingSound;        // 제공 사운드
        private AudioSource audioSource;

        [Header("=== Current State (Debug) ===")]
        [SerializeField] private CookingPhase currentPhase = CookingPhase.None;
        [SerializeField] private GameObject currentHotdog;  // 현재 만들고 있는 핫도그

        // 핫도그 데이터
        private HotdogData hotdogData = new HotdogData();

        // 이벤트
        public event Action<CookingPhase> OnPhaseChanged;
        public event Action OnCookingStarted;   // 요리 시작 이벤트
        public event Action<int> OnHotdogServed;     // 요리 제공 완료 이벤트 (획득 금액 전달)

        public CookingPhase CurrentPhase => currentPhase;
        public GameObject CurrentHotdog => currentHotdog;
        public HotdogData CurrentHotdogData => hotdogData;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            // AudioSource 초기화
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            // 테스트용 자동 시작
            // StartCooking();
        }

        /// <summary>
        /// 요리 시작
        /// </summary>
        [ContextMenu("Start Cooking")]
        public void StartCooking()
        {
            // 이전 요리 정리
            CleanupPreviousCooking();
            
            hotdogData = new HotdogData();
            currentHotdog = null;
            
            // 요리 시작 이벤트 발생
            OnCookingStarted?.Invoke();
            
            ChangePhase(CookingPhase.StickPickup);
            Debug.Log("[SimpleCookingManager] === 요리 시작! ===");
        }

        /// <summary>
        /// 이전 요리 관련 오브젝트 정리
        /// </summary>
        private void CleanupPreviousCooking()
        {
            Debug.Log("[SimpleCookingManager] 이전 요리 정리 시작...");

            // 1. 현재 핫도그 오브젝트 삭제
            if (currentHotdog != null)
            {
                Destroy(currentHotdog);
                currentHotdog = null;
                Debug.Log("[SimpleCookingManager] 이전 핫도그 오브젝트 삭제");
            }

            // 2. "Hotdog" 태그가 붙은 모든 오브젝트 삭제 (태그가 없으면 스킵)
            try
            {
                GameObject[] hotdogs = GameObject.FindGameObjectsWithTag("Hotdog");
                foreach (var hotdog in hotdogs)
                {
                    Destroy(hotdog);
                    Debug.Log($"[SimpleCookingManager] 태그로 찾은 핫도그 삭제: {hotdog.name}");
                }
            }
            catch (UnityException)
            {
                // "Hotdog" 태그가 정의되지 않은 경우 무시
                Debug.Log("[SimpleCookingManager] 'Hotdog' 태그가 없음 - 스킵");
            }

            // 3. 이름에 "Stick" 또는 "Hotdog"이 포함된 동적 생성 오브젝트 찾아서 삭제
            // (Prefab에서 생성된 인스턴스는 보통 "(Clone)" 접미사가 붙음)
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj != null && obj.name.Contains("(Clone)") && 
                    (obj.name.Contains("Stick") || obj.name.Contains("Hotdog")))
                {
                    Destroy(obj);
                    Debug.Log($"[SimpleCookingManager] 클론 오브젝트 삭제: {obj.name}");
                }
            }

            // 4. 팝업 닫기
            if (ingredientPopup != null && ingredientPopup.activeSelf)
            {
                ingredientPopup.SetActive(false);
                Debug.Log("[SimpleCookingManager] 재료 팝업 닫음");
            }
            if (toppingPopup != null && toppingPopup.activeSelf)
            {
                toppingPopup.SetActive(false);
                Debug.Log("[SimpleCookingManager] 토핑 팝업 닫음");
            }

            // 5. IngredientPopup 하위의 Clone 오브젝트 삭제 (이전 손님 재료)
            if (ingredientPopup != null)
            {
                // 역순으로 삭제 (자식 삭제 시 인덱스 변경 방지)
                for (int i = ingredientPopup.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = ingredientPopup.transform.GetChild(i);
                    if (child.name.Contains("(Clone)"))
                    {
                        Destroy(child.gameObject);
                        Debug.Log($"[SimpleCookingManager] IngredientPopup 내 클론 삭제: {child.name}");
                    }
                }
            }

            Debug.Log("[SimpleCookingManager] 이전 요리 정리 완료");
        }

        /// <summary>
        /// 페이즈 변경
        /// </summary>
        public void ChangePhase(CookingPhase newPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
            Debug.Log($"[SimpleCookingManager] Phase changed to: {newPhase}");
        }

        /// <summary>
        /// 현재 핫도그 오브젝트 설정
        /// </summary>
        public void SetCurrentHotdog(GameObject hotdog)
        {
            currentHotdog = hotdog;
        }

        /// <summary>
        /// 다음 페이즈로 진행
        /// </summary>
        public void NextPhase()
        {
            CookingPhase next = currentPhase switch
            {
                CookingPhase.StickPickup => CookingPhase.Ingredient,
                CookingPhase.Ingredient => CookingPhase.Batter,
                CookingPhase.Batter => CookingPhase.Frying,
                CookingPhase.Frying => CookingPhase.Topping,
                CookingPhase.Topping => CookingPhase.Completed,
                _ => CookingPhase.None
            };

            if (next != CookingPhase.None)
            {
                ChangePhase(next);
            }
            else
            {
                CompleteCooking();
            }
        }

        /// <summary>
        /// 요리 완료
        /// </summary>
        private void CompleteCooking()
        {
            Debug.Log($"[SimpleCookingManager] === 요리 완료! ===");
            Debug.Log($"  재료1: {hotdogData.filling1}");
            Debug.Log($"  재료2: {hotdogData.filling2}");
            Debug.Log($"  반죽: {hotdogData.batterStage}단계");
            Debug.Log($"  튀김: {hotdogData.fryingState}");

            currentPhase = CookingPhase.None;
        }

        /// <summary>
        /// 재료 선택 팝업 열기
        /// </summary>
        public void ShowIngredientPopup()
        {
            if (ingredientPopup != null)
            {
                ingredientPopup.SetActive(true);
                Debug.Log("[SimpleCookingManager] 재료 팝업 열림");
            }
        }

        /// <summary>
        /// 재료 선택 팝업 닫기
        /// </summary>
        public void HideIngredientPopup()
        {
            if (ingredientPopup != null)
            {
                ingredientPopup.SetActive(false);
                Debug.Log("[SimpleCookingManager] 재료 팝업 닫힘");
            }
        }

        /// <summary>
        /// 토핑 팝업 열기
        /// </summary>
        public void ShowToppingPopup()
        {
            if (toppingPopup != null)
            {
                toppingPopup.SetActive(true);
                Debug.Log("[SimpleCookingManager] 토핑 팝업 열림");
            }
        }

        /// <summary>
        /// 토핑 팝업 닫기
        /// </summary>
        public void HideToppingPopup()
        {
            if (toppingPopup != null)
            {
                toppingPopup.SetActive(false);
                Debug.Log("[SimpleCookingManager] 토핑 팝업 닫힘");
            }
        }

        /// <summary>
        /// 요리 제공 (핫도그를 창문에 드롭했을 때)
        /// </summary>
        public void ServeHotdog()
        {
            if (currentHotdog == null)
            {
                Debug.LogWarning("[SimpleCookingManager] 제공할 핫도그가 없습니다!");
                return;
            }

            // 완성 정보 로그
            Debug.Log("[SimpleCookingManager] ========== 요리 제공 완료! ==========");
            Debug.Log($"  재료1: {hotdogData.filling1}");
            Debug.Log($"  재료2: {hotdogData.filling2}");
            Debug.Log($"  반죽 단계: {hotdogData.batterStage}");
            Debug.Log($"  튀김 상태: {hotdogData.fryingState}");
            Debug.Log($"  설탕: {(hotdogData.hasSugar ? "O" : "X")}");
            Debug.Log($"  케첩: {(hotdogData.hasKetchup ? "O" : "X")}");
            Debug.Log($"  머스타드: {(hotdogData.hasMustard ? "O" : "X")}");
            Debug.Log("[SimpleCookingManager] =====================================");

            // 점수 계산
            int earnedMoney = CalculateScore();
            Debug.Log($"[SimpleCookingManager] 획득 금액: {earnedMoney}원");

            // GameManager에 서빙 완료 알림
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMoney(earnedMoney);
                GameManager.Instance.CompleteServing(earnedMoney);
            }
            else
            {
                Debug.LogWarning("[SimpleCookingManager] GameManager가 없습니다!");
            }

            // 사운드 재생
            PlayServingSound();

            // 핫도그 제거
            Destroy(currentHotdog);
            currentHotdog = null;

            // Phase 초기화
            currentPhase = CookingPhase.None;

            // 요리 제공 완료 이벤트 발생 (획득 금액 전달)
            OnHotdogServed?.Invoke(earnedMoney);

            Debug.Log("[SimpleCookingManager] 새 요리를 시작하려면 StartCooking()을 호출하세요.");
        }

        /// <summary>
        /// 점수 계산
        /// </summary>
        private int CalculateScore()
        {
            // 현재 주문 가져오기 (GameManager에서)
            OrderData currentOrder = GameManager.Instance?.GetCurrentOrder();

            if (currentOrder != null)
            {
                // 주문 vs 완성품 비교 로그
                Debug.Log($"[SimpleCookingManager] === 주문 비교 점수 계산 ===");
                Debug.Log($"  [주문] 재료: {currentOrder.filling1}, {currentOrder.filling2} / 설탕{(currentOrder.wantsSugar ? "O" : "X")}, 케첩{(currentOrder.wantsKetchup ? "O" : "X")}, 머스타드{(currentOrder.wantsMustard ? "O" : "X")}");
                Debug.Log($"  [완성] 재료: {hotdogData.filling1}, {hotdogData.filling2} / 설탕{(hotdogData.hasSugar ? "O" : "X")}, 케첩{(hotdogData.hasKetchup ? "O" : "X")}, 머스타드{(hotdogData.hasMustard ? "O" : "X")}");

                // 주문이 있으면 주문과 비교하여 점수 계산
                return ScoreCalculator.Calculate(currentOrder, hotdogData);
            }
            else
            {
                // 주문이 없으면 기본 점수 계산 (테스트용)
                Debug.LogWarning("[SimpleCookingManager] 주문 데이터 없음 - 기본 점수 계산 (주문 비교 불가)");
                return ScoreCalculator.CalculateWithoutOrder(hotdogData);
            }
        }

        /// <summary>
        /// 제공 사운드 재생
        /// </summary>
        private void PlayServingSound()
        {
            // AudioManager를 통해 서빙 사운드 재생
            AudioManager.Instance?.PlayServing();
            Debug.Log("[SimpleCookingManager] 제공 사운드 재생 (AudioManager)");
        }

#if UNITY_EDITOR
        [ContextMenu("Force Next Phase")]
        private void ForceNextPhase() => NextPhase();

        [ContextMenu("Log Status")]
        private void LogStatus()
        {
            Debug.Log($"Current Phase: {currentPhase}");
            Debug.Log($"Current Hotdog: {(currentHotdog != null ? currentHotdog.name : "None")}");
        }
#endif
    }

    /// <summary>
    /// 요리 페이즈
    /// </summary>
    public enum CookingPhase
    {
        None,
        StickPickup,    // 1. 꼬치 들기
        Ingredient,     // 2. 재료 끼우기
        Batter,         // 3. 반죽 입히기
        Frying,         // 4. 튀기기
        Topping,        // 5. 토핑
        Completed       // 6. 완료
    }

    /// <summary>
    /// 튀김 상태
    /// </summary>
    public enum FryingState
    {
        Raw,        // 0-3초
        Yellow,     // 3-7초
        Golden,     // 7-9초 ✅
        Brown,      // 9-11초
        Burnt       // 11초+
    }

    /// <summary>
    /// 핫도그 데이터
    /// </summary>
    [Serializable]
    public class HotdogData
    {
        public string filling1 = "";        // 첫번째 재료
        public string filling2 = "";        // 두번째 재료
        public int batterStage = 0;         // 반죽 단계 (1-3)
        public FryingState fryingState = FryingState.Raw;
        public float fryingTime = 0f;       // 튀김 시간
        public bool hasSugar = false;       // 설탕
        public bool hasKetchup = false;     // 케첩
        public bool hasMustard = false;     // 머스타드
    }
}