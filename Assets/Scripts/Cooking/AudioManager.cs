using UnityEngine;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// 오디오 관리자
    /// 모든 효과음과 BGM 재생 담당
    /// 어떤 씬에서든 자동 생성됨 (RuntimeInitializeOnLoadMethod)
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("=== Sound Settings (ScriptableObject) ===")]
        [Tooltip("SoundSettings ScriptableObject 할당")]
        public SoundSettings soundSettings;

        [Header("=== Audio Sources ===")]
        [Tooltip("BGM 전용 AudioSource")]
        public AudioSource bgmSource;

        [Tooltip("일반 SFX AudioSource")]
        public AudioSource sfxSource;

        [Tooltip("루프 SFX AudioSource (반죽, 튀김, 소스)")]
        public AudioSource loopSfxSource;

        /// <summary>
        /// 어떤 씬에서든 AudioManager 자동 생성
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoInit()
        {
            if (Instance == null)
            {
                var go = new GameObject("[AudioManager]");
                DontDestroyOnLoad(go);
                go.AddComponent<AudioManager>();
            }
        }

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
                return;
            }

            // soundSettings 자동 로드 (Inspector 미할당 시)
            if (soundSettings == null)
                soundSettings = Resources.Load<SoundSettings>("SoundSettings");

            // AudioSource 자동 생성 (없으면)
            InitializeAudioSources();
        }

        private void Start()
        {
            // 볼륨 초기화
            ApplyVolumeSettings();

            // Day 변경 이벤트 구독
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayChanged += OnDayChanged;
                
                // 시작 시 현재 Day BGM 재생
                PlayBGM(GameManager.Instance.CurrentDay);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayChanged -= OnDayChanged;
            }
        }

        /// <summary>
        /// AudioSource 초기화
        /// </summary>
        private void InitializeAudioSources()
        {
            // BGM Source
            if (bgmSource == null)
            {
                GameObject bgmObj = new GameObject("BGM_Source");
                bgmObj.transform.SetParent(transform);
                bgmSource = bgmObj.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            // SFX Source
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX_Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            // Loop SFX Source
            if (loopSfxSource == null)
            {
                GameObject loopObj = new GameObject("LoopSFX_Source");
                loopObj.transform.SetParent(transform);
                loopSfxSource = loopObj.AddComponent<AudioSource>();
                loopSfxSource.loop = true;
                loopSfxSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// 볼륨 설정 적용
        /// </summary>
        public void ApplyVolumeSettings()
        {
            if (soundSettings == null) return;

            AudioListener.volume = soundSettings.masterVolume;

            if (bgmSource != null)
                bgmSource.volume = soundSettings.bgmVolume;

            if (sfxSource != null)
                sfxSource.volume = soundSettings.sfxVolume;

            if (loopSfxSource != null)
                loopSfxSource.volume = soundSettings.loopSfxVolume;
        }

        #region BGM

        /// <summary>
        /// Day 변경 이벤트 콜백
        /// </summary>
        private void OnDayChanged(int day)
        {
            PlayBGM(day);
        }

        /// <summary>
        /// BGM 재생
        /// </summary>
        public void PlayBGM(int day)
        {
            if (soundSettings == null || bgmSource == null) return;

            AudioClip bgm = soundSettings.GetBGMForDay(day);
            if (bgm != null)
            {
                bgmSource.clip = bgm;
                bgmSource.volume = soundSettings.bgmVolume;
                bgmSource.Play();
                Debug.Log($"[AudioManager] BGM 재생: Day {day}");
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Day {day} BGM이 할당되지 않았습니다!");
            }
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBGM()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
                Debug.Log("[AudioManager] BGM 정지");
            }
        }

        /// <summary>
        /// BGM 일시정지
        /// </summary>
        public void PauseBGM()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }
        }

        /// <summary>
        /// BGM 재개
        /// </summary>
        public void ResumeBGM()
        {
            if (bgmSource != null && !bgmSource.isPlaying)
            {
                bgmSource.UnPause();
            }
        }

        #endregion

        #region SFX - OneShot

        /// <summary>
        /// 스틱 뽑는 소리
        /// </summary>
        public void PlayStickPickup()
        {
            PlaySFX(soundSettings?.stickPickupSound, "스틱 뽑기");
        }

        /// <summary>
        /// 재료 끼우는 소리
        /// </summary>
        public void PlayIngredientAttach()
        {
            PlaySFX(soundSettings?.ingredientAttachSound, "재료 끼우기");
        }

        /// <summary>
        /// 설탕 묻히는 소리
        /// </summary>
        public void PlaySugarApply()
        {
            PlaySFX(soundSettings?.sugarApplySound, "설탕 묻히기");
        }

        /// <summary>
        /// 서빙 완료 소리
        /// </summary>
        public void PlayServing()
        {
            PlaySFX(soundSettings?.servingSound, "서빙");
        }

        /// <summary>
        /// 코인 획득 소리
        /// </summary>
        public void PlayCoinReward()
        {
            var clip = soundSettings?.coinRewardSound;

            // Inspector 미할당 시 Resources에서 자동 로드
            if (clip == null)
                clip = Resources.Load<AudioClip>("Audio/Coinsound");

            PlaySFX(clip, "코인 획득");
        }

        /// <summary>
        /// 버튼 클릭 소리
        /// </summary>
        public void PlayButtonClick()
        {
            if (soundSettings?.buttonClickSound == null || sfxSource == null) return;
            sfxSource.PlayOneShot(soundSettings.buttonClickSound, (soundSettings?.sfxVolume ?? 1f) * 0.4f);
        }

        /// <summary>
        /// 일반 SFX 재생 (내부용)
        /// </summary>
        private void PlaySFX(AudioClip clip, string soundName)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] {soundName} 사운드가 할당되지 않았습니다!");
                return;
            }

            if (sfxSource == null) return;

            sfxSource.PlayOneShot(clip, soundSettings?.sfxVolume ?? 1f);
            Debug.Log($"[AudioManager] SFX 재생: {soundName}");
        }

        #endregion

        #region SFX - Loop

        /// <summary>
        /// 반죽 소리 재생 (Loop)
        /// </summary>
        public void PlayBatteringLoop()
        {
            PlayLoopSFX(soundSettings?.batteringSound, "반죽");
        }

        /// <summary>
        /// 반죽 소리 정지
        /// </summary>
        public void StopBatteringLoop()
        {
            StopLoopSFX("반죽");
        }

        /// <summary>
        /// 튀김 소리 재생 (Loop)
        /// </summary>
        public void PlayFryingLoop()
        {
            PlayLoopSFX(soundSettings?.fryingSound, "튀김");
        }

        /// <summary>
        /// 튀김 소리 정지
        /// </summary>
        public void StopFryingLoop()
        {
            StopLoopSFX("튀김");
        }

        /// <summary>
        /// 소스 짜는 소리 재생 (Loop)
        /// </summary>
        public void PlaySauceLoop()
        {
            PlayLoopSFX(soundSettings?.sauceSqueezeSound, "소스");
        }

        /// <summary>
        /// 소스 짜는 소리 정지
        /// </summary>
        public void StopSauceLoop()
        {
            StopLoopSFX("소스");
        }

        /// <summary>
        /// 루프 SFX 재생 (내부용)
        /// </summary>
        private void PlayLoopSFX(AudioClip clip, string soundName)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] {soundName} 루프 사운드가 할당되지 않았습니다!");
                return;
            }

            if (loopSfxSource == null) return;

            // 이미 같은 클립 재생 중이면 무시
            if (loopSfxSource.isPlaying && loopSfxSource.clip == clip)
            {
                return;
            }

            loopSfxSource.clip = clip;
            loopSfxSource.volume = soundSettings?.loopSfxVolume ?? 0.7f;
            loopSfxSource.Play();
            Debug.Log($"[AudioManager] 루프 SFX 시작: {soundName}");
        }

        /// <summary>
        /// 루프 SFX 정지 (내부용)
        /// </summary>
        private void StopLoopSFX(string soundName)
        {
            if (loopSfxSource != null && loopSfxSource.isPlaying)
            {
                loopSfxSource.Stop();
                loopSfxSource.clip = null;
                Debug.Log($"[AudioManager] 루프 SFX 정지: {soundName}");
            }
        }

        /// <summary>
        /// 모든 루프 SFX 정지
        /// </summary>
        public void StopAllLoopSFX()
        {
            if (loopSfxSource != null)
            {
                loopSfxSource.Stop();
                loopSfxSource.clip = null;
                Debug.Log("[AudioManager] 모든 루프 SFX 정지");
            }
        }

        #endregion

        #region Volume Control

        /// <summary>
        /// 전체 음향(마스터) 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            if (soundSettings != null)
                soundSettings.masterVolume = Mathf.Clamp01(volume);

            AudioListener.volume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// BGM 볼륨 설정
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            if (soundSettings != null)
                soundSettings.bgmVolume = Mathf.Clamp01(volume);

            if (bgmSource != null)
                bgmSource.volume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// SFX 볼륨 설정
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            if (soundSettings != null)
                soundSettings.sfxVolume = Mathf.Clamp01(volume);

            if (sfxSource != null)
                sfxSource.volume = Mathf.Clamp01(volume);

            if (loopSfxSource != null)
                loopSfxSource.volume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 전체 음소거
        /// </summary>
        public void MuteAll(bool mute)
        {
            if (bgmSource != null) bgmSource.mute = mute;
            if (sfxSource != null) sfxSource.mute = mute;
            if (loopSfxSource != null) loopSfxSource.mute = mute;
        }

        #endregion
    }
}
