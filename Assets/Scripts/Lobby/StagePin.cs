using UnityEngine;
using System.Collections;

namespace RecipeAboutLife.Lobby
{
    /// <summary>
    /// 스테이지 핀 - 위아래로 움직이고 클릭 가능
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class StagePin : MonoBehaviour
    {
        [Header("스테이지 설정")]
        [SerializeField] private int stageIndex = 1;
        [SerializeField] private bool isUnlocked = true;

        [Header("스프라이트")]
        [SerializeField] private Sprite unlockedSprite;
        [SerializeField] private Sprite lockedSprite;

        [Header("부유 애니메이션")]
        [SerializeField] private float floatHeight = 0.15f;
        [SerializeField] private float floatSpeed = 2f;

        private SpriteRenderer spriteRenderer;
        private Vector3 startPosition;

        public int StageIndex => stageIndex;
        public bool IsUnlocked => isUnlocked;
        public System.Action<StagePin> OnClicked;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            startPosition = transform.position;
        }

        private void Start()
        {
            UpdateSprite();
            StartCoroutine(FloatAnimation());

            // 디버그: 컴포넌트 상태 확인
            var collider = GetComponent<BoxCollider2D>();
            Debug.Log($"[StagePin] Stage {stageIndex} 초기화 - Collider: {(collider != null ? "있음" : "없음")}, Enabled: {(collider != null && collider.enabled)}");
        }

        private void Update()
        {
            // 마우스 클릭 감지 (UI에 막히지 않도록 직접 Raycast)
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    Debug.Log($"[StagePin] ★★★ Stage {stageIndex} 클릭됨! ★★★");
                    OnClicked?.Invoke(this);
                }
            }
        }

        private void OnMouseDown()
        {
            // 백업용 (UI가 없는 경우 작동)
            Debug.Log($"[StagePin] (OnMouseDown) Stage {stageIndex} 클릭됨!");
            OnClicked?.Invoke(this);
        }

        /// <summary>
        /// UI Button의 OnClick에서 호출할 메서드
        /// </summary>
        public void OnButtonClick()
        {
            Debug.Log($"[StagePin] ★★★ Stage {stageIndex} 버튼 클릭! ★★★");
            OnClicked?.Invoke(this);
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null) return;

            if (isUnlocked && unlockedSprite != null)
            {
                spriteRenderer.sprite = unlockedSprite;
                spriteRenderer.color = Color.white;
            }
            else if (!isUnlocked && lockedSprite != null)
            {
                spriteRenderer.sprite = lockedSprite;
                spriteRenderer.color = Color.white;
            }
            else if (!isUnlocked)
            {
                spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        /// <summary>
        /// 페이드 인 (검은색으로 변경)
        /// </summary>
        public void FadeToBlack(float duration)
        {
            if (spriteRenderer == null) return;
            // sortingOrder를 낮춰서 FadeUI 캔버스 아래에 표시되도록 보장
            spriteRenderer.sortingOrder = -100;
            StartCoroutine(FadeColorCoroutine(spriteRenderer.color, Color.black, duration));
        }

        /// <summary>
        /// 페이드 아웃 (원래 색으로 복귀)
        /// </summary>
        public void FadeToNormal(float duration)
        {
            if (spriteRenderer == null) return;
            // sortingOrder를 원래 값으로 복구
            spriteRenderer.sortingOrder = 0;
            Color targetColor = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            StartCoroutine(FadeColorCoroutine(spriteRenderer.color, targetColor, duration));
        }

        private IEnumerator FadeColorCoroutine(Color from, Color to, float duration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                spriteRenderer.color = Color.Lerp(from, to, t);
                yield return null;
            }

            spriteRenderer.color = to;
        }

        private IEnumerator FloatAnimation()
        {
            float time = Random.Range(0f, Mathf.PI * 2f);

            while (true)
            {
                time += Time.deltaTime * floatSpeed;
                float yOffset = Mathf.Sin(time) * floatHeight;
                transform.position = startPosition + new Vector3(0, yOffset, 0);
                yield return null;
            }
        }
    }
}
