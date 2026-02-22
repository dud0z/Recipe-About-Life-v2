using UnityEngine;
using UnityEngine.UI;
using RecipeAboutLife.Cooking;

namespace RecipeAboutLife.UI
{
    /// <summary>
    /// 설정 팝업 컨트롤러
    /// 전체 음향, 효과음, BGM 볼륨 슬라이더 관리
    /// </summary>
    public class SettingsPopupController : MonoBehaviour
    {
        private static SettingsPopupController _instance;
        public static SettingsPopupController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SettingsPopupController>(FindObjectsInactive.Include);
                }
                return _instance;
            }
        }

        [Header("UI 참조")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeButton;

        [Header("슬라이더")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider bgmSlider;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (masterSlider != null)
                masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            if (bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        }

        /// <summary>
        /// 설정 팝업 표시
        /// </summary>
        public void Show()
        {
            SyncSliders();
            if (settingsPanel != null)
                settingsPanel.SetActive(true);

            Debug.Log("[SettingsPopup] 설정 팝업 표시");
        }

        /// <summary>
        /// 설정 팝업 숨김
        /// </summary>
        public void Hide()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            Debug.Log("[SettingsPopup] 설정 팝업 숨김");
        }

        /// <summary>
        /// 슬라이더 값을 현재 AudioManager 설정과 동기화
        /// </summary>
        private void SyncSliders()
        {
            var settings = AudioManager.Instance?.soundSettings;
            if (settings == null) return;

            if (masterSlider != null) masterSlider.value = settings.masterVolume;
            if (sfxSlider != null) sfxSlider.value = settings.sfxVolume;
            if (bgmSlider != null) bgmSlider.value = settings.bgmVolume;
        }

        private void OnMasterChanged(float val)
        {
            AudioManager.Instance?.SetMasterVolume(val);
        }

        private void OnSFXChanged(float val)
        {
            AudioManager.Instance?.SetSFXVolume(val);
        }

        private void OnBGMChanged(float val)
        {
            AudioManager.Instance?.SetBGMVolume(val);
        }
    }
}
