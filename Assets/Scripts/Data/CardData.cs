using UnityEngine;

namespace Underdark
{
    public enum CardType
    {
        GiveTurret,       // 특정 타워 N개 지급
        GiveRandomWalls,  // 랜덤 벽 3종 각 1개 지급
        BuffDamage,
        BuffFireRate,
        BuffRange,
    }

    [CreateAssetMenu(fileName = "CardData", menuName = "Underdark/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Card Info")]
        public string cardName = "New Card";
        [TextArea] public string description = "";
        public Color cardColor = new Color(0.3f, 0.6f, 1f);

        [Header("Card Type")]
        public CardType cardType = CardType.GiveTurret;

        [Header("GiveTurret Settings")]
        public TurretType turretType = TurretType.RangedTurret;
        [Tooltip("How many of this turret to add to inventory")]
        public int turretCount = 1;

        [Header("Buff Settings")]
        public TurretType buffTargetType = TurretType.RangedTurret;
        [Tooltip("Buff all turret types")]
        public bool buffAllTypes = false;
        [Tooltip("Multiplier e.g. 1.2 = +20%")]
        public float buffMultiplier = 1.2f;
    }
}
