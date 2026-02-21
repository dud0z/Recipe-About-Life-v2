using UnityEngine;

namespace RecipeAboutLife.Cooking
{
    /// <summary>
    /// 점수 계산기
    /// 주문과 완성품을 비교하여 점수(돈) 계산
    /// 
    /// 점수 구성 (최대 1800원):
    /// - 재료1 일치: 300원
    /// - 재료2 일치: 300원
    /// - 튀김 상태: Golden 600원 / Yellow,Brown 300원 / Raw,Burnt 0원
    /// - 설탕 일치: 300원
    /// - 케첩 일치: 150원
    /// - 머스타드 일치: 150원
    /// </summary>
    public static class ScoreCalculator
    {
        #region Score Constants

        /// <summary>재료 일치 점수 (x2 = 600)</summary>
        public const int FILLING_MATCH = 300;

        /// <summary>튀김 완벽 (Golden)</summary>
        public const int FRYING_PERFECT = 600;

        /// <summary>튀김 괜찮음 (Yellow/Brown)</summary>
        public const int FRYING_GOOD = 300;

        /// <summary>튀김 실패 (Raw/Burnt)</summary>
        public const int FRYING_BAD = 0;

        /// <summary>설탕 일치</summary>
        public const int SUGAR_MATCH = 300;

        /// <summary>케첩 일치</summary>
        public const int KETCHUP_MATCH = 150;

        /// <summary>머스타드 일치</summary>
        public const int MUSTARD_MATCH = 150;

        /// <summary>최대 점수</summary>
        public const int MAX_SCORE = FILLING_MATCH * 2 + FRYING_PERFECT + SUGAR_MATCH + KETCHUP_MATCH + MUSTARD_MATCH; // 1800

        #endregion

        #region Public Methods

        /// <summary>
        /// 점수 계산 (주문 vs 완성품)
        /// </summary>
        /// <param name="order">손님의 주문</param>
        /// <param name="hotdog">완성된 핫도그</param>
        /// <returns>획득 금액 (0 ~ 2000)</returns>
        public static int Calculate(OrderData order, HotdogData hotdog)
        {
            if (order == null || hotdog == null)
            {
                Debug.LogWarning("[ScoreCalculator] order 또는 hotdog가 null입니다!");
                return 0;
            }

            int score = 0;
            
            // 1. 재료 일치 확인
            int fillingScore = CalculateFillingScore(order, hotdog);
            score += fillingScore;
            
            // 2. 튀김 상태 점수
            int fryingScore = GetFryingScore(hotdog.fryingState);
            score += fryingScore;
            
            // 3. 토핑 일치 점수
            int toppingScore = CalculateToppingScore(order, hotdog);
            score += toppingScore;

            // 로그 출력
            Debug.Log($"[ScoreCalculator] === 점수 계산 결과 ===");
            Debug.Log($"  재료 점수: {fillingScore}원");
            Debug.Log($"  튀김 점수: {fryingScore}원 ({hotdog.fryingState})");
            Debug.Log($"  토핑 점수: {toppingScore}원");
            Debug.Log($"  총 점수: {score}원 / {MAX_SCORE}원");

            return score;
        }

        /// <summary>
        /// 주문 없이 튀김 상태만으로 기본 점수 계산
        /// (주문 시스템 연동 전 테스트용)
        /// </summary>
        public static int CalculateWithoutOrder(HotdogData hotdog)
        {
            if (hotdog == null) return 0;

            int score = 0;

            // 재료 있으면 점수 부여
            if (!string.IsNullOrEmpty(hotdog.filling1)) score += FILLING_MATCH;
            if (!string.IsNullOrEmpty(hotdog.filling2)) score += FILLING_MATCH;

            // 튀김 점수
            score += GetFryingScore(hotdog.fryingState);

            // 토핑 있으면 점수 부여
            if (hotdog.hasSugar) score += SUGAR_MATCH / 2;      // 절반 점수
            if (hotdog.hasKetchup) score += KETCHUP_MATCH / 2;
            if (hotdog.hasMustard) score += MUSTARD_MATCH / 2;

            Debug.Log($"[ScoreCalculator] 주문 없이 계산: {score}원");
            return score;
        }

