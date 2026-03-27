using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace Underdark
{
    /// <summary>
    /// 에디터 유틸 - LobbyScene 생성 및 빌드 세팅 등록
    /// </summary>
    public static class SceneSetupTool
    {
        [MenuItem("Underdark/Setup Scenes (Build Settings)")]
        public static void SetupScenes()
        {
            // 올바른 경로에 LobbyScene 없으면 생성
            string lobbyPath = "Assets/Scenes/LobbyScene.unity";
            if (!File.Exists(lobbyPath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, lobbyPath);
                EditorSceneManager.CloseScene(scene, true);
                Debug.Log($"[SceneSetup] LobbyScene 생성: {lobbyPath}");
            }

            // 빌드 세팅에 등록
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/LobbyScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity",  true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[SceneSetup] Build Settings 업데이트 완료! LobbyScene(0), GameScene(1)");
        }
    }
}
