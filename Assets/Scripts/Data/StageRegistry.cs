using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 로비에 표시할 스테이지 목록을 중앙 관리하는 ScriptableObject.
    /// Resources/StageRegistry.asset 에 두면 런타임에서 자동 로드됨.
    /// StageRegistryEditor 메뉴로 Assets/Data/Stages/*.asset 을 자동 스캔 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "StageRegistry", menuName = "Underdark/Stage Registry")]
    public class StageRegistry : ScriptableObject
    {
        [Tooltip("로비에 순서대로 표시할 스테이지 목록")]
        public List<StageData> stages = new List<StageData>();

        /// <summary>stages 리스트를 읽기 전용으로 반환</summary>
        public IReadOnlyList<StageData> All => stages;

        /// <summary>유효한(null 아닌) 스테이지만 반환</summary>
        public List<StageData> ValidStages()
        {
            var result = new List<StageData>();
            foreach (var s in stages)
                if (s != null) result.Add(s);
            return result;
        }
    }
}
