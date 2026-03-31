using UnityEditor;
using UnityEngine;

public class AutoSpriteImporter : AssetPostprocessor
{
    // 옵션: 특정 폴더만 적용하고 싶으면 아래 경로를 수정
    // 예: "Assets/Sprites/"
    private static readonly string[] TargetFolders = { "Assets/__Resources" };

    private static bool IsInTargetFolder(string assetPath)
    {
        foreach (var folder in TargetFolders)
        {
            if (assetPath.StartsWith(folder, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void OnPreprocessTexture()
    {
        // PNG만
        if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            return;

        // 폴더 필터 (원하면 위 TargetFolders를 바꾸면 됨)
        if (!IsInTargetFolder(assetPath))
            return;

        var importer = (TextureImporter)assetImporter;

        // 이미 스프라이트면 굳이 안 건드려도 되지만,
        // Single 강제하려면 그대로 세팅
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        // (선택) 픽셀아트면 끄면 좋음. 필요 없으면 삭제해도 됨.
        // importer.mipmapEnabled = false;

        // (선택) 압축 기본값. 원치 않으면 삭제.
        // importer.textureCompression = TextureImporterCompression.Compressed;

        // (선택) 알파는 기본적으로 유지되지만 명시하고 싶으면:
        // importer.alphaIsTransparency = true;
    }
}