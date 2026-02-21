using System.Collections.Generic;
using UnityEngine;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// 튀김 색상 (구 시스템 호환용)
    /// </summary>
    public enum FryingColor
    {
        Raw,
        Yellow,
        Golden,
        Brown,
        Burnt
    }

    /// <summary>
    /// 속재료 타입 (구 시스템 호환용)
    /// </summary>
    public enum FillingType
    {
        Sausage,  // 소시지 (HalfSausage + HalfSausage)
        Cheese,   // 치즈 (HalfCheese + HalfCheese)
        Mixed     // 반반 (HalfSausage + HalfCheese)
    }

    /// <summary>
    /// 소스 타입 (구 시스템 호환용)
    /// </summary>
    public enum SauceType
    {
        Ketchup,
        Mustard
    }

    /// <summary>
    /// 소스 양 (구 시스템 호환용)
    /// </summary>
    public enum SauceAmount
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// 소스 요구사항 (구 시스템 호환용)
    /// </summary>
    public class SauceRequirement
    {
        public SauceType type;
        public SauceAmount amount;

        public SauceRequirement(SauceType sauceType, SauceAmount sauceAmount)
        {
            type = sauceType;
            amount = sauceAmount;
        }
    }

    /// <summary>
    /// 핫도그 레시피 (구 시스템 이벤트 호환용)
    /// SimpleCookingManager의 HotdogData를 기존 이벤트 시스템과 호환되도록 변환한 클래스
    /// </summary>
    public class HotdogRecipe
    {
        // 기본 데이터
        public bool hasStick;
        public FillingType fillingType;
        public float batterAmount;
        public float fryingTime;
        public FryingColor fryingColor;
        public bool hasSugar;

        // 소스 데이터
        public List<SauceType> sauces;
        public Dictionary<SauceType, float> sauceAmounts;

        // 검증 결과
        public float quality;           // 0-100 품질 점수
        public bool matchesOrder;       // 주문 일치 여부

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public HotdogRecipe()
        {
            sauces = new List<SauceType>();
            sauceAmounts = new Dictionary<SauceType, float>();
        }

        /// <summary>
        /// [Deprecated] 보상 계산 - ScoreCalculator를 사용하세요
        /// ScoreCalculator.Calculate()가 공식 점수 계산 시스템입니다.
        /// 이 메서드는 구 시스템 호환용으로만 유지됩니다.
        /// </summary>
        /// <param name="config">사용 안 함 (호환성 유지용)</param>
        /// <returns>보상 코인</returns>
        [System.Obsolete("ScoreCalculator.Calculate()를 사용하세요. 이 메서드는 별도 공식으로 계산하여 ScoreCalculator와 결과가 다릅니다.")]
        public int CalculateReward(RecipeConfigSO config)
        {
            // 기본 보상: 2000원
            int baseReward = 2000;

            // 품질 배수: 0.0 ~ 1.0
            float qualityMultiplier = quality / 100f;

            // 주문 일치 보너스: 일치 시 +50%
            float orderBonus = matchesOrder ? 1.5f : 1.0f;

            // 최종 보상 계산
            int totalReward = (int)(baseReward * qualityMultiplier * orderBonus);

            // 최소 10코인 보장
            return Mathf.Max(totalReward, 10);
        }

        /// <summary>
        /// 디버그용 설명 생성
        /// </summary>
        public string GetDescription()
        {
            string desc = $"[HotdogRecipe]\n";
            desc += $"  Filling: {fillingType}\n";
            desc += $"  Batter: {batterAmount:F1}\n";
            desc += $"  Frying: {fryingColor} ({fryingTime:F1}s)\n";
            desc += $"  Sugar: {(hasSugar ? "O" : "X")}\n";
            desc += $"  Quality: {quality:F1}\n";
            desc += $"  Matches Order: {(matchesOrder ? "YES" : "NO")}\n";

            if (sauces.Count > 0)
            {
                desc += $"  Sauces: ";
                foreach (var sauce in sauces)
                {
                    desc += $"{sauce} ";
                }
                desc += "\n";
            }

            return desc;
        }
    }

    /// <summary>
    /// RecipeConfigSO (구 시스템 호환용 - 실제로는 사용 안 함)
    /// </summary>
    public class RecipeConfigSO : ScriptableObject
    {
        public int baseReward = 100;
        public float qualityMultiplier = 1.0f;
        public float orderMatchBonus = 1.5f;
        public string sfxStepComplete = "StepComplete";
    }
}
