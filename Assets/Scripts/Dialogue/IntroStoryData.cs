using UnityEngine;
using System.Collections.Generic;

namespace RecipeAboutLife.Dialogue
{
    /// <summary>
    /// 인트로 스토리 데이터 (ScriptableObject)
    /// 메인 타이틀 → 로비 전환 전에 표시되는 배경 스토리
    /// 페이지별로 배경 이미지 + 대사(독백) 구성
    /// </summary>
    [CreateAssetMenu(fileName = "IntroStoryData", menuName = "RecipeAboutLife/Dialogue/Intro Story Data", order = 3)]
    public class IntroStoryData : ScriptableObject
    {
        /// <summary>
        /// 인트로 한 페이지 (배경 이미지 + 대사들)
        /// </summary>
        [System.Serializable]
        public class IntroPage
        {
            [Header("배경 이미지")]
            [Tooltip("이 페이지의 배경 이미지 (null이면 검은 화면)")]
            public Sprite backgroundImage;

            [Header("대사")]
            [Tooltip("이 페이지에서 순차적으로 표시할 대사들")]
            public List<DialogueLine> lines = new List<DialogueLine>();
        }

        [Header("인트로 페이지 목록")]
        [Tooltip("순서대로 재생되는 인트로 페이지들")]
        public List<IntroPage> pages = new List<IntroPage>();

        /// <summary>
        /// 유효한 인트로 데이터인지 확인
        /// </summary>
        public bool HasPages()
        {
            return pages != null && pages.Count > 0;
        }
    }
}
