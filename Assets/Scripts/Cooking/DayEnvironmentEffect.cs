using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// Day별 방해/환경 효과
    /// Day 1: 지속적인 약한 블러 (반투명 흰색 오버레이)
    /// Day 2: 12~20초마다 3초간 일시적 블러
    /// Day 3: 화사한 따뜻한 톤 오버레이
    /// GamePlayScene 진입 시 자동 생성됨
    /// </summary>
    public class DayEnvironmentEffect : MonoBehaviour
    {
        public static DayEnvironmentEffect Instance { get; private set; }

        private Image overlayImage;
        public bool isEnabled = true;
        private Coroutine day2Coroutine;

        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "GamePlayScene")
            {
                var go = new GameObject("[DayEnvironmentEffect]");
                go.AddComponent<DayEnvironmentEffect>();
            }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateOverlayUI();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayChanged += OnDayChanged;
                OnDayChanged(GameManager.Instance.CurrentDay);
            }
        }

        /// <summary>
        /// 동적 Canvas + Image 생성
        /// </summary>
        private void CreateOverlayUI()
        {
            // Canvas (SortingOrder 150 — UICanvas(100)보다 위, FadeUI(999)보다 아래)
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 전체화면 Image
            var imageGO = new GameObject("OverlayImage");
            imageGO.transform.SetParent(transform, false);
            overlayImage = imageGO.AddComponent<Image>();
            overlayImage.raycastTarget = false;

            var rt = overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            SetOverlay(Color.clear);
        }

        /// <summary>
        /// Day 변경 시 효과 적용
        /// </summary>
        private void OnDayChanged(int day)
        {
            if (day2Coroutine != null)
            {
                StopCoroutine(day2Coroutine);
                day2Coroutine = null;
            }

            if (!isEnabled)
            {
                SetOverlay(Color.clear);
                return;
            }

            switch (day)
            {
                case 1: ApplyDay1Effect(); break;
                case 2: ApplyDay2Effect(); break;
                case 3: ApplyDay3Effect(); break;
                default: SetOverlay(Color.clear); break;
            }

            Debug.Log($"[DayEnvironmentEffect] Day {day} 환경 효과 적용");
        }

        /// <summary>
        /// Day 1: 지속적인 약한 블러 (반투명 흰색 오버레이)
        /// </summary>
        private void ApplyDay1Effect()
        {
            SetOverlay(new Color(1f, 1f, 1f, 0.12f));
        }

        /// <summary>
        /// Day 2: 12~20초마다 3초간 블러 (주기적 오버레이)
        /// </summary>
        private void ApplyDay2Effect()
        {
            SetOverlay(Color.clear);
            day2Coroutine = StartCoroutine(Day2BlurCoroutine());
        }

        private IEnumerator Day2BlurCoroutine()
        {
            while (true)
            {
                float waitTime = Random.Range(12f, 20f);
                yield return new WaitForSeconds(waitTime);

                if (!isEnabled)
                {
                    SetOverlay(Color.clear);
                    yield break;
                }

                // 페이드 인 (0.5초)
                yield return FadeOverlay(0f, 0.2f, 0.5f, Color.white);

                // 3초 유지
                yield return new WaitForSeconds(3f);

                // 페이드 아웃 (0.5초)
                yield return FadeOverlay(0.2f, 0f, 0.5f, Color.white);
            }
        }

        /// <summary>
        /// Day 3: 화사한 오버레이 (따뜻한 톤)
        /// </summary>
        private void ApplyDay3Effect()
        {
            SetOverlay(new Color(1f, 0.92f, 0.7f, 0.1f));
        }

        private void SetOverlay(Color color)
        {
            if (overlayImage != null)
                overlayImage.color = color;
        }

        private IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration, Color baseColor)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                SetOverlay(new Color(baseColor.r, baseColor.g, baseColor.b, alpha));
                yield return null;
            }
            SetOverlay(new Color(baseColor.r, baseColor.g, baseColor.b, toAlpha));
        }

        /// <summary>
        /// 환경 효과 토글 (디버그용)
        /// </summary>
        public void Toggle()
        {
            isEnabled = !isEnabled;
            Debug.Log($"[DayEnvironmentEffect] 환경 효과 {(isEnabled ? "켜짐" : "꺼짐")}");

            if (isEnabled)
            {
                if (GameManager.Instance != null)
                    OnDayChanged(GameManager.Instance.CurrentDay);
            }
            else
            {
                if (day2Coroutine != null)
                {
                    StopCoroutine(day2Coroutine);
                    day2Coroutine = null;
                }
                SetOverlay(Color.clear);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GameManager.Instance != null)
                GameManager.Instance.OnDayChanged -= OnDayChanged;
        }
    }
}
