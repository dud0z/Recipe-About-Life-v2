using UnityEngine;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// Phase 1: 꼬치 들기 핸들러
    /// 꼬치통에 부착 - 클릭하면 꼬치 생성 후 드래그 시작
    /// </summary>
    public class StickPickupHandler : MonoBehaviour
    {
        [Header("References")]
        public SimpleDropZone cuttingBoardZone;  // 도마 드롭존

        private SimpleCookingManager manager;
        private GameObject currentStick;
        private SimpleDraggable currentDraggable;
        private bool isActive = false;
        private UnityEngine.Camera mainCamera;

        private void Start()
        {
            manager = SimpleCookingManager.Instance;
            mainCamera = UnityEngine.Camera.main;

            if (manager != null)
            {
                manager.OnPhaseChanged += OnPhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnPhaseChanged -= OnPhaseChanged;
            }
        }

        private void OnPhaseChanged(CookingPhase phase)
        {
            isActive = (phase == CookingPhase.StickPickup);
            Debug.Log($"[StickPickupHandler] Active: {isActive}");
        }

        private void OnMouseDown()
        {
            if (!isActive || manager == null) return;
            if (manager.CurrentPhase != CookingPhase.StickPickup) return;

            CreateAndStartDrag();
        }

        /// <summary>
        /// 꼬치 생성 및 드래그 시작
        /// </summary>
        private void CreateAndStartDrag()
        {
            if (manager.stickPrefab == null)
            {
                Debug.LogError("[StickPickupHandler] Stick prefab is null!");
                return;
            }

            // 스틱 뽑는 소리 재생
            AudioManager.Instance?.PlayStickPickup();

            // 마우스 위치에 꼬치 생성
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            currentStick = Instantiate(manager.stickPrefab, mousePos, Quaternion.Euler(0, 0, 90)); // 세로로 생성
            currentStick.name = "Stick";

            // SimpleDraggable 추가
            currentDraggable = currentStick.GetComponent<SimpleDraggable>();
            if (currentDraggable == null)
            {
                currentDraggable = currentStick.AddComponent<SimpleDraggable>();
            }

            // 이벤트 구독
            currentDraggable.OnDragEnded += OnStickDropped;

            // 드래그 시작
            currentDraggable.StartDrag();

            Debug.Log("[StickPickupHandler] 꼬치 생성 및 드래그 시작");
        }

        private void Update()
        {
            // 드래그 중이면 계속 업데이트
            if (currentDraggable != null && currentDraggable.IsDragging)
            {
                currentDraggable.UpdateDrag();

                // 마우스 버튼 놓으면 드래그 종료
                if (Input.GetMouseButtonUp(0))
                {
                    currentDraggable.EndDrag();
                }
            }
        }

        /// <summary>
        /// 꼬치 드롭 처리
        /// </summary>
        private void OnStickDropped(SimpleDraggable draggable, SimpleDropZone dropZone)
        {
            // 이벤트 구독 해제
            draggable.OnDragEnded -= OnStickDropped;

            // 도마에 드롭 성공
            if (dropZone != null && dropZone.zoneType == SimpleDropZone.ZoneType.CuttingBoard)
            {
                Debug.Log("[StickPickupHandler] ✅ 도마에 드롭 성공!");

                // 가로로 회전, 도마 중앙에 배치
                draggable.SetRotation(0);
                draggable.MoveTo(dropZone.GetSnapPosition());
                draggable.isDraggable = false;  // 일단 드래그 비활성화

                // 매니저에 현재 핫도그 설정
                manager.SetCurrentHotdog(currentStick);

                // 다음 단계로 (재료 선택 팝업)
                manager.NextPhase();
                manager.ShowIngredientPopup();
            }
            else
            {
                // 드롭 실패 - 꼬치 삭제
                Debug.Log("[StickPickupHandler] ❌ 드롭 실패 - 꼬치 삭제");
                Destroy(currentStick);
            }

            currentStick = null;
            currentDraggable = null;
        }
    }
}