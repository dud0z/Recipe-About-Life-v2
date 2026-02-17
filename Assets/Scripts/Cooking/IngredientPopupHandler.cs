using UnityEngine;
using UnityEngine.UI;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// Phase 2: 재료 선택 팝업 핸들러
    /// 전체화면 팝업에서 재료 2개를 꼬치에 끼움
    /// </summary>
    public class IngredientPopupHandler : MonoBehaviour
    {
        [Header("=== Stick Display ===")]
        public Transform stickDisplay;       // 팝업 내 꼬치 표시 위치
        public Transform ingredient1Pos;     // 첫번째 재료 위치
        public Transform ingredient2Pos;     // 두번째 재료 위치

        [Header("=== Ingredient Sources (드래그 시작점) ===")]
        public IngredientSource sausageSource;
        public IngredientSource cheeseSource;

        [Header("=== Drop Zone ===")]
        public SimpleDropZone stickDropZone; // 꼬치 위 드롭존

        [Header("=== Prefabs ===")]
        public GameObject sausagePrefab;     // 소시지 프리팹
        public GameObject cheesePrefab;      // 치즈 프리팹

        private SimpleCookingManager manager;
        private int ingredientCount = 0;
        private GameObject currentDragging;
        private string currentIngredientType;
        private UnityEngine.Camera mainCamera;

        private void Start()
        {
            manager = SimpleCookingManager.Instance;
            mainCamera = UnityEngine.Camera.main;

            // 재료 소스 이벤트 연결
            if (sausageSource != null)
            {
                sausageSource.OnDragStarted += () => OnIngredientDragStart("Sausage", sausagePrefab);
            }
            if (cheeseSource != null)
            {
                cheeseSource.OnDragStarted += () => OnIngredientDragStart("Cheese", cheesePrefab);
            }
        }

        private void OnEnable()
        {
            // 팝업 열릴 때 초기화
            ingredientCount = 0;
            currentDragging = null;
            Debug.Log("[IngredientPopupHandler] 팝업 열림 - 재료 선택 시작");

            // 메인 UI 숨김 (MoneyPanel, PauseButton, DayImage)
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.HideMainUI();
            }
        }

        private void OnDisable()
        {
            // 팝업 닫힐 때 메인 UI 표시
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowMainUI();
            }
        }

        /// <summary>
        /// 재료 드래그 시작
        /// </summary>
        private void OnIngredientDragStart(string ingredientType, GameObject prefab)
        {
            if (ingredientCount >= 2)
            {
                Debug.Log("[IngredientPopupHandler] 이미 2개 완료됨");
                return;
            }

            if (currentDragging != null)
            {
                Debug.Log("[IngredientPopupHandler] 이미 드래그 중");
                return;
            }

            if (prefab == null)
            {
                Debug.LogError($"[IngredientPopupHandler] {ingredientType} prefab is null!");
                return;
            }

            // 마우스 위치에 재료 생성
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            currentDragging = Instantiate(prefab, mousePos, Quaternion.identity, transform);
            currentIngredientType = ingredientType;

            // SimpleDraggable 추가
            SimpleDraggable draggable = currentDragging.GetComponent<SimpleDraggable>();
            if (draggable == null)
            {
                draggable = currentDragging.AddComponent<SimpleDraggable>();
            }

            draggable.OnDragEnded += OnIngredientDropped;
            draggable.StartDrag();

            Debug.Log($"[IngredientPopupHandler] {ingredientType} 드래그 시작");
        }

        private void Update()
        {
            // 드래그 중 업데이트
            if (currentDragging != null)
            {
                SimpleDraggable draggable = currentDragging.GetComponent<SimpleDraggable>();
                if (draggable != null && draggable.IsDragging)
                {
                    draggable.UpdateDrag();

                    if (Input.GetMouseButtonUp(0))
                    {
                        draggable.EndDrag();
                    }
                }
            }
        }

        /// <summary>
        /// 재료 드롭 처리
        /// </summary>
        private void OnIngredientDropped(SimpleDraggable draggable, SimpleDropZone dropZone)
        {
            draggable.OnDragEnded -= OnIngredientDropped;

            // 꼬치 위에 드롭 성공
            if (dropZone != null && dropZone.zoneType == SimpleDropZone.ZoneType.StickDropZone)
            {
                Debug.Log($"[IngredientPopupHandler] ✅ {currentIngredientType} 꼬치에 드롭 성공!");

                // 재료 끼우는 소리 재생
                AudioManager.Instance?.PlayIngredientAttach();

                // 재료 위치 설정
                Transform targetPos = ingredientCount == 0 ? ingredient1Pos : ingredient2Pos;
                if (targetPos != null)
                {
                    draggable.MoveTo(targetPos.position);
                }

                draggable.isDraggable = false;

                // 데이터 저장
                if (manager != null)
                {
                    if (ingredientCount == 0)
                        manager.CurrentHotdogData.filling1 = currentIngredientType;
                    else
                        manager.CurrentHotdogData.filling2 = currentIngredientType;
                }

                ingredientCount++;

                // 2개 완료 시 팝업 종료
                if (ingredientCount >= 2)
                {
                    Invoke(nameof(CompleteIngredientPhase), 0.5f); // 잠시 대기 후 종료
                }
            }
            else
            {
                // 드롭 실패 - 재료 삭제
                Debug.Log($"[IngredientPopupHandler] ❌ {currentIngredientType} 드롭 실패");
                Destroy(currentDragging);
            }

            currentDragging = null;
            currentIngredientType = null;
        }

        /// <summary>
        /// 재료 선택 완료
        /// </summary>
        private void CompleteIngredientPhase()
        {
            Debug.Log("[IngredientPopupHandler] ✅ 재료 2개 완료!");

            // 메인 화면 스틱에 재료 붙이기
            AttachIngredientsToMainStick();

            // 팝업 닫기
            manager.HideIngredientPopup();

            // 다음 단계로
            manager.NextPhase();
        }

        /// <summary>
        /// 메인 화면 스틱에 재료 프리팹을 자식으로 붙이기
        /// </summary>
        private void AttachIngredientsToMainStick()
        {
            GameObject stick = manager.CurrentHotdog;
            if (stick == null)
            {
                Debug.LogError("[IngredientPopupHandler] ❌ CurrentHotdog가 null!");
                return;
            }

            // 스틱 Z 위치 조정 (클릭 우선순위를 위해 카메라에 더 가깝게)
            Vector3 pos = stick.transform.position;
            pos.z = -1f;
            stick.transform.position = pos;
            Debug.Log("[IngredientPopupHandler] 스틱 Z 위치를 -1로 조정 (클릭 우선순위)");

            // 스틱 Sorting Order 설정
            SpriteRenderer stickSr = stick.GetComponent<SpriteRenderer>();
            if (stickSr != null)
            {
                stickSr.sortingOrder = 10;
            }

            // 첫번째 재료 생성 (스틱의 자식으로)
            CreateIngredientOnStick(
                manager.CurrentHotdogData.filling1, 
                manager.mainIngredient1Offset, 
                stick,
                11  // 스틱보다 앞에
            );

            // 두번째 재료 생성 (스틱의 자식으로)
            CreateIngredientOnStick(
                manager.CurrentHotdogData.filling2, 
                manager.mainIngredient2Offset, 
                stick,
                12  // 첫번째 재료보다 앞에
            );

            // SimpleDraggable 드래그 활성화 (없으면 추가)
            SimpleDraggable draggable = stick.GetComponent<SimpleDraggable>();
            if (draggable == null)
            {
                draggable = stick.AddComponent<SimpleDraggable>();
                Debug.Log("[IngredientPopupHandler] SimpleDraggable 컴포넌트 추가됨");
            }
            draggable.isDraggable = true;
            Debug.Log("[IngredientPopupHandler] 스틱 드래그 활성화");

            // 재료 완료 후 스케일 적용
            stick.transform.localScale = manager.ingredientCompleteScale;
            Debug.Log($"[IngredientPopupHandler] 재료 완료 스케일 적용: {manager.ingredientCompleteScale}");
            
            // Collider2D 확인 (없으면 드래그 감지 불가)
            Collider2D collider = stick.GetComponent<Collider2D>();
            if (collider == null)
            {
                BoxCollider2D boxCollider = stick.AddComponent<BoxCollider2D>();
                // SpriteRenderer 기준으로 크기 자동 설정
                SpriteRenderer sr = stick.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    boxCollider.size = sr.sprite.bounds.size;
                }
                Debug.Log("[IngredientPopupHandler] BoxCollider2D 추가됨");
            }

            // BatterHandler 동적 추가 (없으면)
            BatterHandler batterHandler = stick.GetComponent<BatterHandler>();
            if (batterHandler == null)
            {
                batterHandler = stick.AddComponent<BatterHandler>();
                Debug.Log("[IngredientPopupHandler] BatterHandler 추가됨");
            }
            
            // BatterHandler에 반죽통 영역 설정
            if (manager.batterZone != null)
            {
                SimpleDropZone batterZone = manager.batterZone.GetComponent<SimpleDropZone>();
                if (batterZone != null)
                {
                    batterHandler.batterZone = batterZone;
                }
            }

            Debug.Log($"[IngredientPopupHandler] 메인 화면 스틱에 재료 부착 완료: {manager.CurrentHotdogData.filling1}, {manager.CurrentHotdogData.filling2}");
        }

        /// <summary>
        /// 스틱에 재료 프리팹 생성
        /// </summary>
        private void CreateIngredientOnStick(string ingredientType, Vector3 offset, GameObject stick, int sortingOrder)
        {
            if (string.IsNullOrEmpty(ingredientType))
            {
                Debug.LogWarning("[IngredientPopupHandler] ⚠️ ingredientType이 비어있음!");
                return;
            }

            // 재료 타입에 따라 프리팹 선택
            GameObject prefab = null;
            if (ingredientType == "Sausage")
                prefab = manager.mainSausagePrefab;
            else if (ingredientType == "Cheese")
                prefab = manager.mainCheesePrefab;

            if (prefab == null)
            {
                Debug.LogError($"[IngredientPopupHandler] ❌ {ingredientType} 메인 프리팹이 SimpleCookingManager에 할당되지 않았습니다!");
                return;
            }

            // 스틱의 자식으로 생성
            GameObject ingredient = Instantiate(prefab, stick.transform);
            ingredient.name = $"Ingredient_{ingredientType}";
            
            // 로컬 위치 설정 (스틱 기준 상대 위치)
            ingredient.transform.localPosition = offset;
            ingredient.transform.localRotation = Quaternion.identity;

            // Sorting Order 설정
            SpriteRenderer sr = ingredient.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = sortingOrder;
                Debug.Log($"[IngredientPopupHandler] ✅ {ingredientType} 생성 완료 - 오프셋: {offset}, SortingOrder: {sortingOrder}");
            }
            else
            {
                Debug.LogWarning($"[IngredientPopupHandler] ⚠️ {ingredientType} 프리팹에 SpriteRenderer가 없습니다!");
            }
        }
    }

}