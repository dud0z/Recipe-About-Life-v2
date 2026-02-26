using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using RecipeAboutLife.Dialogue;

namespace RecipeAboutLife.UI
{
    /// <summary>
    /// 인트로 스토리 연출 컨트롤러
    /// 메인 타이틀에서 게임 시작 시, 로비 전환 전에 배경 스토리를 보여줌
    /// 배경 이미지 + 독백 텍스트를 페이지별로 순차 재생
    /// </summary>
    public class IntroStoryController : MonoBehaviour
    {
        [Header("인트로 데이터")]
        [SerializeField]
        [Tooltip("인트로 스토리 ScriptableObject")]
        private IntroStoryData introData;

        [Header("UI 참조")]
        [SerializeField]
        [Tooltip("인트로 전체 패널 (비활성 상태로 시작)")]
        private GameObject introPanel;

        [SerializeField]
        [Tooltip("배경 이미지 (전체 화면)")]
        private Image backgroundImage;

        [SerializeField]
        [Tooltip("대사 텍스트 (하단)")]
        private TextMeshProUGUI dialogueText;

        [Header("페이드 효과")]
        [SerializeField]
        [Tooltip("페이드용 검은 오버레이 이미지")]
        private Image fadeOverlay;

        [Header("연출 설정")]
        [SerializeField]
        [Tooltip("대사 표시 시간 (초)")]
        private float lineDisplayTime = 3f;

        [SerializeField]
        [Tooltip("대사 간 간격 (초)")]
        private float linePauseDuration = 0.3f;

        [SerializeField]
        [Tooltip("페이드 효과 시간 (초)")]
        private float fadeDuration = 1f;

        // 입력 처리
        private bool waitingForInput = false;
        private bool inputReceived = false;

        // 완료 콜백
        private System.Action onIntroComplete;

        // 재생 상태
        private bool isPlaying = false;

        private void Update()
        {
            if (!waitingForInput || inputReceived)
                return;

            // 모바일 터치 입력
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                inputReceived = true;
            }
            // PC 마우스 클릭 (테스트용)
            else if (Input.GetMouseButtonDown(0))
            {
                inputReceived = true;
            }
        }

        /// <summary>
        /// 인트로 스토리 재생 시작
        /// </summary>
        /// <param name="onComplete">인트로 완료 시 호출되는 콜백</param>
        public void PlayIntro(System.Action onComplete)
        {
            Debug.Log($"[IntroStoryController] PlayIntro 호출됨. isPlaying={isPlaying}, introData={introData != null}, introPanel={introPanel != null}");

            if (isPlaying)
            {
                Debug.LogWarning("[IntroStoryController] 이미 재생 중입니다.");
                return;
            }

            // 데이터 유효성 확인
            if (introData == null || !introData.HasPages())
            {
                Debug.Log("[IntroStoryController] 인트로 데이터가 없습니다. 스킵합니다.");
                onComplete?.Invoke();
                return;
            }

            // 패널을 먼저 활성화 (비활성 상태에서는 StartCoroutine 실행 불가)
            if (introPanel != null)
                introPanel.SetActive(true);

            // 자신의 gameObject도 활성화 (IntroStoryPanel에 부착된 경우 대비)
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            onIntroComplete = onComplete;
            isPlaying = true;
            StartCoroutine(PlayIntroCoroutine());
        }

        /// <summary>
        /// 인트로 연출 코루틴
        /// </summary>
        private IEnumerator PlayIntroCoroutine()
        {
            Debug.Log("[IntroStoryController] === 인트로 스토리 시작 ===");

            // 1. 페이드 오버레이를 완전 불투명으로 설정
            SetFadeAlpha(1f);

            // 대사 텍스트 초기화 + 한글 폰트 적용
            if (dialogueText != null)
            {
                dialogueText.text = "";
                var koreanFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/인천교육힘찬 SDF");
                if (koreanFont != null)
                    dialogueText.font = koreanFont;
            }

            // 2. 배경 이미지를 첫 번째 페이지에서 한 번만 설정
            if (backgroundImage != null)
            {
                Sprite firstBg = null;
                foreach (var page in introData.pages)
                {
                    if (page.backgroundImage != null)
                    {
                        firstBg = page.backgroundImage;
                        break;
                    }
                }

                if (firstBg != null)
                {
                    backgroundImage.sprite = firstBg;
                    backgroundImage.gameObject.SetActive(true);
                }
                else
                {
                    backgroundImage.gameObject.SetActive(false);
                }
            }

            yield return new WaitForSeconds(0.5f);

            // 3. 페이드 아웃 (화면 밝아짐) — 한 번만
            yield return FadeCoroutine(1f, 0f, fadeDuration);

            // 4. 모든 페이지의 대사를 순차 재생 (배경 전환 없음)
            for (int pageIndex = 0; pageIndex < introData.pages.Count; pageIndex++)
            {
                var page = introData.pages[pageIndex];

                if (page.lines != null)
                {
                    for (int lineIndex = 0; lineIndex < page.lines.Count; lineIndex++)
                    {
                        var line = page.lines[lineIndex];
                        if (line == null || line.IsEmpty())
                            continue;

                        if (dialogueText != null)
                            dialogueText.text = line.text;

                        Debug.Log($"[IntroStoryController] [{line.speaker}] {line.text}");

                        float displayTime = line.displayDuration > 0 ? line.displayDuration : lineDisplayTime;
                        yield return WaitForInputOrTime(displayTime);

                        yield return new WaitForSeconds(linePauseDuration);
                    }
                }
            }

            // 5. 텍스트 지우기
            if (dialogueText != null)
                dialogueText.text = "";

            // 6. 마지막 페이드 인 (검은 화면으로)
            yield return FadeCoroutine(0f, 1f, fadeDuration);

            // 배경 숨기기 + 완료 처리
            if (backgroundImage != null)
                backgroundImage.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.3f);

            Debug.Log("[IntroStoryController] === 인트로 스토리 완료 ===");

            isPlaying = false;

            if (introPanel != null)
                introPanel.SetActive(false);

            onIntroComplete?.Invoke();
            onIntroComplete = null;
        }

        /// <summary>
        /// 터치 입력 또는 시간 대기
        /// </summary>
        private IEnumerator WaitForInputOrTime(float maxTime)
        {
            waitingForInput = true;
            inputReceived = false;

            float elapsedTime = 0f;

            while (elapsedTime < maxTime && !inputReceived)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            waitingForInput = false;
            inputReceived = false;
        }

        /// <summary>
        /// 페이드 효과 코루틴
        /// </summary>
        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
        {
            if (fadeOverlay == null)
                yield break;

            float elapsedTime = 0f;
            Color color = fadeOverlay.color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = endAlpha;
            fadeOverlay.color = color;
        }

        /// <summary>
        /// 페이드 오버레이 알파 설정
        /// </summary>
        private void SetFadeAlpha(float alpha)
        {
            if (fadeOverlay != null)
            {
                Color color = fadeOverlay.color;
                color.a = alpha;
                fadeOverlay.color = color;
            }
        }
    }
}
