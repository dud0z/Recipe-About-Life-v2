using UnityEngine;

namespace RecipeAboutLife.Data
{
    /// <summary>
    /// Day별 스테이지 데이터 (ScriptableObject)
    /// Inspector에서 Day별 배경 스프라이트, 목표 금액 등을 설정
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "RecipeAboutLife/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("Day 번호 (1, 2, 3...)")]
        public int stageNumber;

        [Tooltip("표시 이름 (예: 'Day 1 - 첫 영업일')")]
        public string stageName;

        [Tooltip("목표 금액")]
        public int goalAmount;

        [Tooltip("손님 수")]
        public int customerCount = 5;

        [Header("배경 스프라이트")]
        [Tooltip("메인 배경 스프라이트")]
        public Sprite backgroundSprite;

        [Tooltip("카운터 스프라이트")]
        public Sprite counterSprite;

        [Tooltip("주방 스프라이트")]
        public Sprite kitchenSprite;

        [Header("BGM")]
        [Tooltip("Day별 BGM (선택)")]
        public AudioClip bgmClip;
    }
}
