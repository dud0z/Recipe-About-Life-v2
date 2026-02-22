using System.Collections.Generic;
using UnityEngine;
using RecipeAboutLife.Events;
using RecipeAboutLife.Cooking;
using RecipeAboutLife.Orders;
using RecipeAboutLife.Utilities;

namespace RecipeAboutLife.Managers
{
    /// <summary>
    /// 점수 및 재화 관리 시스템
    /// 5명의 NPC에게 음식을 제공하고 재화를 획득
    /// 목표 재화 달성 시 플레이어 대화 잠금 해제
    ///
    /// SimpleCookingManager와 직접 연동하여 주문 검증 및 보상 계산
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        // ==========================================
        // Singleton Pattern
        // ==========================================

        private static ScoreManager _instance;
        public static ScoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ScoreManager>();
                }
                return _instance;
            }
        }

        // ==========================================
        // Configuration
        // ==========================================

        [Header("목표 설정")]
        [SerializeField]
        [Tooltip("대화 잠금 해제에 필요한 최소 재화")]
        private int targetTotalReward = 400;

        [SerializeField]
        [Tooltip("스테이지당 NPC 수")]
        private int maxNPCCount = 5;

        // ==========================================
        // Current State
        // ==========================================

        [Header("현재 상태 (Debug)")]
        [SerializeField]
        private int totalReward = 0;

        [SerializeField]
        private int currentNPCIndex = 0;

        [SerializeField]
        private bool isDialogueUnlocked = false;

        [SerializeField]
        private List<NPCRewardData> npcRewards = new List<NPCRewardData>();

        // ==========================================
        // Order Bridge Integration
        // ==========================================

        [Header("주문 관리 (OrderBridge 통합)")]
        [SerializeField]
        private Orders.OrderData activeOrder;

        [SerializeField]
        private bool isOrderServed = false;

        /// <summary>
        /// SimpleCookingManager에서 전달받은 ScoreCalculator 기반 점수
        /// OnRecipeCompleted에서 사용 (HotdogRecipe.CalculateReward 대신)
        /// </summary>
        private int lastCalculatedScore = 0;

        // ==========================================
        // Events
        // ==========================================

        /// <summary>
        /// 재화가 변경되었을 때 발생 (UI 업데이트용)
        /// </summary>
        public System.Action<int> OnTotalRewardChanged;

        /// <summary>
        /// NPC에게 재화를 지급했을 때 발생
        /// </summary>
        public System.Action<int, int> OnNPCRewarded; // (npcIndex, reward)

        /// <summary>
        /// 대화가 잠금 해제되었을 때 발생
        /// </summary>
        public System.Action OnDialogueUnlocked;

        /// <summary>
        /// 스테이지가 완료되었을 때 발생
        /// </summary>
        public System.Action<bool> OnStageCompleted; // (success)

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

            InitializeNPCRewards();
        }

        private void Start()
        {
            // SimpleCookingManager 이벤트 구독 (Awake에서는 Instance가 없을 수 있음)
            if (SimpleCookingManager.Instance != null)
            {
                SimpleCookingManager.Instance.OnHotdogServed += OnHotdogServedHandler;
                DebugLogger.Log("[ScoreManager] SimpleCookingManager.OnHotdogServed 구독 완료");
            }
            else
            {
                DebugLogger.LogWarning("[ScoreManager] SimpleCookingManager.Instance를 찾을 수 없습니다!");
            }

            // GameManager의 dayGoals와 목표 금액 동기화
            if (GameManager.Instance != null)
            {
                targetTotalReward = GameManager.Instance.GetCurrentDayGoal();
                DebugLogger.Log($"[ScoreManager] 목표 금액을 GameManager에서 동기화: {targetTotalReward}");
            }
        }

        private void OnEnable()
        {
            // 요리 완료 이벤트 구독 (내부 이벤트)
            GameEvents.OnRecipeCompleted += OnRecipeCompleted;
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            GameEvents.OnRecipeCompleted -= OnRecipeCompleted;

            if (SimpleCookingManager.Instance != null)
            {
                SimpleCookingManager.Instance.OnHotdogServed -= OnHotdogServedHandler;
            }
        }

        private void OnDestroy()
        {
            // 씬 전환 시 static 참조 명시적 정리
            if (_instance == this)
                _instance = null;
        }

        // ==========================================
        // Initialization
        // ==========================================

        /// <summary>
        /// NPC 보상 데이터 초기화
        /// </summary>
        private void InitializeNPCRewards()
        {
            npcRewards.Clear();

            for (int i = 0; i < maxNPCCount; i++)
            {
                npcRewards.Add(new NPCRewardData
                {
                    npcIndex = i + 1,
                    reward = 0,
                    quality = 0,
                    orderMatched = false,
                    isServed = false
                });
            }

            DebugLogger.Log($"[ScoreManager] Initialized {maxNPCCount} NPC reward slots");
        }

        // ==========================================
        // Order Management (OrderBridge 통합)
        // ==========================================

        /// <summary>
        /// 활성 주문 설정 (NPCOrderController에서 호출)
        /// </summary>
        public void SetActiveOrder(Orders.OrderData order)
        {
            if (order == null)
            {
                Debug.LogError("[ScoreManager] Cannot set null order!");
                return;
            }

            activeOrder = order;
            isOrderServed = false;

            DebugLogger.Log($"[ScoreManager] Active order set: {order.OrderName}\n{order.GetOrderDescription()}");
        }

        /// <summary>
        /// 주문 초기화
        /// </summary>
        public void ClearOrder()
        {
            activeOrder = null;
            isOrderServed = false;
            DebugLogger.Log("[ScoreManager] Order cleared");
        }

        /// <summary>
        /// 핫도그 서빙 이벤트 처리
        /// SimpleCookingManager.OnHotdogServed 이벤트 발생 시 호출됨
        /// </summary>
        /// <param name="earnedMoney">SimpleCookingManager가 ScoreCalculator로 계산한 획득 금액</param>
        private void OnHotdogServedHandler(int earnedMoney)
        {
            // 중복 서빙 방지
            if (isOrderServed)
            {
                DebugLogger.LogWarning("[ScoreManager] Order already served! Ignoring duplicate.");
                return;
            }

            // HotdogData 가져오기
            if (SimpleCookingManager.Instance == null)
            {
                Debug.LogError("[ScoreManager] SimpleCookingManager.Instance is null!");
                return;
            }

            HotdogData hotdog = SimpleCookingManager.Instance.CurrentHotdogData;
            if (hotdog == null)
            {
                Debug.LogError("[ScoreManager] CurrentHotdogData is null!");
                return;
            }

            DebugLogger.Log($"[ScoreManager] Hotdog served:\n" +
                     $"  Filling: {hotdog.filling1} + {hotdog.filling2}\n" +
                     $"  Batter: {hotdog.batterStage}\n" +
                     $"  Frying: {hotdog.fryingState} ({hotdog.fryingTime:F1}s)\n" +
                     $"  Sugar: {hotdog.hasSugar}\n" +
                     $"  Ketchup: {hotdog.hasKetchup}\n" +
                     $"  Mustard: {hotdog.hasMustard}");

            // HotdogData → HotdogRecipe 변환 + 검증
            HotdogRecipe recipe = CreateRecipeFromHotdog(hotdog);

            // SimpleCookingManager가 ScoreCalculator로 계산한 점수 저장
            lastCalculatedScore = earnedMoney;

            // 서빙 완료 표시
            isOrderServed = true;

            // GameEvents.OnRecipeCompleted 발생
            // → OnRecipeCompleted()가 호출되어 보상 처리 진행
            GameEvents.TriggerRecipeCompleted(recipe);

            DebugLogger.Log($"[ScoreManager] Recipe created and event triggered!\n" +
                     $"  Quality: {recipe.quality:F1}/100\n" +
                     $"  Matches Order: {recipe.matchesOrder}\n" +
                     $"  ScoreCalculator 점수: {earnedMoney}원");
        }

        /// <summary>
        /// HotdogData를 HotdogRecipe로 변환 + OrderData와 비교
        /// </summary>
        private HotdogRecipe CreateRecipeFromHotdog(HotdogData hotdog)
        {
            HotdogRecipe recipe = new HotdogRecipe
            {
                hasStick = true,
                batterAmount = hotdog.batterStage * 33f,
                fryingTime = hotdog.fryingTime,
                fryingColor = ConvertFryingState(hotdog.fryingState),
                hasSugar = hotdog.hasSugar
            };

            // 소스 추가
            if (hotdog.hasKetchup)
            {
                recipe.sauces.Add(Cooking.SauceType.Ketchup);
                recipe.sauceAmounts[Cooking.SauceType.Ketchup] = 50f;
            }

            if (hotdog.hasMustard)
            {
                recipe.sauces.Add(Cooking.SauceType.Mustard);
                recipe.sauceAmounts[Cooking.SauceType.Mustard] = 50f;
            }

            // 주문이 있으면 검증
            if (activeOrder != null)
            {
                ValidationResult result = OrderValidator.Validate(hotdog, activeOrder);

                recipe.quality = result.quality;
                recipe.matchesOrder = result.isMatch;
                recipe.fillingType = DetermineFillingTypeFromOrder(activeOrder);

                DebugLogger.Log($"[ScoreManager] Order validation:\n" +
                         $"  Ingredient: {result.ingredientMatch}\n" +
                         $"  Topping: {result.toppingMatch}\n" +
                         $"  Sauce: {result.sauceMatch}\n" +
                         $"  Match: {result.isMatch}\n" +
                         $"  Quality: {result.quality:F1}/100");
            }
            else
            {
                // 주문 없이 요리한 경우
                recipe.quality = 50f;
                recipe.matchesOrder = false;
                recipe.fillingType = DetermineFillingTypeFromStrings(hotdog.filling1, hotdog.filling2);

                DebugLogger.LogWarning("[ScoreManager] No active order! Default quality applied.");
            }

            return recipe;
        }

        /// <summary>
        /// FryingState → FryingColor 변환
        /// </summary>
        private FryingColor ConvertFryingState(FryingState state)
        {
            switch (state)
            {
                case FryingState.Raw: return FryingColor.Raw;
                case FryingState.Yellow: return FryingColor.Yellow;
                case FryingState.Golden: return FryingColor.Golden;
                case FryingState.Brown: return FryingColor.Brown;
                case FryingState.Burnt: return FryingColor.Burnt;
                default: return FryingColor.Golden;
            }
        }

        /// <summary>
        /// OrderData로부터 FillingType 결정
        /// </summary>
        private Cooking.FillingType DetermineFillingTypeFromOrder(Orders.OrderData order)
        {
            if (order.FillingSlot1 == Orders.FillingType.HalfSausage &&
                order.FillingSlot2 == Orders.FillingType.HalfSausage)
                return Cooking.FillingType.Sausage;

            if (order.FillingSlot1 == Orders.FillingType.HalfCheese &&
                order.FillingSlot2 == Orders.FillingType.HalfCheese)
                return Cooking.FillingType.Cheese;

            return Cooking.FillingType.Mixed;
        }

        /// <summary>
        /// 문자열로부터 FillingType 결정
        /// </summary>
        private Cooking.FillingType DetermineFillingTypeFromStrings(string fill1, string fill2)
        {
            if (fill1 == "Sausage" && fill2 == "Sausage")
                return Cooking.FillingType.Sausage;

            if (fill1 == "Cheese" && fill2 == "Cheese")
                return Cooking.FillingType.Cheese;

            return Cooking.FillingType.Mixed;
        }

        // ==========================================
        // Recipe Completion Handler
        // ==========================================

        /// <summary>
        /// 요리 완료 이벤트 처리
        /// CookingManager에서 발생한 이벤트를 받아서 재화 지급
        /// </summary>
        private void OnRecipeCompleted(HotdogRecipe recipe)
        {
            if (currentNPCIndex >= maxNPCCount)
            {
                DebugLogger.LogWarning("[ScoreManager] 이미 모든 NPC에게 음식을 제공했습니다!");
                return;
            }

            // ScoreCalculator 기반 점수 사용 (SimpleCookingManager에서 전달받은 값)
            // HotdogRecipe.CalculateReward()는 별도 공식이므로 사용하지 않음
            int reward = lastCalculatedScore;

            // NPC 보상 데이터 저장
            NPCRewardData npcData = npcRewards[currentNPCIndex];
            npcData.reward = reward;
            npcData.quality = recipe.quality;
            npcData.orderMatched = recipe.matchesOrder;
            npcData.isServed = true;

            // 총 재화 증가
            totalReward += reward;

            DebugLogger.Log($"[ScoreManager] NPC {currentNPCIndex + 1} 보상 지급: {reward}원 (품질: {recipe.quality:F1}, 총합: {totalReward}원)");

            // 이벤트 발생
            OnNPCRewarded?.Invoke(currentNPCIndex + 1, reward);
            OnTotalRewardChanged?.Invoke(totalReward);

            // NPC에게 음식 서빙 완료 알림 (ServedSuccess/ServedFail 대화 트리거)
            NotifyNPCFoodServed(recipe.matchesOrder);

            // 다음 NPC로
            currentNPCIndex++;

            // 모든 NPC 완료 확인
            if (currentNPCIndex >= maxNPCCount)
            {
                OnAllNPCsServed();
            }
        }

        /// <summary>
        /// NPC에게 음식 서빙 완료 알림
        /// </summary>
        /// <param name="isSuccess">서빙 성공 여부</param>
        private void NotifyNPCFoodServed(bool isSuccess)
        {
            // 현재 NPC 찾기
            NPC.NPCSpawnManager spawnManager = FindFirstObjectByType<NPC.NPCSpawnManager>();
            if (spawnManager == null)
            {
                DebugLogger.LogWarning("[ScoreManager] NPCSpawnManager를 찾을 수 없습니다!");
                return;
            }

            GameObject currentNPC = spawnManager.GetCurrentNPC();
            if (currentNPC == null)
            {
                DebugLogger.LogWarning("[ScoreManager] 현재 NPC가 없습니다!");
                return;
            }

            // NPCOrderController 찾기
            NPC.NPCOrderController orderController = currentNPC.GetComponent<NPC.NPCOrderController>();
            if (orderController == null)
            {
                DebugLogger.LogWarning("[ScoreManager] NPCOrderController를 찾을 수 없습니다!");
                return;
            }

            // 음식 서빙 완료 알림 (대화 트리거)
            orderController.OnFoodServed(isSuccess);
        }

        // ==========================================
        // Stage Completion
        // ==========================================

        /// <summary>
        /// 모든 NPC에게 음식을 제공했을 때
        /// </summary>
        private void OnAllNPCsServed()
        {
            DebugLogger.Log($"[ScoreManager] 모든 NPC 완료! 총 재화: {totalReward}원 / 목표: {targetTotalReward}원");

            // 목표 달성 확인
            bool success = totalReward >= targetTotalReward;

            if (success)
            {
                UnlockDialogue();
            }
            else
            {
                DebugLogger.Log($"[ScoreManager] 목표 미달성! 부족한 재화: {targetTotalReward - totalReward}원");
            }

            // 스테이지 완료 이벤트
            OnStageCompleted?.Invoke(success);

            // 게임 이벤트 발생
            GameEvents.TriggerAllCustomersServed();
        }

        /// <summary>
        /// 대화 잠금 해제
        /// </summary>
        private void UnlockDialogue()
        {
            if (isDialogueUnlocked)
            {
                DebugLogger.LogWarning("[ScoreManager] 이미 대화가 잠금 해제되었습니다!");
                return;
            }

            isDialogueUnlocked = true;

            DebugLogger.Log("[ScoreManager] 대화 잠금 해제! 플레이어와 대화할 수 있습니다.");

            // 이벤트 발생
            OnDialogueUnlocked?.Invoke();

            // 알림 표시
            GameEvents.TriggerShowNotification("목표 달성! 대화가 가능합니다.");
        }

        // ==========================================
        // Public API
        // ==========================================

        /// <summary>
        /// 스테이지 재시작
        /// </summary>
        public void RestartStage()
        {
            totalReward = 0;
            currentNPCIndex = 0;
            isDialogueUnlocked = false;

            // 주문 초기화
            ClearOrder();

            InitializeNPCRewards();

            OnTotalRewardChanged?.Invoke(totalReward);

            DebugLogger.Log("[ScoreManager] 스테이지 재시작");
        }

        /// <summary>
        /// 현재 총 재화 가져오기
        /// </summary>
        public int GetTotalReward()
        {
            return totalReward;
        }

        /// <summary>
        /// 목표 재화 가져오기
        /// </summary>
        public int GetTargetReward()
        {
            return targetTotalReward;
        }

        /// <summary>
        /// 대화 잠금 해제 여부
        /// </summary>
        public bool IsDialogueUnlocked()
        {
            return isDialogueUnlocked;
        }

        /// <summary>
        /// NPC 보상 데이터 가져오기
        /// </summary>
        public List<NPCRewardData> GetNPCRewards()
        {
            return npcRewards;
        }

        /// <summary>
        /// 진행률 가져오기 (0-1)
        /// </summary>
        public float GetProgress()
        {
            return (float)totalReward / targetTotalReward;
        }

        // ==========================================
        // Public Test Methods (버튼에서 호출 가능)
        // ==========================================

        /// <summary>
        /// 테스트용: 완벽한 요리로 NPC 서빙 (최고 재화 획득)
        /// UI 버튼에서 호출 가능
        /// </summary>
        public void TestServePerfectFood()
        {
            if (currentNPCIndex >= maxNPCCount)
            {
                DebugLogger.Log("[TestButton] 모든 NPC 완료!");
                return;
            }

            // 완벽한 레시피 생성
            HotdogRecipe perfectRecipe = new HotdogRecipe
            {
                hasStick = true,
                fillingType = Cooking.FillingType.Sausage,
                batterAmount = 100f,
                fryingTime = 8f, // Golden 구간
                fryingColor = Cooking.FryingColor.Golden,
                hasSugar = false,
                quality = 100f,
                matchesOrder = true
            };

            // 최고 보상 계산 (100 * 1.0 * 1.5 = 150원)
            int reward = perfectRecipe.CalculateReward(null);

            // NPC 보상 데이터 저장
            NPCRewardData npcData = npcRewards[currentNPCIndex];
            npcData.reward = reward;
            npcData.quality = 100f;
            npcData.orderMatched = true;
            npcData.isServed = true;

            // 총 재화 증가
            totalReward += reward;

            DebugLogger.Log($"[TestButton] NPC {currentNPCIndex + 1} 완벽 서빙! 보상: {reward}원 (총: {totalReward}원)");

            // 이벤트 발생
            OnNPCRewarded?.Invoke(currentNPCIndex + 1, reward);
            OnTotalRewardChanged?.Invoke(totalReward);

            // NPC에게 음식 서빙 완료 알림 (ServedSuccess 대화 트리거)
            NotifyNPCFoodServed(true);

            // 다음 NPC로
            currentNPCIndex++;

            // 모든 NPC 완료 확인
            if (currentNPCIndex >= maxNPCCount)
            {
                OnAllNPCsServed();
            }
        }

        /// <summary>
        /// 테스트용: 현재 NPC의 주문에 완벽히 맞는 음식 제공 (최고 재화 획득)
        /// UI 버튼에서 호출 가능
        /// </summary>
        public void TestServePerfectFoodForCurrentOrder()
        {
            if (currentNPCIndex >= maxNPCCount)
            {
                DebugLogger.Log("[TestButton] 모든 NPC 완료!");
                return;
            }

            // 현재 NPC 가져오기
            NPC.NPCSpawnManager spawnManager = FindFirstObjectByType<NPC.NPCSpawnManager>();
            if (spawnManager == null)
            {
                Debug.LogError("[TestButton] NPCSpawnManager를 찾을 수 없습니다!");
                return;
            }

            GameObject currentNPC = spawnManager.GetCurrentNPC();
            if (currentNPC == null)
            {
                Debug.LogError("[TestButton] 현재 NPC가 없습니다!");
                return;
            }

            // NPC의 주문 가져오기
            NPC.NPCOrderController orderController = currentNPC.GetComponent<NPC.NPCOrderController>();
            if (orderController == null)
            {
                Debug.LogError("[TestButton] NPCOrderController를 찾을 수 없습니다!");
                return;
            }

            Orders.OrderData currentOrder = orderController.GetCurrentOrder();
            if (currentOrder == null)
            {
                Debug.LogError("[TestButton] 현재 주문이 없습니다!");
                return;
            }

            // 주문에 맞는 완벽한 레시피 생성
            HotdogRecipe perfectRecipe = CreatePerfectRecipe(currentOrder);

            // 최고 보상 계산
            int reward = perfectRecipe.CalculateReward(null);

            // NPC 보상 데이터 저장
            NPCRewardData npcData = npcRewards[currentNPCIndex];
            npcData.reward = reward;
            npcData.quality = 100f;
            npcData.orderMatched = true;
            npcData.isServed = true;

            // 총 재화 증가
            totalReward += reward;

            DebugLogger.Log($"[TestButton] NPC {currentNPCIndex + 1} 완벽 서빙! 주문: {currentOrder.GetOrderDescription()}, 보상: {reward}원 (총: {totalReward}원)");

            // 이벤트 발생
            OnNPCRewarded?.Invoke(currentNPCIndex + 1, reward);
            OnTotalRewardChanged?.Invoke(totalReward);

            // NPC에게 음식 서빙 완료 알림 (ServedSuccess 대화 트리거)
            if (orderController != null)
            {
                orderController.OnFoodServed(true); // 테스트는 항상 성공
                DebugLogger.Log("[TestButton] NPC 서빙 완료 알림 (ServedSuccess 대화)");
            }

            // NPC 퇴장 처리 (currentNPCIndex 증가 전에 실행)
            if (currentNPC != null)
            {
                NPC.NPCMovement movement = currentNPC.GetComponent<NPC.NPCMovement>();
                if (movement != null)
                {
                    movement.OnOrderComplete(); // NPC 퇴장 시작 (퇴장 완료 시 자동으로 다음 NPC 스폰됨)
                    DebugLogger.Log("[TestButton] NPC 퇴장 명령 전송");
                }
                else
                {
                    DebugLogger.LogWarning("[TestButton] NPCMovement를 찾을 수 없습니다!");
                }
            }

            // 다음 NPC로 (인덱스만 증가, 스폰은 NPC 퇴장 완료 후 자동으로 처리됨)
            currentNPCIndex++;

            // 모든 NPC 완료 확인
            if (currentNPCIndex >= maxNPCCount)
            {
                OnAllNPCsServed();
            }

            // 주의: TestButton은 정상 플레이를 우회하므로 GameEvents.TriggerRecipeCompleted를 호출하지 않음
            // 정상 플레이에서는 CookingManager가 이벤트를 발생시켜 ScoreManager.OnRecipeCompleted가 호출됨
            // TestButton에서는 이미 보상을 직접 처리했으므로 이벤트 발생 시 중복 처리가 발생함
        }

        /// <summary>
        /// 주문에 완벽히 맞는 레시피 생성
        /// </summary>
        private HotdogRecipe CreatePerfectRecipe(Orders.OrderData order)
        {
            // FillingType 결정 (OrderData → HotdogRecipe)
            Cooking.FillingType fillingType = DetermineFillingType(order.FillingSlot1, order.FillingSlot2);

            HotdogRecipe recipe = new HotdogRecipe
            {
                hasStick = true,
                fillingType = fillingType,
                batterAmount = 100f, // 완벽한 반죽 양
                fryingTime = 8f, // Golden 구간 (7-9초)
                fryingColor = Cooking.FryingColor.Golden,
                hasSugar = order.NeedSugar,
                quality = 100f,
                matchesOrder = true
            };

            // 소스 추가 (주문에 맞게)
            recipe.sauces = new System.Collections.Generic.List<Cooking.SauceType>();
            recipe.sauceAmounts = new System.Collections.Generic.Dictionary<Cooking.SauceType, float>();

            foreach (var sauceReq in order.SauceRequirements)
            {
                // Orders.SauceType → Cooking.SauceType 변환
                Cooking.SauceType cookingSauceType = sauceReq.SauceType == Orders.SauceType.Ketchup
                    ? Cooking.SauceType.Ketchup
                    : Cooking.SauceType.Mustard;

                recipe.sauces.Add(cookingSauceType);

                // 소스 양을 정확히 맞춤
                float amount = GetPerfectSauceAmount(sauceReq.MinAmount);
                recipe.sauceAmounts[cookingSauceType] = amount;
            }

            return recipe;
        }

        /// <summary>
        /// OrderData FillingType → HotdogRecipe FillingType 변환
        /// </summary>
        private Cooking.FillingType DetermineFillingType(Orders.FillingType slot1, Orders.FillingType slot2)
        {
            if (slot1 == Orders.FillingType.HalfSausage && slot2 == Orders.FillingType.HalfSausage)
                return Cooking.FillingType.Sausage;

            if (slot1 == Orders.FillingType.HalfCheese && slot2 == Orders.FillingType.HalfCheese)
                return Cooking.FillingType.Cheese;

            return Cooking.FillingType.Mixed;
        }

        /// <summary>
        /// SauceAmount에 맞는 완벽한 소스 양 반환
        /// </summary>
        private float GetPerfectSauceAmount(Orders.SauceAmount amount)
        {
            switch (amount)
            {
                case Orders.SauceAmount.Low:
                    return 20f; // Low: 0-33%, 중간값 사용
                case Orders.SauceAmount.Medium:
                    return 50f; // Medium: 34-66%, 중간값 사용
                case Orders.SauceAmount.High:
                    return 85f; // High: 67-100%, 중간값 사용
                default:
                    return 50f;
            }
        }

        // ==========================================
        // Debug
        // ==========================================

