using System;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    // 보상 종류
    public enum PassRewardType
    {
        Gold,       // 골드
        Gem,        // 보석
        TurretCard, // 터렛 카드
        LootBox,    // 상자
    }

    // 보상 1개
    [Serializable]
    public class PassReward
    {
        [Tooltip("보상 종류")]
        public PassRewardType type = PassRewardType.Gold;

        [Tooltip("수량")]
        public int amount = 100;

        [Tooltip("터렛 카드일 때 특정 카드. 비워두면 랜덤")]
        public CardData specificCard;

        [Tooltip("UI 표시 이름. 비우면 자동 생성")]
        public string displayName;

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(displayName)) return displayName;
            return type switch
            {
                PassRewardType.Gold       => $"골드 {amount}",
                PassRewardType.Gem        => $"보석 {amount}",
                PassRewardType.TurretCard => specificCard != null
                    ? $"{specificCard.cardName} 카드 {amount}장"
                    : $"터렛 카드 {amount}장",
                PassRewardType.LootBox    => $"상자 {amount}개",
                _                        => "보상",
            };
        }
    }

    // 레벨 1줄
    [Serializable]
    public class PassLevelEntry
    {
        [Tooltip("레벨 번호 (1~MAX)")]
        public int level = 1;

        [Tooltip("무료 보상")]
        public PassReward freeReward = new PassReward { type = PassRewardType.Gold, amount = 100 };

        [Tooltip("유료 보상 (패스 구매 후 수령 가능)")]
        public PassReward paidReward = new PassReward { type = PassRewardType.Gem, amount = 50 };
    }

    /// <summary>
    /// 시즌패스 보상 테이블 ScriptableObject.
    /// 위치: Assets/Resources/PassRewardTable.asset
    /// 에디터에서 직접 편집. 레벨 추가·삭제는 entries 배열만 수정.
    /// 메뉴: Underdark > Pass Reward > Create Default Table
    /// </summary>
    [CreateAssetMenu(fileName = "PassRewardTable", menuName = "Underdark/Pass Reward Table")]
    public class PassRewardData : ScriptableObject
    {
        [Tooltip("레벨별 보상 목록. level 오름차순 정렬 권장.")]
        public List<PassLevelEntry> entries = new List<PassLevelEntry>();

        /// <summary>entries 개수 = 최대 레벨</summary>
        public int MaxLevel => entries.Count;

        /// <summary>레벨로 항목 조회 (없으면 null)</summary>
        public PassLevelEntry GetEntry(int level)
        {
            foreach (var e in entries)
                if (e.level == level) return e;
            return null;
        }

        /// <summary>Resources.Load 경로</summary>
        public const string RESOURCE_PATH = "PassRewardTable";
    }
}
