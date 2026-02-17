using UnityEngine;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// Day별 배경 스프라이트 변경 컴포넌트
    /// 배경 오브젝트에 부착하여 Day 변경 시 스프라이트 교체
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DayBackground : MonoBehaviour
    {
        [Header("=== Day별 스프라이트 ===")]
        [Tooltip("Day 1 배경 스프라이트")]
        public Sprite day1Sprite;

        [Tooltip("Day 2 배경 스프라이트")]
        public Sprite day2Sprite;

        [Tooltip("Day 3 배경 스프라이트")]
        public Sprite day3Sprite;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            // GameManager 이벤트 구독
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayChanged += OnDayChanged;

                // 현재 Day로 초기화
                OnDayChanged(GameManager.Instance.CurrentDay);
            }
            else
            {
                Debug.LogWarning($"[DayBackground] {gameObject.name}: GameManager를 찾을 수 없습니다!");
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayChanged -= OnDayChanged;
            }
        }

        /// <summary>
        /// Day 변경 시 스프라이트 교체
        /// StageDataManager에서 먼저 시도, 없으면 기존 하드코딩 방식으로 폴백
        /// </summary>
        private void OnDayChanged(int day)
        {
            if (spriteRenderer == null) return;

            Sprite newSprite = null;

            // StageDataManager에서 먼저 시도
            if (Data.StageDataManager.Instance != null)
            {
                var stageData = Data.StageDataManager.Instance.GetStageData(day);
                if (stageData != null)
                {
                    newSprite = stageData.backgroundSprite;
                }
            }

            // 폴백: 기존 하드코딩 방식
            if (newSprite == null)
            {
                newSprite = day switch
                {
                    1 => day1Sprite,
                    2 => day2Sprite,
                    3 => day3Sprite,
                    _ => day1Sprite
                };
            }

            if (newSprite != null)
            {
                spriteRenderer.sprite = newSprite;
                Debug.Log($"[DayBackground] {gameObject.name}: Day {day} 스프라이트로 변경");
            }
            else
            {
                Debug.LogWarning($"[DayBackground] {gameObject.name}: Day {day} 스프라이트가 할당되지 않았습니다!");
            }
        }

        /// <summary>
        /// 수동으로 Day 설정 (테스트용)
        /// </summary>
        public void SetDay(int day)
        {
            OnDayChanged(day);
        }

#if UNITY_EDITOR
        [ContextMenu("Preview Day 1")]
        private void PreviewDay1() => SetDay(1);

        [ContextMenu("Preview Day 2")]
        private void PreviewDay2() => SetDay(2);

        [ContextMenu("Preview Day 3")]
        private void PreviewDay3() => SetDay(3);
#endif
    }
}
