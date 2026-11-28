#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Deixa toda a arte do jogo nítida: compressão de alta qualidade (BC7), mipmaps com
// filtro trilinear (sem serrilhado quando a imagem aparece menor) e sem limite de tamanho baixo.
public class ImportacaoNitida : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.mipmapEnabled = true;
        ti.mipMapBias = -0.4f;                 // mipmap um pouco mais nítido
        ti.borderMipmap = false;
        ti.filterMode = FilterMode.Trilinear;
        ti.anisoLevel = 4;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;   // BC7 no PC
        ti.crunchedCompression = false;
    }

    [MenuItem("Arena de Cartas/Reimportar arte (nitidez)")]
    static void Reimportar()
    {
        AssetDatabase.ImportAsset("Assets/Resources", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        Debug.Log("Arte reimportada com as configurações de nitidez.");
    }
}
#endif
