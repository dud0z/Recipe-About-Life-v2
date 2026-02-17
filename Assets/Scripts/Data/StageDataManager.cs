using UnityEngine;
using System.Collections.Generic;

namespace RecipeAboutLife.Data
{
    /// <summary>
    /// StageData ScriptableObject를 중앙에서 관리
    /// Inspector에서 stages 리스트에 Day별 StageData SO를 할당
    /// </summary>
    public class StageDataManager : MonoBehaviour
    {
        public static StageDataManager Instance { get; private set; }

        [Header("스테이지 데이터 목록")]
        [Tooltip("Day별 StageData ScriptableObject를 여기에 할당")]
        [SerializeField] private List<StageData> stages = new List<StageData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Day 번호로 StageData 가져오기
        /// </summary>
        /// <param name="dayNumber">Day 번호 (1, 2, 3...)</param>
        /// <returns>해당 Day의 StageData, 없으면 null</returns>
        public StageData GetStageData(int dayNumber)
        {
            return stages.Find(s => s != null && s.stageNumber == dayNumber);
        }

        /// <summary>
        /// 전체 스테이지 수
        /// </summary>
        public int StageCount => stages.Count;
    }
}
