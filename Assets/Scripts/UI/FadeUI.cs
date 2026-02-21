using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace RecipeAboutLife.UI
{
    /// <summary>
    /// 페이드 인/아웃 효과 UI
    /// 화면 전환 시 검은 화면 페이드 효과 및 텍스트 표시
    /// Singleton 패턴으로 전역 접근 가능
    /// </summary>
    public class FadeUI : MonoBehaviour
    {
        // ==========================================
        // Singleton
        // ==========================================

        private static FadeUI _instance;
        public static FadeUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FadeUI>();
                }
                return _instance;
            }
        }

        // ==========================================
        // UI References
        // ==========================================

        [Header("UI 참조")]
        [SerializeField]
        [Tooltip("페이드 패널 (검은색 전체 화면)")]
        private GameObject fadePanel;

        [SerializeField]
        [Tooltip("페이드 이미지 (검은색 Image)")]
        private Image fadeImage;

        [SerializeField]
        [Tooltip("중앙 텍스트 (예: '잠시만요')")]
        private TextMeshProUGUI centerText;

        [SerializeField]
        [Tooltip("중앙 이미지 (편지 등)")]
        private Image centerImage;

        // ==========================================
        // State
        // ==========================================

        private Coroutine currentFadeCoroutine;
        private CanvasGroup fadePanelCanvasGroup;

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

            // 시작 시 완전히 투명하게 설정
            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = 0f;
                fadeImage.color = color;
            }

            // 텍스트 숨김
            if (centerText != null)
            {
                centerText.gameObject.SetActive(false);
            }

            // 이미지 숨김
            if (centerImage != null)
            {
                centerImage.gameObject.SetActive(false);
            }

            // FadeUI Canvas가 항상 최상위에 표시되도록 sortingOrder 설정
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 999;
                Debug.Log($"[FadeUI] Canvas sortingOrder를 {canvas.sortingOrder}로 설정");
            }

            // 패널은 활성화 상태로 유지 (알파값으로 제어)
            if (fadePanel != null)
            {
                fadePanel.SetActive(true);
                fadePanel.transform.SetAsLastSibling();

                // CanvasGroup으로 raycast 차단 일괄 제어
                fadePanelCanvasGroup = fadePanel.GetComponent<CanvasGroup>();
                if (fadePanelCanvasGroup == null)
                {
                    fadePanelCanvasGroup = fadePanel.AddComponent<CanvasGroup>();
                }
                fadePanelCanvasGroup.blocksRaycasts = false;
            }
        }

        // ==========================================
        // Public API
        // ==========================================

        /// <summary>
        /// 페이드 인 (화면이 어두워짐)
        /// </summary>
        /// <param name="duration">페이드 시간 (초), 0이면 기본값 1초</param>
        /// <param name="onComplete">완료 콜백</param>
        public void FadeIn(float duration = 0f, System.Action onComplete = null)
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            // 페이드 시작 시 클릭 차단 활성화
            if (fadePanelCanvasGroup != null) fadePanelCanvasGroup.blocksRaycasts = true;

            float targetDuration = duration > 0 ? duration : 1f;
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, targetDuration, onComplete));
        }

        /// <summary>
        /// 페이드 아웃 (화면이 밝아짐)
        /// </summary>
        /// <param name="duration">페이드 시간 (초), 0이면 기본값 1초</param>
        /// <param name="onComplete">완료 콜백</param>
        public void FadeOut(float duration = 0f, System.Action onComplete = null)
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            float targetDuration = duration > 0 ? duration : 1f;
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, targetDuration, onComplete));
        }

        /// <summary>
        /// 중앙 텍스트 표시
        /// </summary>
        /// <param name="text">표시할 텍스트</param>
        public void ShowText(string text)
        {
            Debug.Log($"[FadeUI] ShowText 호출: '{text}'");

            if (centerText != null)
            {
                centerText.text = text;
                centerText.gameObject.SetActive(true);
                Debug.Log($"[FadeUI] ✅ 텍스트 표시 완료 - Active: {centerText.gameObject.activeSelf}, Text: '{centerText.text}'");
            }
            else
            {
                Debug.LogError("[FadeUI] ❌ CenterText가 null입니다! Inspector에서 설정하세요.");
            }
        }

        /// <summary>
        /// 중앙 텍스트 숨김
        /// </summary>
        public void HideText()
        {
            Debug.Log("[FadeUI] HideText 호출");

            if (centerText != null)
            {
                centerText.gameObject.SetActive(false);
                Debug.Log("[FadeUI] ✅ 텍스트 숨김 완료");
            }
            else
            {
                Debug.LogError("[FadeUI] ❌ CenterText가 null입니다!");
            }
        }

        /// <summary>
        /// 중앙 이미지 표시 (편지 등)
        /// </summary>
        /// <param name="sprite">표시할 스프라이트</param>
        public void ShowImage(Sprite sprite)
        {
            Debug.Log("[FadeUI] ShowImage 호출");

            if (centerImage != null && sprite != null)
            {
                centerImage.sprite = sprite;
                centerImage.SetNativeSize(); // 원본 크기로 설정
                centerImage.gameObject.SetActive(true);
                Debug.Log("[FadeUI] ✅ 이미지 표시 완료");
            }
            else
            {
                if (centerImage == null)
                    Debug.LogError("[FadeUI] ❌ CenterImage가 null입니다! Inspector에서 설정하세요.");
                if (sprite == null)
                    Debug.LogError("[FadeUI] ❌ Sprite가 null입니다!");
            }
        }

        /// <summary>
        /// 중앙 이미지 숨김
        /// </summary>
        public void HideImage()
        {
            Debug.Log("[FadeUI] HideImage 호출");

            if (centerImage != null)
            {
                centerImage.gameObject.SetActive(false);
                Debug.Log("[FadeUI] ✅ 이미지 숨김 완료");
            }
            else
            {
                Debug.LogError("[FadeUI] ❌ CenterImage가 null입니다!");
            }
        }

        /// <summary>
        /// 중앙 이미지가 표시 중인지 확인
        /// </summary>
        public bool IsImageVisible()
        {
            return centerImage != null && centerImage.gameObject.activeSelf;
        }

        /// <summary>
        /// 즉시 검은 화면으로 설정
        /// </summary>
        public void SetBlack()
        {
            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = 1f;
                fadeImage.color = color;
                if (fadePanelCanvasGroup != null) fadePanelCanvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// 즉시 투명하게 설정
        /// </summary>
        public void SetTransparent()
        {
            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = 0f;
                fadeImage.color = color;
                if (fadePanelCanvasGroup != null) fadePanelCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// FadePanel의 raycast 차단 설정
        /// 결산 UI 등 FadePanel 위에 표시되는 UI의 클릭을 허용하기 위해 사용
        /// </summary>
        public void SetBlocksRaycasts(bool block)
        {
            if (fadePanelCanvasGroup != null)
                fadePanelCanvasGroup.blocksRaycasts = block;
        }

        /// <summary>
        /// 현재 페이드 중인지 확인
        /// </summary>
        public bool IsFading()
        {
            return currentFadeCoroutine != null;
        }

        // ==========================================
        // Private Methods
        // ==========================================

        /// <summary>
        /// 페이드 코루틴
        /// </summary>
        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, System.Action onComplete)
        {
            if (fadeImage == null)
            {
                Debug.LogError("[FadeUI] FadeImage가 설정되지 않았습니다!");
                yield break;
            }

            float elapsedTime = 0f;
            Color color = fadeImage.color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                color.a = alpha;
                fadeImage.color = color;
                yield return null;
            }

            // 최종 알파값 설정
            color.a = endAlpha;
            fadeImage.color = color;

            // 투명해지면 클릭 차단 해제 (페이드 아웃 완료 시)
            if (endAlpha <= 0f)
            {
                if (fadePanelCanvasGroup != null) fadePanelCanvasGroup.blocksRaycasts = false;
            }

            currentFadeCoroutine = null;

            // 완료 콜백 호출
            onComplete?.Invoke();

            Debug.Log($"[FadeUI] 페이드 완료 (Alpha: {endAlpha})");
        }

        // ==========================================
        // Debug
        // ==========================================

#if UNITY_EDITOR
        [ContextMenu("Test: Fade In")]
        private void TestFadeIn()
        {
            FadeIn(1f, () => Debug.Log("Fade In Complete!"));
        }

        [ContextMenu("Test: Fade Out")]
        private void TestFadeOut()
        {
            FadeOut(1f, () => Debug.Log("Fade Out Complete!"));
        }

        [ContextMenu("Test: Show Text")]
        private void TestShowText()
        {
            ShowText("잠시만요");
        }

        [ContextMenu("Test: Hide Text")]
        private void TestHideText()
        {
            HideText();
        }
#endif
    }
}
