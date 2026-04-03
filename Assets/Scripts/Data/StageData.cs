using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 웨이브 내 몬스터 그룹 1개.
    /// 프리팹(외형/행동)과 스탯(HP/Speed)을 분리해서 자유롭게 조합 가능.
    /// 예: 같은 Enemy_0 프리팹을 1웨이브엔 약하게, 5웨이브엔 보스처럼 강하게 사용 가능.
    /// </summary>
    [System.Serializable]
    public class MonsterSpawnGroup
    {
        [Header("프리팹 (외형 / 애니메이션)")]
        [Tooltip("사용할 MonsterStatData - 프리팹 연결용. 없으면 Enemy_0 사용")]
        public MonsterStatData statData;

        [Header("스폰 설정")]
        [Tooltip("이 그룹에서 스폰될 마릿수")]
        public int count = 5;

        [Tooltip("스폰 간격 (초). 0이면 MonsterManager 기본값 사용")]
        public float spawnInterval = 0f;

        [Header("스탯 (웨이브별 직접 입력)")]
        [Tooltip("체력")]
        public float hp     = 30f;
        [Tooltip("이동속도")]
        public float speed  = 1.8f;
        [Tooltip("처치 시 지급 XP")]
        public int   reward = 5;

        [Header("보스 설정")]
        [Tooltip("보스 여부 - true면 bossSizeScale 적용 + 크기 커짐")]
        public bool  isBoss      = false;
        [Tooltip("보스일 때 HP 배율 (기본 1 = hp 그대로 사용)")]
        public float bossHpMult  = 1f;
    }

    /// <summary>웨이브 1개의 전체 구성</summary>
    [System.Serializable]
    public class WaveData
    {
        [Tooltip("이 웨이브 이름 (Inspector 표시용)")]
        public string waveName = "Wave";

        [Tooltip("이 웨이브에 등장할 몬스터 그룹들")]
        public List<MonsterSpawnGroup> groups = new List<MonsterSpawnGroup>();

        [Tooltip("웨이브 클리어 시 지급되는 XP (-1이면 GameManager.xpPerWave 사용, 0이면 XP 없음)")]
        public int waveCompleteXp = -1;
    }

    /// <summary>스테이지 1개의 전체 데이터</summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "Underdark/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Stage Info")]
        public string stageName   = "Stage 1";
        public string description = "";
        [Tooltip("스테이지 썸네일 (선택)")]
        public Sprite thumbnail;

        [Header("Waves")]
        [Tooltip("이 스테이지의 웨이브 목록. 순서대로 진행.")]
        public List<WaveData> waves = new List<WaveData>();

        [Header("Start Setup")]
        [Tooltip("게임 시작 시 선택할 수 있는 터렛 풀")]
        public TurretType[] startTurretPool;
        [Tooltip("시작 시 제공할 터렛 수량")]
        public int startTurretCount = 4;
        [Tooltip("게임 시작 시 카드 선택 횟수")]
        public int initialCardPicks = 2;

        [Header("Map")]
        [Tooltip("사용할 MapData (없으면 기본 맵)")]
        public MapData mapData;

        public int TotalWaves => waves != null ? waves.Count : 0;
    }
}
