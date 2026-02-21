using UnityEngine;
using RecipeAboutLife.Orders;
using RecipeAboutLife.Events;
using RecipeAboutLife.Managers;
using CookingOrderData = RecipeAboutLife.Cooking.OrderData;

namespace RecipeAboutLife.NPC
{
    /// <summary>
    /// NPC 주문 컨트롤러
    /// NPC가 정지했을 때 DialogueSet에서 주문을 가져와 말풍선 UI에 표시하고
    /// 요리 시스템에 주문 전달
    /// </summary>
    public class NPCOrderController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField]
        [Tooltip("NPC 대화 세트 (주문 정보 포함)")]
        private Dialogue.NPCDialogueSet dialogueSet;

        [SerializeField]
        [Tooltip("NPC 대화 컨트롤러")]
        private Dialogue.NPCDialogueController dialogueController;

        [SerializeField]
        [Tooltip("말풍선 UI (DialogueBubbleUI 사용)")]
        private UI.DialogueBubbleUI dialogueBubbleUI;

        [Header("상태")]
        private OrderData currentOrder;
        private bool hasOrder = false;
        private bool isWaitingForFood = false;

        private void Awake()
        {
            // DialogueBubbleUI 자동 찾기 (비활성화된 것도 찾기)
            if (dialogueBubbleUI == null)
            {
                dialogueBubbleUI = GetComponentInChildren<UI.DialogueBubbleUI>(true);
                if (dialogueBubbleUI == null)
                {
                    Debug.LogWarning("[NPCOrderController] DialogueBubbleUI를 찾을 수 없습니다!");
                }
            }

            // NPCDialogueController 자동 찾기
            if (dialogueController == null)
            {
                dialogueController = GetComponent<Dialogue.NPCDialogueController>();
                if (dialogueController == null)
                {
                    Debug.LogWarning("[NPCOrderController] NPCDialogueController를 찾을 수 없습니다!");
                }
            }

            // DialogueSet 자동 찾기 (NPCDialogueController에서 가져오기)
            if (dialogueSet == null && dialogueController != null)
            {
                dialogueSet = dialogueController.GetDialogueSet();
                if (dialogueSet == null)
                {
                    Debug.LogError("[NPCOrderController] DialogueSet을 찾을 수 없습니다!");
                }
            }
        }

        private void OnEnable()
        {
            // 대화 종료 이벤트 구독
            if (dialogueController != null)
            {
                dialogueController.OnDialogueEnded += OnDialogueEnded;
            }
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (dialogueController != null)
            {
                dialogueController.OnDialogueEnded -= OnDialogueEnded;
            }
        }

        /// <summary>
        /// 대화 종료 이벤트 처리
        /// </summary>
        private void OnDialogueEnded(Dialogue.DialogueType dialogueType)
        {
            // Order 대화가 끝나면 요리 시스템 시작
            if (dialogueType == Dialogue.DialogueType.Order)
            {
                StartCookingSystem();
            }
        }

        /// <summary>
        /// 요리 시스템 시작
        /// </summary>
        private void StartCookingSystem()
        {
            if (Cooking.SimpleCookingManager.Instance != null)
            {
                Cooking.SimpleCookingManager.Instance.StartCooking();
                Debug.Log("[NPCOrderController] 요리 시스템 시작! 플레이어가 요리할 수 있습니다.");
            }
            else
            {
                Debug.LogError("[NPCOrderController] SimpleCookingManager를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// NPC가 정지했을 때 호출 - 주문 요청
        /// </summary>
        public void OnNPCStopped()
        {
            if (hasOrder)
            {
                Debug.LogWarning("[NPCOrderController] 이미 주문을 받았습니다.");
                return;
            }

            RequestOrder();
        }

        /// <summary>
        /// 주문 요청 - DialogueSet에서 주문 가져오기
        /// </summary>
        private void RequestOrder()
        {
            if (dialogueSet == null)
            {
                Debug.LogError("[NPCOrderController] DialogueSet이 없어서 주문을 받을 수 없습니다!");
                return;
            }

            // DialogueSet에서 주문 가져오기
            currentOrder = dialogueSet.npcOrder;

            if (currentOrder != null)
            {
                hasOrder = true;

                // Order 대화 시작 (주문 내용은 대화문에 포함됨)
                if (dialogueController != null)
                {
                    bool hasOrderDialogue = dialogueController.StartDialogue(Dialogue.DialogueType.Order);
                    if (hasOrderDialogue)
                    {
                        Debug.Log("[NPCOrderController] Order 대화 시작");
                    }
                    else
                    {
                        Debug.LogWarning("[NPCOrderController] Order 대화가 없습니다! 바로 요리 시스템 시작");
                        StartCookingSystem();
                    }
                }
                else
                {
                    Debug.LogWarning("[NPCOrderController] DialogueController가 없습니다! 바로 요리 시스템 시작");
                    StartCookingSystem();
                }

                // 주문 UI는 표시하지 않음 (대화문에 이미 주문 내용 포함)
                // DisplayOrder(); // 주석 처리

                // 주문을 요리 시스템으로 전달
                SendOrderToCookingSystem();

                // 주문 상세 로그
                string fill1 = currentOrder.FillingSlot1 == FillingType.HalfSausage ? "Sausage" : "Cheese";
                string fill2 = currentOrder.FillingSlot2 == FillingType.HalfSausage ? "Sausage" : "Cheese";
                string sauces = "";
                foreach (var req in currentOrder.SauceRequirements)
                {
                    string name = req.SauceType == SauceType.Ketchup ? "케첩" : "머스타드";
                    sauces += $"{name}({req.MinAmount}) ";
                }
                if (string.IsNullOrEmpty(sauces)) sauces = "없음";

                Debug.Log($"[NPCOrderController] ===== NPC 주문 접수 =====");
                Debug.Log($"  주문명: {currentOrder.OrderName}");
                Debug.Log($"  재료: {fill1} + {fill2}");
                Debug.Log($"  설탕: {(currentOrder.NeedSugar ? "O" : "X")}");
                Debug.Log($"  소스: {sauces.Trim()}");
            }
            else
            {
                Debug.LogError($"[NPCOrderController] DialogueSet({dialogueSet.npcID})에 주문 정보가 없습니다!");
            }
        }

        /// <summary>
        /// 주문을 요리 시스템으로 전달
        /// </summary>
        private void SendOrderToCookingSystem()
        {
            if (currentOrder == null)
            {
                Debug.LogError("[NPCOrderController] No order data!");
                return;
            }

            // ScoreManager에 주문 전달
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.SetActiveOrder(currentOrder);
                isWaitingForFood = true;

                Debug.Log($"[NPCOrderController] Order sent to ScoreManager: {currentOrder.OrderName}");
            }
            else
            {
                Debug.LogError("[NPCOrderController] ScoreManager not found!");
            }

            // GameManager에도 주문 전달 (ScoreCalculator 연동)
            if (GameManager.Instance != null)
            {
                var cookingOrder = ConvertToCookingOrder(currentOrder);
                GameManager.Instance.SetCurrentOrder(cookingOrder);
                Debug.Log($"[NPCOrderController] Order sent to GameManager: {cookingOrder}");
            }
        }

        /// <summary>
        /// Orders.OrderData → Cooking.OrderData 변환
        /// </summary>
        private CookingOrderData ConvertToCookingOrder(OrderData order)
        {
            bool wantsKetchup = false;
            bool wantsMustard = false;
            foreach (var req in order.SauceRequirements)
            {
                if (req.SauceType == SauceType.Ketchup) wantsKetchup = true;
                if (req.SauceType == SauceType.Mustard) wantsMustard = true;
            }

            return new CookingOrderData(
                fill1: order.FillingSlot1 == FillingType.HalfSausage ? "Sausage" : "Cheese",
                fill2: order.FillingSlot2 == FillingType.HalfSausage ? "Sausage" : "Cheese",
                sugar: order.NeedSugar,
                ketchup: wantsKetchup,
                mustard: wantsMustard
            );
        }

        /// <summary>
        /// 말풍선 UI에 주문 표시
        /// </summary>
        private void DisplayOrder()
        {
            if (dialogueBubbleUI == null)
            {
                Debug.LogWarning("[NPCOrderController] DialogueBubbleUI가 없어서 주문을 표시할 수 없습니다!");
                return;
            }

            // Canvas가 꺼져있으면 켜기
            if (!dialogueBubbleUI.gameObject.activeSelf)
            {
                dialogueBubbleUI.gameObject.SetActive(true);
                Debug.Log("[NPCOrderController] DialogueCanvas를 활성화했습니다.");
            }

            // DialogueBubbleUI의 ShowOrder 메서드 사용
            dialogueBubbleUI.ShowOrder(currentOrder);
        }

        /// <summary>
        /// 현재 주문 가져오기
        /// </summary>
        public OrderData GetCurrentOrder()
        {
            return currentOrder;
        }

        /// <summary>
        /// 주문 초기화
        /// </summary>
        public void ClearOrder()
        {
            currentOrder = null;
            hasOrder = false;
            isWaitingForFood = false;

            // GameManager 주문도 정리
            if (GameManager.Instance != null)
                GameManager.Instance.ClearCurrentOrder();

            if (dialogueBubbleUI != null)
            {
                dialogueBubbleUI.Hide();
            }
        }

        /// <summary>
        /// 음식 제공 대기 중인지 확인
        /// </summary>
        public bool IsWaitingForFood()
        {
            return isWaitingForFood;
        }

        /// <summary>
        /// 음식 서빙 완료 처리 (외부에서 호출)
        /// </summary>
        /// <param name="isSuccess">서빙 성공 여부 (true: 성공, false: 실패)</param>
        public void OnFoodServed(bool isSuccess)
        {
            if (!isWaitingForFood)
            {
                Debug.LogWarning("[NPCOrderController] 음식을 기다리고 있지 않습니다!");
                return;
            }

            isWaitingForFood = false;

            // 서빙 결과에 따라 대화 시작
            if (dialogueController != null)
            {
                Dialogue.DialogueType dialogueType = isSuccess ?
                    Dialogue.DialogueType.ServedSuccess :
                    Dialogue.DialogueType.ServedFail;

                bool hasDialogue = dialogueController.StartDialogue(dialogueType);
                if (hasDialogue)
                {
                    Debug.Log($"[NPCOrderController] {dialogueType} 대화 시작");
                }
                else
                {
                    Debug.LogWarning($"[NPCOrderController] {dialogueType} 대화가 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning("[NPCOrderController] NPCDialogueController가 없어서 서빙 대화를 표시할 수 없습니다!");
            }

            Debug.Log($"[NPCOrderController] 음식 서빙 완료 - 성공: {isSuccess}");
        }

    }
}
