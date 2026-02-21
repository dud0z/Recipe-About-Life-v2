using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using RecipeAboutLife.Cooking;

/// <summary>
/// 게임 UI 관리자
/// Day 표시, 돈 표시, 일시정지 버튼 등 관리
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("=== Day UI ===")]
    public Image dayImage;
    public Sprite day1Sprite;
    public Sprite day2Sprite;
    public Sprite day3Sprite;

    [Header("=== Background (Day별 배경) ===")]
    [Tooltip("배경 오브젝트의 SpriteRenderer (BG_gamePlay01)")]
    public SpriteRenderer backgroundRenderer;
    [Tooltip("Day 1 배경 스프라이트")]
    public Sprite day1Background;
    [Tooltip("Day 2 배경 스프라이트")]
    public Sprite day2Background;
    [Tooltip("Day 3 배경 스프라이트")]
    public Sprite day3Background;

    [Header("=== Money UI ===")]
    public GameObject moneyPanel;           // MoneyPanel 전체 (표시/숨김 대상)
    public Image moneyBackground;
    public TMP_Text moneyText;
    public string moneyFormat = "{0}";      // 표시 형식 (예: "{0}원")

    [Header("=== Money Panel Display Settings ===")]
    [Tooltip("MoneyPanel 표시 시간 (초)")]
    public float moneyDisplayDuration = 3f;
    [Tooltip("시작 시 MoneyPanel 숨김 여부")]
    public bool hideMoneyPanelOnStart = true;

    [Header("=== Pause Button ===")]
    public Button pauseButton;

    [Header("=== Pause Popup ===")]
    public GameObject pausePopup;
    public Button resumeButton;      // 계속 진행
    public Button settingsButton;    // 설정
    public Button quitButton;        // 종료

    private GameManager gameManager;
    private Coroutine hideMoneyCoroutine;
    private bool isPopupOpen = false;  // 팝업 열림 상태 추적

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("[GameUIManager] GameManager를 찾을 수 없습니다!");
            return;
        }

        // GameManager 이벤트 구독
        gameManager.OnDayChanged += UpdateDayUI;
        gameManager.OnMoneyChanged += UpdateMoneyUI;
        gameManager.OnPauseChanged += UpdatePauseUI;

        // SimpleCookingManager 이벤트 구독 (요리 시작/제공 시 MoneyPanel 표시)
        if (SimpleCookingManager.Instance != null)
        {
            SimpleCookingManager.Instance.OnCookingStarted += OnCookingStarted;
            SimpleCookingManager.Instance.OnHotdogServed += OnHotdogServed;
        }

        // 버튼 이벤트 연결
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseButtonClicked);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeButtonClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);

        // 초기 UI 설정
        InitializeUI();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnDayChanged -= UpdateDayUI;
            gameManager.OnMoneyChanged -= UpdateMoneyUI;
            gameManager.OnPauseChanged -= UpdatePauseUI;
        }

        // SimpleCookingManager 이벤트 해제
        if (SimpleCookingManager.Instance != null)
        {
            SimpleCookingManager.Instance.OnCookingStarted -= OnCookingStarted;
            SimpleCookingManager.Instance.OnHotdogServed -= OnHotdogServed;
        }

        // 버튼 이벤트 해제
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPauseButtonClicked);

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(OnResumeButtonClicked);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitButtonClicked);
    }

    /// <summary>
    /// 초기 UI 설정
    /// </summary>
    private void InitializeUI()
    {
        // 현재 상태로 UI 초기화
        UpdateDayUI(gameManager.CurrentDay);
        UpdateMoneyUI(gameManager.CurrentMoney);
        
        // 일시정지 팝업 숨김
        if (pausePopup != null)
            pausePopup.SetActive(false);

        // MoneyPanel 초기 상태 설정
        if (hideMoneyPanelOnStart && moneyPanel != null)
        {
            moneyPanel.SetActive(false);
        }
    }

    #region UI Update Methods

    /// <summary>
    /// Day UI 업데이트
    /// </summary>
    private void UpdateDayUI(int day)
    {
        // Day 이미지 업데이트
        if (dayImage != null)
        {
            Sprite sprite = day switch
            {
                1 => day1Sprite,
                2 => day2Sprite,
                3 => day3Sprite,
                _ => day1Sprite
            };

            if (sprite != null)
            {
                dayImage.sprite = sprite;
                Debug.Log($"[GameUIManager] Day UI 업데이트: Day {day}");
            }
            else
            {
                Debug.LogWarning($"[GameUIManager] Day {day} 스프라이트가 할당되지 않았습니다!");
            }
        }

        // 배경 스프라이트 업데이트
        UpdateBackgroundForDay(day);
    }

    /// <summary>
    /// Day별 배경 스프라이트 업데이트
    /// (DayBackground.cs를 사용하는 경우 이 기능은 선택적)
    /// </summary>
    private void UpdateBackgroundForDay(int day)
    {
        // backgroundRenderer가 할당되지 않았으면 무시 (DayBackground.cs 사용 시)
        if (backgroundRenderer == null)
        {
            return;
        }

        Sprite bgSprite = day switch
        {
            1 => day1Background,
            2 => day2Background,
            3 => day3Background,
            _ => day1Background
        };

        if (bgSprite != null)
        {
            backgroundRenderer.sprite = bgSprite;
            Debug.Log($"[GameUIManager] 배경 스프라이트 변경: Day {day}");
        }
    }

    /// <summary>
    /// Money UI 업데이트 (텍스트만 업데이트, 표시/숨김은 별도)
    /// </summary>
    private void UpdateMoneyUI(int money)
    {
        if (moneyText == null) return;

        moneyText.text = string.Format(moneyFormat, money);
        Debug.Log($"[GameUIManager] Money UI 업데이트: {money}");
    }

    /// <summary>
    /// Pause UI 업데이트
    /// </summary>
    private void UpdatePauseUI(bool isPaused)
    {
        // 일시정지 팝업 표시/숨김
        if (pausePopup != null)
        {
            pausePopup.SetActive(isPaused);
            Debug.Log($"[GameUIManager] Pause Popup: {(isPaused ? "표시" : "숨김")}");
        }
    }

    #endregion

    #region Money Panel Show/Hide

    /// <summary>
    /// 요리 시작 시 호출 (이벤트)
    /// </summary>
    private void OnCookingStarted()
    {
        Debug.Log("[GameUIManager] 요리 시작 - MoneyPanel 표시");
        ShowMoneyPanel();
    }

    /// <summary>
    /// 핫도그 제공 시 호출 (이벤트)
    /// </summary>
    private void OnHotdogServed(int earnedMoney)
    {
        Debug.Log("[GameUIManager] 핫도그 제공 - MoneyPanel 표시");
        ShowMoneyPanel();
    }

    /// <summary>
    /// MoneyPanel 표시 (일정 시간 후 자동 숨김)
    /// </summary>
    public void ShowMoneyPanel()
    {
        if (moneyPanel == null) return;

        // 팝업이 열려있으면 표시하지 않음
        if (isPopupOpen)
        {
            Debug.Log("[GameUIManager] 팝업이 열려있어 MoneyPanel 표시 안 함");
            return;
        }

        // 기존 숨김 코루틴 중지
        if (hideMoneyCoroutine != null)
        {
            StopCoroutine(hideMoneyCoroutine);
        }

        // 현재 돈 업데이트
        UpdateMoneyUI(gameManager.CurrentMoney);

        // MoneyPanel 표시
        moneyPanel.SetActive(true);
        Debug.Log($"[GameUIManager] MoneyPanel 표시 ({moneyDisplayDuration}초 후 숨김)");

        // 일정 시간 후 숨김
        hideMoneyCoroutine = StartCoroutine(HideMoneyPanelAfterDelay());
    }

    /// <summary>
    /// MoneyPanel 즉시 숨김
    /// </summary>
    public void HideMoneyPanel()
    {
        if (moneyPanel == null) return;

        // 기존 코루틴 중지
        if (hideMoneyCoroutine != null)
        {
            StopCoroutine(hideMoneyCoroutine);
            hideMoneyCoroutine = null;
        }

        moneyPanel.SetActive(false);
        Debug.Log("[GameUIManager] MoneyPanel 숨김");
    }

    /// <summary>
    /// 일정 시간 후 MoneyPanel 숨김 코루틴
    /// </summary>
    private IEnumerator HideMoneyPanelAfterDelay()
    {
        yield return new WaitForSeconds(moneyDisplayDuration);
        
        // 팝업이 열려있으면 숨기지 않음
        if (isPopupOpen)
        {
            hideMoneyCoroutine = null;
            yield break;
        }

        if (moneyPanel != null)
        {
            moneyPanel.SetActive(false);
            Debug.Log("[GameUIManager] MoneyPanel 자동 숨김");
        }

        hideMoneyCoroutine = null;
    }

    /// <summary>
    /// MoneyPanel 표시 (커스텀 시간)
    /// </summary>
    public void ShowMoneyPanel(float duration)
    {
        if (moneyPanel == null) return;

        // 팝업이 열려있으면 표시하지 않음
        if (isPopupOpen)
        {
            Debug.Log("[GameUIManager] 팝업이 열려있어 MoneyPanel 표시 안 함");
            return;
        }

        // 기존 숨김 코루틴 중지
        if (hideMoneyCoroutine != null)
        {
            StopCoroutine(hideMoneyCoroutine);
        }

        // 현재 돈 업데이트
        UpdateMoneyUI(gameManager.CurrentMoney);

        // MoneyPanel 표시
        moneyPanel.SetActive(true);

        // 일정 시간 후 숨김
        hideMoneyCoroutine = StartCoroutine(HideMoneyPanelAfterDelayCustom(duration));
    }

    private IEnumerator HideMoneyPanelAfterDelayCustom(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // 팝업이 열려있으면 숨기지 않음
        if (isPopupOpen)
        {
            hideMoneyCoroutine = null;
            yield break;
        }

        if (moneyPanel != null)
        {
            moneyPanel.SetActive(false);
        }

        hideMoneyCoroutine = null;
    }

    #endregion

    #region Button Callbacks

    /// <summary>
    /// 일시정지 버튼 클릭
    /// </summary>
    private void OnPauseButtonClicked()
    {
        // 버튼 클릭 소리 재생
        AudioManager.Instance?.PlayButtonClick();
        
        Debug.Log("[GameUIManager] 일시정지 버튼 클릭");
        gameManager?.SetPause(true);
    }

    /// <summary>
    /// 계속 진행 버튼 클릭
    /// </summary>
    private void OnResumeButtonClicked()
    {
        // 버튼 클릭 소리 재생
        AudioManager.Instance?.PlayButtonClick();
        
        Debug.Log("[GameUIManager] 계속 진행 버튼 클릭");
        gameManager?.ResumeGame();
    }

    /// <summary>
    /// 설정 버튼 클릭
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        // 버튼 클릭 소리 재생
        AudioManager.Instance?.PlayButtonClick();
        
        Debug.Log("[GameUIManager] 설정 버튼 클릭");
        gameManager?.OpenSetting();
    }

    /// <summary>
    /// 종료 버튼 클릭
    /// </summary>
    private void OnQuitButtonClicked()
    {
        // 버튼 클릭 소리 재생
        AudioManager.Instance?.PlayButtonClick();
        
        Debug.Log("[GameUIManager] 종료 버튼 클릭");
        gameManager?.QuitGame();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 돈 표시 형식 변경
    /// </summary>
    public void SetMoneyFormat(string format)
    {
        moneyFormat = format;
        UpdateMoneyUI(gameManager.CurrentMoney);
    }

    /// <summary>
    /// Day 이미지 수동 설정 (Inspector 미할당 시 사용)
    /// </summary>
    public void SetDaySprites(Sprite d1, Sprite d2, Sprite d3)
    {
        day1Sprite = d1;
        day2Sprite = d2;
        day3Sprite = d3;
        UpdateDayUI(gameManager.CurrentDay);
    }

    /// <summary>
    /// 배경 스프라이트 수동 설정 (Inspector 미할당 시 사용)
    /// </summary>
    public void SetBackgroundSprites(Sprite bg1, Sprite bg2, Sprite bg3)
    {
        day1Background = bg1;
        day2Background = bg2;
        day3Background = bg3;
        UpdateBackgroundForDay(gameManager.CurrentDay);
    }

    /// <summary>
    /// 일시정지 버튼 활성화/비활성화
    /// 팝업이 열려있을 때 비활성화
    /// </summary>
    public void SetPauseButtonInteractable(bool interactable)
    {
        if (pauseButton != null)
        {
            pauseButton.interactable = interactable;
            Debug.Log($"[GameUIManager] 일시정지 버튼 {(interactable ? "활성화" : "비활성화")}");
        }
    }

    /// <summary>
    /// 일시정지 버튼 활성화
    /// </summary>
    public void EnablePauseButton()
    {
        SetPauseButtonInteractable(true);
    }

    /// <summary>
    /// 일시정지 버튼 비활성화
    /// </summary>
    public void DisablePauseButton()
    {
        SetPauseButtonInteractable(false);
    }

    /// <summary>
    /// 메인 UI 숨김 (팝업 열릴 때 호출)
    /// MoneyPanel, PauseButton, DayImage 모두 숨김
    /// </summary>
    public void HideMainUI()
    {
        // 팝업 열림 상태 설정
        isPopupOpen = true;

        // 진행 중인 MoneyPanel 숨김 코루틴 중지
        if (hideMoneyCoroutine != null)
        {
            StopCoroutine(hideMoneyCoroutine);
            hideMoneyCoroutine = null;
        }

        // MoneyPanel 숨김
        if (moneyPanel != null)
        {
            moneyPanel.SetActive(false);
        }

        // 일시정지 버튼 숨김
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }

        // Day 이미지 숨김
        if (dayImage != null)
        {
            dayImage.gameObject.SetActive(false);
        }

        Debug.Log("[GameUIManager] 메인 UI 숨김 (MoneyPanel, PauseButton, DayImage)");
    }

    /// <summary>
    /// 메인 UI 표시 (팝업 닫힐 때 호출)
    /// MoneyPanel, PauseButton, DayImage 모두 표시
    /// </summary>
    public void ShowMainUI()
    {
        // 팝업 닫힘 상태 설정
        isPopupOpen = false;

        // MoneyPanel 표시 (자동 숨김 타이머와 함께)
        if (moneyPanel != null)
        {
            moneyPanel.SetActive(true);
            
            // 자동 숨김 타이머 시작
            if (hideMoneyCoroutine != null)
            {
                StopCoroutine(hideMoneyCoroutine);
            }
            hideMoneyCoroutine = StartCoroutine(HideMoneyPanelAfterDelay());
        }

        // 일시정지 버튼 표시 및 활성화
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
            pauseButton.interactable = true;
        }

        // Day 이미지 표시
        if (dayImage != null)
        {
            dayImage.gameObject.SetActive(true);
        }

        Debug.Log("[GameUIManager] 메인 UI 표시 (MoneyPanel, PauseButton, DayImage)");
    }

    #endregion
}