        /// <summary>
        /// 튀김 상태별 점수 반환
        /// </summary>
        public static int GetFryingScore(FryingState state)
        {
            return state switch
            {
                FryingState.Golden => FRYING_PERFECT,  // 600원
                FryingState.Yellow => FRYING_GOOD,     // 300원
                FryingState.Brown => FRYING_GOOD,      // 300원
                FryingState.Raw => FRYING_BAD,         // 0원
                FryingState.Burnt => FRYING_BAD,       // 0원
                _ => FRYING_BAD
            };
        }

        /// <summary>
        /// 튀김 상태 등급 문자열 반환
        /// </summary>
        public static string GetFryingGrade(FryingState state)
        {
            return state switch
            {
                FryingState.Golden => "완벽",
                FryingState.Yellow => "괜찮음",
                FryingState.Brown => "괜찮음",
                FryingState.Raw => "덜 익음",
                FryingState.Burnt => "탐",
                _ => "알 수 없음"
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 재료 일치 점수 계산
        /// </summary>
        private static int CalculateFillingScore(OrderData order, HotdogData hotdog)
        {
            int score = 0;

            // 재료1 일치
            if (!string.IsNullOrEmpty(order.filling1) && order.filling1 == hotdog.filling1)
            {
                score += FILLING_MATCH;
                Debug.Log($"[ScoreCalculator] 재료1 일치: {order.filling1} ✅");
            }
            else
            {
                Debug.Log($"[ScoreCalculator] 재료1 불일치: 주문={order.filling1}, 실제={hotdog.filling1} ❌");
            }

            // 재료2 일치
            if (!string.IsNullOrEmpty(order.filling2) && order.filling2 == hotdog.filling2)
            {
                score += FILLING_MATCH;
                Debug.Log($"[ScoreCalculator] 재료2 일치: {order.filling2} ✅");
            }
            else
            {
                Debug.Log($"[ScoreCalculator] 재료2 불일치: 주문={order.filling2}, 실제={hotdog.filling2} ❌");
            }

            return score;
        }

        /// <summary>
        /// 토핑 일치 점수 계산
        /// </summary>
        private static int CalculateToppingScore(OrderData order, HotdogData hotdog)
        {
            int score = 0;

            // 설탕 일치
            if (order.wantsSugar == hotdog.hasSugar)
            {
                score += SUGAR_MATCH;
                Debug.Log($"[ScoreCalculator] 설탕 일치: {(order.wantsSugar ? "O" : "X")} ✅");
            }
            else
            {
                Debug.Log($"[ScoreCalculator] 설탕 불일치: 주문={order.wantsSugar}, 실제={hotdog.hasSugar} ❌");
            }

            // 케첩 일치
            if (order.wantsKetchup == hotdog.hasKetchup)
            {
                score += KETCHUP_MATCH;
                Debug.Log($"[ScoreCalculator] 케첩 일치: {(order.wantsKetchup ? "O" : "X")} ✅");
            }
            else
            {
                Debug.Log($"[ScoreCalculator] 케첩 불일치: 주문={order.wantsKetchup}, 실제={hotdog.hasKetchup} ❌");
            }

            // 머스타드 일치
            if (order.wantsMustard == hotdog.hasMustard)
            {
                score += MUSTARD_MATCH;
                Debug.Log($"[ScoreCalculator] 머스타드 일치: {(order.wantsMustard ? "O" : "X")} ✅");
            }
            else
            {
                Debug.Log($"[ScoreCalculator] 머스타드 불일치: 주문={order.wantsMustard}, 실제={hotdog.hasMustard} ❌");
            }

            return score;
        }

        #endregion
    }
}
