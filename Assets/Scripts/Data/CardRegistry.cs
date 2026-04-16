using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 게임에서 사용할 카드 풀을 중앙 관리하는 ScriptableObject.
    /// Resources/CardRegistry.asset 에 두면 런타임 자동 로드.
    /// CardRegistryEditor 메뉴로 Assets/Data/Cards/*.asset 자동 스캔 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "CardRegistry", menuName = "Underdark/Card Registry")]
    public class CardRegistry : ScriptableObject
    {
        [Tooltip("게임에서 사용할 카드 풀 (순서 무관)")]
        public List<CardData> cards = new List<CardData>();

        public IReadOnlyList<CardData> All => cards;

        public List<CardData> ValidCards()
        {
            var result = new List<CardData>();
            foreach (var c in cards)
                if (c != null) result.Add(c);
            return result;
        }
    }
}
