using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using RecipeAboutLife.Cooking;

/// <summary>
/// 게임 전체 관리자
/// Day, 돈, 일시정지, 결산 등 게임 상태 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== Day Settings ===")]
    [SerializeField] private int currentDay = 1;
    public int maxDay = 3;

    [Header("=== Day Goal Settings ===")]
    public int[] dayGoals = { 6000, 8000, 9000 };  // Day별 목표 금액
    public int customersPerDay = 5;                 // 하루 손님 수

    [Header("=== Money Settings ===")]
    [SerializeField] private int currentMoney = 0;

    [Header("=== Today's Progress ===")]
    [SerializeField] private int todayEarnings = 0;
    [SerializeField] private int customersServed = 0;

    [Header("=== Pause Settings ===")]
    [SerializeField] private bool isPaused = false;

    // 결산 데이터
    private DayResultData currentDayResult = new DayResultData();

    // 현재 주문 (팀원 NPC 시스템에서 설정)
    private OrderData currentOrder;

    // 이벤트
    public event Action<int> OnDayChanged;
    public event Action<int> OnMoneyChanged;
    public event Action<bool> OnPauseChanged;
    public event Action<int, int> OnEarningsChanged;      // (현재수입, 목표)
    public event Action<int, int> OnCustomerServed;       // (서빙수, 전체)
    public event Action<DayResultData> OnDayEnded;        // 하루 종료 시
    public event Action<OrderData> OnOrderChanged;        // 주문 변경 시

    // 프로퍼티
    public int CurrentDay => currentDay;
    public int CurrentMoney => currentMoney;
    public bool IsPaused => isPaused;
    public int TodayEarnings => todayEarnings;
    public int CustomersServed => customersServed;
    public int CustomersPerDay => customersPerDay;
    public OrderData CurrentOrder => currentOrder;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 결산 데이터 초기화
        InitializeDayResult();
    }

    #region Day Management

    /// <summary>
    /// Day 설정 (외부 호출용)
    /// </summary>
    public void SetDay(int day)
    {
        if (day < 1 || day > maxDay)
        {
            Debug.LogWarning($"[GameManager] 유효하지 않은 Day: {day} (1~{maxDay})");
            return;
        }

        currentDay = day;
        InitializeDayResult();
        OnDayChanged?.Invoke(currentDay);
        Debug.Log($"[GameManager] Day 변경: {currentDay}");
    }

    /// <summary>
    /// 다음 Day로 진행
    /// </summary>
    public void NextDay()
    {
        if (currentDay < maxDay)
        {
            currentDay++;
            InitializeDayResult();
            OnDayChanged?.Invoke(currentDay);
            Debug.Log($"[GameManager] 다음 Day: {currentDay}");
        }
        else
        {
            Debug.Log("[GameManager] 마지막 Day입니다! 게임 클리어!");
            // 게임 클리어 로직 추가 가능
        }
    }

    /// <summary>
    /// 현재 Day 목표 금액 반환
    /// StageDataManager에서 먼저 시도, 없으면 기존 배열 방식으로 폴백
    /// </summary>
    public int GetCurrentDayGoal()
    {
        // StageDataManager에서 먼저 시도
        if (RecipeAboutLife.Data.StageDataManager.Instance != null)
        {
            var stageData = RecipeAboutLife.Data.StageDataManager.Instance.GetStageData(currentDay);
            if (stageData != null)
            {
                return stageData.goalAmount;
            }
        }

        // 폴백: 기존 배열 방식
        int index = Mathf.Clamp(currentDay - 1, 0, dayGoals.Length - 1);
        return dayGoals[index];
    }

    /// <summary>
    /// 목표 달성 여부
    /// </summary>
    public bool IsDayGoalAchieved()
    {
        return todayEarnings >= GetCurrentDayGoal();
    }

    /// <summary>
    /// 결산 데이터 초기화
    /// </summary>
    private void InitializeDayResult()
    {
        currentDayResult = new DayResultData
        {
            day = currentDay,
            goalAmount = GetCurrentDayGoal(),
            totalCustomers = customersPerDay
        };
        
        todayEarnings = 0;
        customersServed = 0;
    }

    #endregion

    #region Money Management

    /// <summary>
    /// 돈 추가
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[GameManager] AddMoney에 음수 값 전달됨");
            return;
        }

        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"[GameManager] 돈 추가: +{amount} (현재: {currentMoney})");
    }

    /// <summary>
    /// 돈 사용
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[GameManager] SpendMoney에 음수 값 전달됨");
            return false;
        }

        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            OnMoneyChanged?.Invoke(currentMoney);
            Debug.Log($"[GameManager] 돈 사용: -{amount} (현재: {currentMoney})");
            return true;
        }
        else
        {
            Debug.Log($"[GameManager] 돈 부족! 필요: {amount}, 현재: {currentMoney}");
            return false;
        }
    }

    /// <summary>
    /// 돈 설정 (직접 설정)
    /// </summary>
    public void SetMoney(int amount)
    {
        currentMoney = Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"[GameManager] 돈 설정: {currentMoney}");
    }

    #endregion

    #region Pause Management

    /// <summary>
    /// 일시정지 토글
    /// </summary>
    public void TogglePause()
    {
        SetPause(!isPaused);
    }

    /// <summary>
    /// 일시정지 설정
    /// </summary>
    public void SetPause(bool pause)
    {
        isPaused = pause;
        Time.timeScale = isPaused ? 0f : 1f;
        OnPauseChanged?.Invoke(isPaused);
        Debug.Log($"[GameManager] 일시정지: {isPaused}");
    }

    /// <summary>
    /// 게임 재개 (계속 진행)
    /// </summary>
    public void ResumeGame()
    {
        SetPause(false);
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// 로비 씬으로 이동
    /// </summary>
    public void LoadLobbyScene()
    {
        // 일시정지 해제 후 씬 이동
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("LobbyScene");
    }

    /// <summary>
    /// 설정 열기
    /// </summary>
    public void OpenSetting()
    {
        // 설정 팝업 열기 로직 (추후 구현)
        Debug.Log("[GameManager] 설정 열기");
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] 게임 종료");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("Add 100 Money")]
    private void DebugAddMoney() => AddMoney(100);

    [ContextMenu("Next Day")]
    private void DebugNextDay() => NextDay();

    [ContextMenu("Toggle Pause")]
    private void DebugTogglePause() => TogglePause();

    [ContextMenu("Test Complete Serving (Random)")]
    private void DebugCompleteServing() => CompleteServing(UnityEngine.Random.Range(500, 2001));

    [ContextMenu("Force End Day")]
    private void DebugEndDay() => EndDay();
#endif

    #region Order Management (팀원 NPC 시스템 연동)

    /// <summary>
    /// 현재 주문 설정 (팀원 NPC 시스템에서 호출)
    /// </summary>
    /// <param name="order">주문 데이터</param>
    public void SetCurrentOrder(OrderData order)
    {
        currentOrder = order;
        OnOrderChanged?.Invoke(currentOrder);
        
        if (order != null)
        {
            Debug.Log($"[GameManager] 새 주문 설정: {order}");
        }
        else
        {
            Debug.Log("[GameManager] 주문 초기화됨");
        }
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
    public void ClearCurrentOrder()
    {
        currentOrder = null;
        OnOrderChanged?.Invoke(null);
    }

    #endregion

    #region Serving Management (손님 서빙)

    /// <summary>
    /// 손님 서빙 완료 (요리 제공 시 호출)
    /// </summary>
    /// <param name="earnedMoney">번 금액</param>
    public void CompleteServing(int earnedMoney)
    {
        // 오늘 수입 증가
        todayEarnings += earnedMoney;
        customersServed++;

        // 결산 데이터에 기록
        currentDayResult.earnedAmount = todayEarnings;
        currentDayResult.customersServed = customersServed;
        currentDayResult.earningsPerCustomer.Add(earnedMoney);

        Debug.Log($"[GameManager] 서빙 완료! +{earnedMoney}원 (오늘 총: {todayEarnings}원, 손님: {customersServed}/{customersPerDay})");

        // 이벤트 발생
        OnEarningsChanged?.Invoke(todayEarnings, GetCurrentDayGoal());
        OnCustomerServed?.Invoke(customersServed, customersPerDay);

        // 현재 주문 초기화
        ClearCurrentOrder();

        // 손님 모두 서빙 완료 시 하루 종료
        if (customersServed >= customersPerDay)
        {
            EndDay();
        }
    }

    /// <summary>
    /// 하루 종료
    /// </summary>
    public void EndDay()
    {
        // 결산 데이터 완성
        currentDayResult.day = currentDay;
        currentDayResult.goalAmount = GetCurrentDayGoal();
        currentDayResult.earnedAmount = todayEarnings;
        currentDayResult.customersServed = customersServed;
        currentDayResult.totalCustomers = customersPerDay;
        currentDayResult.isGoalAchieved = todayEarnings >= GetCurrentDayGoal();

        Debug.Log($"[GameManager] === Day {currentDay} 종료 ===");
        Debug.Log($"  수입: {todayEarnings} / 목표: {GetCurrentDayGoal()}");
        Debug.Log($"  결과: {(currentDayResult.isGoalAchieved ? "성공!" : "실패...")}");

        // 이벤트 발생 (팀원의 결산 화면에서 구독)
        OnDayEnded?.Invoke(currentDayResult);
    }

    /// <summary>
    /// 다음 Day 시작 (목표 달성 시 - 결산 화면에서 호출)
    /// </summary>
    public void StartNextDay()
    {
        if (currentDayResult.isGoalAchieved)
        {
            if (currentDay < maxDay)
            {
                NextDay();
                Debug.Log($"[GameManager] Day {currentDay} 시작!");
            }
            else
            {
                Debug.Log("[GameManager] 모든 Day 완료! 게임 클리어!");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] 목표 미달성 상태에서 StartNextDay 호출됨");
        }
    }

    /// <summary>
    /// Day 재시작 (목표 미달성 시 - 결산 화면에서 호출)
    /// </summary>
    public void RestartDay()
    {
        Debug.Log($"[GameManager] Day {currentDay} 재시작! 돈 초기화");

        // 돈 초기화
        currentMoney = 0;
        OnMoneyChanged?.Invoke(currentMoney);

        // 진행 상황 초기화
        InitializeDayResult();

        // 이벤트 발생
        OnDayChanged?.Invoke(currentDay);
        OnEarningsChanged?.Invoke(0, GetCurrentDayGoal());
        OnCustomerServed?.Invoke(0, customersPerDay);
    }

    #endregion

    #region Result Data (결산 데이터)

    /// <summary>
    /// 결산 데이터 반환 (팀원 결산 화면용)
    /// </summary>
    public DayResultData GetDayResultData()
    {
        return currentDayResult;
    }

    /// <summary>
    /// 오늘 진행 상황 요약 문자열
    /// </summary>
    public string GetTodayProgressString()
    {
        return $"{todayEarnings} / {GetCurrentDayGoal()}원 ({customersServed}/{customersPerDay}명)";
    }

    #endregion
}