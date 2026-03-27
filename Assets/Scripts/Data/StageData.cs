using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>한 웨이브에서 스폰될 몬스터 그룹 1개</summary>
    [System.Serializable]
    public class MonsterSpawnGroup
    {
        [Tooltip("사용할 MonsterStatData (없으면 기본 Enemy_0)")]
        public MonsterStatData statData;

        [Tooltip("이 그룹에서 스폰될 마릿수")]
        public int count = 5;

        [Tooltip("보스 여부 (true면 MonsterStatData의 boss 배율 적용)")]
        public bool isBoss = false;

        [Tooltip("스폰 간격 (초). 0이면 MonsterManager 기본값 사용")]
        public float spawnInterval = 0f;

        [Tooltip("이 그룹의 몬스터 처치 시 지급되는 XP. 0이면 MonsterStatData.baseReward 사용")]
        public int killXpOverride = 0;
    }

    /// <summary>웨이브 1개의 전체 구성</summary>
    [System.Serializable]
    public class WaveData
    {
        [Tooltip("이 웨이브에 등장할 몬스터 그룹들 (여러 종류 동시 설정 가능)")]
        public List<MonsterSpawnGroup> groups = new List<MonsterSpawnGroup>();

        [Tooltip("이 웨이브 이름 (Inspector 표시용)")]
        public string waveName = "Wave";

        [Tooltip("웨이브 클리어 시 지급되는 XP (0이면 GameManager.xpPerWave 사용)")]
        public int waveCompleteXp = 0;
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
        [Tooltip("게임 시작 시 선택할 수 있는 터렛 풀 (4~5개 선택)")]
        public TurretType[] startTurretPool;
        [Tooltip("시작 시 제공할 터렛 수량 (각 1개씩)")]
        public int startTurretCount = 4;
        [Tooltip("게임 시작 시 카드 선택 횟수")]
        public int initialCardPicks = 2;

        [Header("Map")]
        [Tooltip("사용할 MapData (없으면 기본 맵)")]
        public MapData mapData;

        public int TotalWaves => waves != null ? waves.Count : 0;
    }
}
