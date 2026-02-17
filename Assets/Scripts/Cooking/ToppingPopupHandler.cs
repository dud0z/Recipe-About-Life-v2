using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// Phase 5: 토핑 팝업 핸들러
    /// 설탕, 케첩, 머스타드 토핑 처리
    /// </summary>
    public class ToppingPopupHandler : MonoBehaviour
    {
        [Header("=== Hotdog Display ===")]
        public Transform hotdogDisplay;           // 핫도그 표시 위치
        public SpriteRenderer hotdogSprite;       // 핫도그 스프라이트 렌더러
        public GameObject sugarOverlay;           // 설탕 이미지 오브젝트 (초기 비활성화)
        public int hotdogSortingOrder = 51;
        public int sugarSortingOrder = 52;

        [Header("=== Topping Sources ===")]
        public ToppingSource ketchupSource;       // 케첩통
        public ToppingSource mustardSource;       // 머스타드통
        public SimpleDropZone sugarTray;          // 설탕 트레이 드롭존

        [Header("=== Sauce Drawer ===")]
        public SauceDrawer sauceDrawer;           // 소스 그리기 컴포넌트

        [Header("=== Gauge Bar (UI Canvas) ===")]
        public GameObject gaugeBarObject;         // 게이지 바 전체 오브젝트
        public Image gaugeBackground;             // 게이지 배경 이미지
        public Image gaugeOutline;                // 게이지 외곽선 이미지
        public Image gaugeFill;                   // 게이지 바 (Fill)
        public Sprite ketchupFillSprite;          // 빨강 게이지 스프라이트
        public Sprite mustardFillSprite;          // 노랑 게이지 스프라이트

        [Header("=== Gauge Settings ===")]
        public float maxSauceAmount = 1f;         // 최대 소스량 (게이지 1 = 100%)
        public float sauceDecreaseRate = 0.05f;   // 드래그당 소스 감소량 (Inspector 조정)

        [Header("=== Close Button ===")]
        public Button closeButton;                // X 버튼 (ToppingUICanvas 안)

        [Header("=== Close Button (Main Canvas) ===")]
        [Tooltip("메인 Canvas(Screen Space)에 있는 닫기 버튼 - 확실한 클릭 감지용")]
        public GameObject mainCanvasCloseButton;  // 메인 Canvas의 닫기 버튼

        [Header("=== Hotdog Draggable (설탕용) ===")]
        public SimpleDraggable hotdogDraggable;   // 핫도그 드래그용

        // 상태
        private SimpleCookingManager manager;
        private bool canUseSugar = true;          // 설탕 사용 가능 여부
        private float ketchupRemaining = 1f;      // 남은 케첩량 (0~1)
        private float mustardRemaining = 1f;      // 남은 머스타드량 (0~1)
        private ToppingType currentTopping = ToppingType.None;
        private bool hasSugar = false;
        private Vector3 hotdogOriginalPosition;   // 핫도그 원래 위치 저장

        private void Start()
        {
            manager = SimpleCookingManager.Instance;

            // 소스통 이벤트 연결
            if (ketchupSource != null)
                ketchupSource.OnSourceClicked += OnSourceClicked;
            if (mustardSource != null)
                mustardSource.OnSourceClicked += OnSourceClicked;

            // 소스 그리기 이벤트 연결
            if (sauceDrawer != null)
                sauceDrawer.OnSauceUsed += OnSauceUsed;

            // 닫기 버튼 이벤트 연결
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);

            // 설탕 드롭 이벤트 연결
            if (hotdogDraggable != null)
                hotdogDraggable.OnDragEnded += OnHotdogDropped;

            // 게이지 바 초기 숨김
            if (gaugeBarObject != null)
                gaugeBarObject.SetActive(false);

            // 메인 Canvas 닫기 버튼 - 팝업이 비활성화 상태일 때만 숨김
            // (OnEnable이 Start보다 먼저 호출되므로, 이미 활성화된 상태면 건드리지 않음)
            if (mainCanvasCloseButton != null && !gameObject.activeInHierarchy)
            {
                mainCanvasCloseButton.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            if (ketchupSource != null)
                ketchupSource.OnSourceClicked -= OnSourceClicked;
            if (mustardSource != null)
                mustardSource.OnSourceClicked -= OnSourceClicked;
            if (sauceDrawer != null)
                sauceDrawer.OnSauceUsed -= OnSauceUsed;
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            if (hotdogDraggable != null)
                hotdogDraggable.OnDragEnded -= OnHotdogDropped;
        }

        private void OnEnable()
        {
            // manager가 null이면 직접 가져오기 (OnEnable이 Start보다 먼저 호출될 수 있음)
            if (manager == null)
            {
                manager = SimpleCookingManager.Instance;
            }
            
            // 팝업 열릴 때 초기화
            InitializePopup();

            // 메인 UI 숨김
            HideMainUIImmediate();

            // 메인 Canvas의 닫기 버튼 표시
            if (mainCanvasCloseButton != null)
            {
                mainCanvasCloseButton.SetActive(true);
                Debug.Log("[ToppingPopupHandler] 메인 Canvas 닫기 버튼 표시");
            }
        }

        private void OnDisable()
        {
            // 지연 호출 취소
            CancelInvoke(nameof(HideMainUIImmediate));

            // 소스 선택 상태 초기화 (다음 손님을 위해)
            ResetToppingState();

            // 메인 Canvas의 닫기 버튼 숨김
            if (mainCanvasCloseButton != null)
            {
                mainCanvasCloseButton.SetActive(false);
                Debug.Log("[ToppingPopupHandler] 메인 Canvas 닫기 버튼 숨김");
            }

            // 팝업 닫힐 때 메인 UI 표시
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowMainUI();
            }
        }

        /// <summary>
        /// 토핑 상태 초기화 (다음 손님을 위해)
        /// </summary>
        private void ResetToppingState()
        {
            Debug.Log("[ToppingPopupHandler] 토핑 상태 초기화 시작...");

            // 1. 현재 토핑 선택 해제
            currentTopping = ToppingType.None;

            // 2. 소스 잔량 초기화
            ketchupRemaining = 1f;
            mustardRemaining = 1f;

            // 3. 게이지 바 숨김
            if (gaugeBarObject != null)
                gaugeBarObject.SetActive(false);

            // 4. SauceDrawer 초기화
            if (sauceDrawer != null)
            {
                sauceDrawer.StopDrawing();
                sauceDrawer.ClearAllDots();
            }

            // 5. 설탕 상태 초기화
            hasSugar = false;
            canUseSugar = true;

            // 6. 설탕 오버레이 숨김
            if (sugarOverlay != null)
                sugarOverlay.SetActive(false);

            // 7. 핫도그 드래그 상태 초기화
            if (hotdogDraggable != null)
                hotdogDraggable.isDraggable = true;

            Debug.Log("[ToppingPopupHandler] 토핑 상태 초기화 완료");
        }

        /// <summary>
        /// 메인 UI 즉시 숨김
        /// </summary>
        private void HideMainUIImmediate()
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.HideMainUI();
                Debug.Log("[ToppingPopupHandler] HideMainUI 호출됨");
            }
            else
            {
                Debug.LogWarning("[ToppingPopupHandler] GameUIManager.Instance가 null!");
            }
        }

        /// <summary>
        /// 팝업 초기화
        /// </summary>
        private void InitializePopup()
        {
            // manager 재확인
            if (manager == null)
            {
                manager = SimpleCookingManager.Instance;
            }
            
            canUseSugar = true;
            hasSugar = false;
            ketchupRemaining = maxSauceAmount;
            mustardRemaining = maxSauceAmount;
            currentTopping = ToppingType.None;

            // 설탕 오버레이 숨김
            if (sugarOverlay != null)
                sugarOverlay.SetActive(false);

            // 게이지 바 숨김
            if (gaugeBarObject != null)
                gaugeBarObject.SetActive(false);

            // 소스 그리기 초기화
            if (sauceDrawer != null)
                sauceDrawer.ClearAllDots();

            // 핫도그 드래그 활성화 (설탕용)
            if (hotdogDraggable != null)
            {
                hotdogDraggable.isDraggable = true;
                Debug.Log("[ToppingPopupHandler] ✅ 핫도그 드래그 활성화됨");
            }
            else
            {
                Debug.LogError("[ToppingPopupHandler] ❌ hotdogDraggable이 Inspector에서 할당되지 않았습니다!");
            }

            // 핫도그 원래 위치 저장
            if (hotdogSprite != null)
            {
                hotdogOriginalPosition = hotdogSprite.transform.position;
                Debug.Log($"[ToppingPopupHandler] 핫도그 원래 위치 저장: {hotdogOriginalPosition}");
            }

            // 핫도그 스프라이트 설정 (현재 튀김 상태 이미지)
            UpdateHotdogSprite();

            Debug.Log("[ToppingPopupHandler] 팝업 초기화 완료");
        }

        /// <summary>
        /// 핫도그 스프라이트 업데이트
        /// </summary>
        private void UpdateHotdogSprite()
        {
            if (manager == null)
            {
                Debug.LogError("[ToppingPopupHandler] ❌ manager가 null입니다!");
                return;
            }
            
            if (hotdogSprite == null)
            {
                Debug.LogError("[ToppingPopupHandler] ❌ hotdogSprite가 할당되지 않았습니다!");
                return;
            }

            // 현재 튀김 상태에 맞는 스프라이트 설정
            FryingState state = manager.CurrentHotdogData.fryingState;
            Debug.Log($"[ToppingPopupHandler] 튀김 상태: {state}");
            
            Sprite sprite = state switch
            {
                FryingState.Raw => manager.fryingRaw,
                FryingState.Yellow => manager.fryingYellow,
                FryingState.Golden => manager.fryingGolden,
                FryingState.Brown => manager.fryingBrown,
                FryingState.Burnt => manager.fryingBurnt,
                _ => manager.fryingGolden
            };

            if (sprite != null)
            {
                hotdogSprite.sprite = sprite;
                hotdogSprite.sortingOrder = hotdogSortingOrder;
                Debug.Log($"[ToppingPopupHandler] ✅ 핫도그 스프라이트 설정 완료: {sprite.name}");
            }
            else
            {
                Debug.LogError($"[ToppingPopupHandler] ❌ {state} 스프라이트가 SimpleCookingManager에 할당되지 않았습니다!");
            }
        }

        /// <summary>
        /// 소스통 클릭 처리
        /// </summary>
        private void OnSourceClicked(ToppingType type)
        {
            // 설탕 사용 불가로 변경
            canUseSugar = false;
            if (hotdogDraggable != null)
                hotdogDraggable.isDraggable = false;

            // 현재 토핑 타입 설정
            currentTopping = type;

            // 게이지 바 표시
            ShowGaugeBar(type);

            // 소스 그리기 시작
            if (sauceDrawer != null)
                sauceDrawer.StartDrawing(type);

            // 소스 소리는 실제로 드래그할 때 SauceDrawer에서 재생됨

            Debug.Log($"[ToppingPopupHandler] {type} 선택됨 - 그리기 시작");
        }

        /// <summary>
        /// 게이지 바 표시
        /// </summary>
        private void ShowGaugeBar(ToppingType type)
        {
            if (gaugeBarObject == null || gaugeFill == null) 
            {
                Debug.LogError("[ToppingPopupHandler] ❌ gaugeBarObject 또는 gaugeFill이 null!");
                return;
            }

            // 게이지 바 활성화
            gaugeBarObject.SetActive(true);

            // Image Type 확인 (Filled여야 fillAmount 작동)
            Debug.Log($"[ToppingPopupHandler] GaugeFill Image Type: {gaugeFill.type}");
            if (gaugeFill.type != UnityEngine.UI.Image.Type.Filled)
            {
                Debug.LogError("[ToppingPopupHandler] ❌ GaugeFill Image Type이 Filled가 아닙니다! Inspector에서 Image Type을 Filled로 변경하세요!");
            }

            // 스프라이트 변경
            if (type == ToppingType.Ketchup && ketchupFillSprite != null)
            {
                gaugeFill.sprite = ketchupFillSprite;
                gaugeFill.fillAmount = ketchupRemaining;
                Debug.Log($"[ToppingPopupHandler] 케첩 게이지 설정: fillAmount={ketchupRemaining}");
            }
            else if (type == ToppingType.Mustard && mustardFillSprite != null)
            {
                gaugeFill.sprite = mustardFillSprite;
                gaugeFill.fillAmount = mustardRemaining;
                Debug.Log($"[ToppingPopupHandler] 머스타드 게이지 설정: fillAmount={mustardRemaining}");
            }

            Debug.Log($"[ToppingPopupHandler] 게이지 바 표시: {type}");
        }

        /// <summary>
        /// 소스 사용 콜백
        /// </summary>
        private void OnSauceUsed(float amount)
        {
            if (currentTopping == ToppingType.None) return;

            Debug.Log($"[ToppingPopupHandler] 소스 사용됨 - 타입: {currentTopping}, 감소량: {sauceDecreaseRate}");

            // 남은 소스량 감소
            if (currentTopping == ToppingType.Ketchup)
            {
                ketchupRemaining -= sauceDecreaseRate;
                ketchupRemaining = Mathf.Max(0, ketchupRemaining);

                Debug.Log($"[ToppingPopupHandler] 케첩 남은량: {ketchupRemaining:F2}");

                // 게이지 바 업데이트
                UpdateGaugeFill(ketchupRemaining);

                // 소스 다 쓰면 그리기 중지
                if (ketchupRemaining <= 0)
                {
                    sauceDrawer?.StopDrawing();
                    // 소스 소리 정지
                    AudioManager.Instance?.StopSauceLoop();
                    Debug.Log("[ToppingPopupHandler] 케첩 소진!");
                }
            }
            else if (currentTopping == ToppingType.Mustard)
            {
                mustardRemaining -= sauceDecreaseRate;
                mustardRemaining = Mathf.Max(0, mustardRemaining);

                Debug.Log($"[ToppingPopupHandler] 머스타드 남은량: {mustardRemaining:F2}");

                // 게이지 바 업데이트
                UpdateGaugeFill(mustardRemaining);

                // 소스 다 쓰면 그리기 중지
                if (mustardRemaining <= 0)
                {
                    sauceDrawer?.StopDrawing();
                    // 소스 소리 정지
                    AudioManager.Instance?.StopSauceLoop();
                    Debug.Log("[ToppingPopupHandler] 머스타드 소진!");
                }
            }

            // 데이터 저장
            if (manager != null)
            {
                manager.CurrentHotdogData.hasKetchup = sauceDrawer.HasKetchup();
                manager.CurrentHotdogData.hasMustard = sauceDrawer.HasMustard();
            }
        }

        /// <summary>
        /// 게이지 Fill 업데이트
        /// </summary>
        private void UpdateGaugeFill(float fillValue)
        {
            if (gaugeFill == null)
            {
                Debug.LogError("[ToppingPopupHandler] ❌ gaugeFill이 null입니다!");
                return;
            }

            gaugeFill.fillAmount = fillValue;
            Debug.Log($"[ToppingPopupHandler] 게이지 업데이트: {fillValue:F2}");
        }

        /// <summary>
        /// 핫도그 드롭 처리 (설탕용)
        /// </summary>
        private void OnHotdogDropped(SimpleDraggable draggable, SimpleDropZone dropZone)
        {
            Debug.Log($"[ToppingPopupHandler] 핫도그 드롭 - dropZone: {(dropZone != null ? dropZone.name : "null")}");
            
            // 설탕 트레이에 드롭 & 설탕 사용 가능
            if (dropZone != null && dropZone.zoneType == SimpleDropZone.ZoneType.SugarTray && canUseSugar)
            {
                ApplySugar();
            }

            // 핫도그 원래 위치로 복귀 (저장된 위치 사용)
            draggable.MoveTo(hotdogOriginalPosition);
            Debug.Log($"[ToppingPopupHandler] 핫도그 원래 위치로 복귀: {hotdogOriginalPosition}");
        }

        /// <summary>
        /// 설탕 적용
        /// </summary>
        private void ApplySugar()
        {
            if (!canUseSugar || hasSugar) return;

            hasSugar = true;
            canUseSugar = false;

            // 설탕 묻히는 소리 재생
            AudioManager.Instance?.PlaySugarApply();

            // 설탕 오버레이 표시
            if (sugarOverlay != null)
            {
                sugarOverlay.SetActive(true);
                SpriteRenderer sr = sugarOverlay.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = sugarSortingOrder;
                
                // Collider가 있으면 비활성화 (클릭 가로막기 방지)
                Collider2D col = sugarOverlay.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.enabled = false;
                    Debug.Log("[ToppingPopupHandler] 설탕 오버레이 Collider 비활성화");
                }
            }

            // 데이터 저장
            if (manager != null)
                manager.CurrentHotdogData.hasSugar = true;

            // 설탕 후에는 핫도그 드래그 비활성화
            if (hotdogDraggable != null)
                hotdogDraggable.isDraggable = false;

            Debug.Log("[ToppingPopupHandler] ✅ 설탕 적용!");
        }

        /// <summary>
        /// X 버튼 클릭 - 완료 (public으로 변경하여 Inspector 및 외부에서 호출 가능)
        /// </summary>
        public void OnCloseButtonClicked()
        {
            Debug.Log("[ToppingPopupHandler] ✅ 토핑 완료!");

            // 소스 소리 정지 (혹시 재생 중이라면)
            AudioManager.Instance?.StopSauceLoop();

            // 메인 화면 핫도그에 토핑 반영
            ApplyToppingsToMainHotdog();

            // 팝업 닫기
            manager?.HideToppingPopup();

            // 직접 비활성화 (manager가 실패할 경우 대비)
            gameObject.SetActive(false);

            // 다음 단계로
            manager?.NextPhase();
        }

        /// <summary>
        /// 메인 화면 핫도그에 토핑 반영
        /// </summary>
        private void ApplyToppingsToMainHotdog()
        {
            if (manager == null || manager.CurrentHotdog == null) 
            {
                Debug.LogError("[ToppingPopupHandler] ❌ manager 또는 CurrentHotdog가 null!");
                return;
            }

            GameObject mainHotdog = manager.CurrentHotdog;
            Debug.Log($"[ToppingPopupHandler] 메인 핫도그에 토핑 반영 시작 - {mainHotdog.name}");

            // 설탕 반영
            if (hasSugar)
            {
                if (manager.sugarOverlaySprite == null)
                {
                    Debug.LogError("[ToppingPopupHandler] ❌ sugarOverlaySprite가 SimpleCookingManager에 할당되지 않았습니다!");
                }
                else
                {
                    GameObject sugar = new GameObject("Sugar_Overlay");
                    sugar.transform.SetParent(mainHotdog.transform);
                    sugar.transform.localPosition = manager.sugarPositionOffset;
                    sugar.transform.localRotation = Quaternion.identity;
                    sugar.transform.localScale = manager.sugarScale;

                    SpriteRenderer sr = sugar.AddComponent<SpriteRenderer>();
                    sr.sprite = manager.sugarOverlaySprite;
                    sr.sortingOrder = 15; // 핫도그보다 위

                    Debug.Log($"[ToppingPopupHandler] ✅ 메인 핫도그에 설탕 반영 - 위치: {manager.sugarPositionOffset}, 스케일: {manager.sugarScale}");
                }
            }

            // 소스 반영
            if (sauceDrawer != null)
            {
                List<SauceDotData> sauceDots = sauceDrawer.GetAllSauceDots();
                
                foreach (var dotData in sauceDots)
                {
                    // 스케일 조정 (팝업 → 메인 화면) + 오프셋 적용
                    Vector3 scaledPos = dotData.localPosition * manager.sauceScaleRatio + manager.saucePositionOffset;

                    GameObject dot = new GameObject($"Sauce_{dotData.type}");
                    dot.transform.SetParent(mainHotdog.transform);
                    dot.transform.localPosition = scaledPos;
                    dot.transform.localScale = Vector3.one * dotData.scale * manager.sauceScaleRatio;

                    SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
                    sr.sprite = dotData.type == ToppingType.Ketchup 
                        ? manager.ketchupDotSprite 
                        : manager.mustardDotSprite;
                    sr.sortingOrder = dotData.type == ToppingType.Ketchup ? 16 : 17;
                }

                Debug.Log($"[ToppingPopupHandler] ✅ 메인 핫도그에 소스 {sauceDots.Count}개 반영");
            }
        }
    }
}