#if UNITY_EDITOR
        [ContextMenu("Test: Add Random Reward")]
        private void TestAddReward()
        {
            if (currentNPCIndex >= maxNPCCount)
            {
                Debug.Log("모든 NPC 완료!");
                return;
            }

            // 테스트용 랜덤 보상
            int testReward = Random.Range(50, 150);

            NPCRewardData npcData = npcRewards[currentNPCIndex];
            npcData.reward = testReward;
            npcData.quality = Random.Range(60f, 100f);
            npcData.orderMatched = Random.value > 0.5f;
            npcData.isServed = true;

            totalReward += testReward;
            currentNPCIndex++;

            Debug.Log($"[Test] NPC {currentNPCIndex} 보상: {testReward}원, 총합: {totalReward}원");

            if (currentNPCIndex >= maxNPCCount)
            {
                OnAllNPCsServed();
            }
        }

        [ContextMenu("Test: Restart Stage")]
        private void TestRestartStage()
        {
            RestartStage();
        }

        [ContextMenu("Log Current State")]
        private void LogCurrentState()
        {
            Debug.Log("=== ScoreManager State ===");
            Debug.Log($"Total Reward: {totalReward} / {targetTotalReward}");
            Debug.Log($"Progress: {GetProgress() * 100f:F1}%");
            Debug.Log($"Current NPC: {currentNPCIndex + 1} / {maxNPCCount}");
            Debug.Log($"Dialogue Unlocked: {isDialogueUnlocked}");

            Debug.Log("NPC Rewards:");
            foreach (var data in npcRewards)
            {
                if (data.isServed)
                {
                    Debug.Log($"  NPC {data.npcIndex}: {data.reward}원 (품질: {data.quality:F1}, 일치: {data.orderMatched})");
                }
            }
        }
#endif
    }

    // ==========================================
    // Data Classes
    // ==========================================

    /// <summary>
    /// NPC별 보상 데이터
    /// </summary>
    [System.Serializable]
    public class NPCRewardData
    {
        public int npcIndex;        // NPC 번호 (1-5)
        public int reward;          // 획득한 재화
        public float quality;       // 품질 점수
        public bool orderMatched;   // 주문 일치 여부
        public bool isServed;       // 제공 완료 여부
    }
}
