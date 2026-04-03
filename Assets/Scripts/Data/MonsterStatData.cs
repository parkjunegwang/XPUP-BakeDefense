using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 몬스터 프리팹 정보만 관리.
    /// HP/Speed/Reward 등 스탯은 StageData의 MonsterSpawnGroup에서 웨이브별로 직접 설정.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterStatData", menuName = "Underdark/Monster Stat Data")]
    public class MonsterStatData : ScriptableObject
    {
        [Header("프리팹")]
        [Tooltip("Resources/prefabs/ 폴더 안의 프리팹 이름")]
        public string prefabName = "Enemy_0";

        [Header("보스 크기 배율 (isBoss일 때 적용)")]
        [Tooltip("보스로 사용될 때 크기 배율. 1이면 기본 크기.")]
        public float bossSizeScale = 1.8f;
    }
}
