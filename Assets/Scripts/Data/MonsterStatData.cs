using UnityEngine;

namespace Underdark
{
    [CreateAssetMenu(fileName = "MonsterStatData", menuName = "Underdark/Monster Stat Data")]
    public class MonsterStatData : ScriptableObject
    {
        [Header("Prefab Name (Resources/prefabs/)")]
        public string prefabName = "Enemy_0";

        [Header("Base Stats")]
        public float baseHp     = 10f;
        public float baseSpeed  = 1.8f;
        public int   baseReward = 5;

        [Header("Per Wave Scaling")]
        [Tooltip("HP += hpPerWave * waveIndex")]
        public float hpPerWave     = 20f;
        [Tooltip("Speed += speedPerWave * waveIndex")]
        public float speedPerWave  = 0.12f;
        [Tooltip("Reward += rewardPerWave * waveIndex")]
        public int   rewardPerWave = 1;

        [Header("Boss Override")]
        public bool  isBoss          = false;
        public float bossHpMult      = 1f;
        public float bossSpeedMult   = 1f;
        public float bossSizeScale   = 1f;

        [Header("Spawn Count")]
        [Tooltip("웨이브당 기본 스폰 수")]
        public int   baseCount       = 8;
        [Tooltip("waveIndex당 추가 스폰 수")]
        public int   countPerWave    = 2;
        [Tooltip("최대 스폰 수 제한 (0 = 무제한)")]
        public int   maxCount        = 30;

        public float GetHp(int waveIndex)    => baseHp    + hpPerWave    * waveIndex;
        public float GetSpeed(int waveIndex) => baseSpeed + speedPerWave * waveIndex;
        public int   GetReward(int waveIndex)=> baseReward + rewardPerWave * waveIndex;
        public int   GetCount(int waveIndex)
        {
            int c = baseCount + countPerWave * waveIndex;
            return maxCount > 0 ? Mathf.Min(c, maxCount) : c;
        }
    }
}
