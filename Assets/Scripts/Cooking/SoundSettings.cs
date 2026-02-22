using UnityEngine;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// 사운드 설정 ScriptableObject
    /// 모든 효과음과 BGM을 Inspector에서 할당
    /// </summary>
    [CreateAssetMenu(fileName = "SoundSettings", menuName = "Recipe About Life/Sound Settings")]
    public class SoundSettings : ScriptableObject
    {
        [Header("=== 요리 SFX ===")]
        [Tooltip("스틱 뽑는 소리")]
        public AudioClip stickPickupSound;

        [Tooltip("재료 끼우는 소리")]
        public AudioClip ingredientAttachSound;

        [Tooltip("반죽 소리 (Loop)")]
        public AudioClip batteringSound;

        [Tooltip("튀김 소리 (Loop)")]
        public AudioClip fryingSound;

        [Tooltip("소스 짜는 소리 (Loop)")]
        public AudioClip sauceSqueezeSound;

        [Tooltip("설탕 묻히는 소리")]
        public AudioClip sugarApplySound;

        [Tooltip("서빙 완료 소리")]
        public AudioClip servingSound;

        [Header("=== UI SFX ===")]
        [Tooltip("버튼 클릭 소리")]
        public AudioClip buttonClickSound;

        [Header("=== BGM ===")]
        [Tooltip("Day 1 배경음악")]
        public AudioClip day1BGM;

        [Tooltip("Day 2 배경음악")]
        public AudioClip day2BGM;

        [Tooltip("Day 3 배경음악")]
        public AudioClip day3BGM;

        [Header("=== Volume Settings ===")]
        [Range(0f, 1f)]
        [Tooltip("전체 음향 볼륨 (마스터)")]
        public float masterVolume = 1f;

        [Range(0f, 1f)]
        [Tooltip("효과음 볼륨")]
        public float sfxVolume = 1f;

        [Range(0f, 1f)]
        [Tooltip("배경음악 볼륨")]
        public float bgmVolume = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("루프 효과음 볼륨 (반죽, 튀김, 소스)")]
        public float loopSfxVolume = 0.7f;

        /// <summary>
        /// Day에 해당하는 BGM 반환
        /// </summary>
        public AudioClip GetBGMForDay(int day)
        {
            return day switch
            {
                1 => day1BGM,
                2 => day2BGM,
                3 => day3BGM,
                _ => day1BGM
            };
        }
    }
}
