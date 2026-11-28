#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu "Arena de Cartas > Gerar .exe (Windows)": monta o jogo em Build/Windows/ArenaDeCartas.exe
[InitializeOnLoad]
public static class Construir
{
    // O progresso salvo (PlayerPrefs) fica guardado com o nome da empresa + produto.
    // Mantém os nomes originais do projeto para não "perder" o save.
    static Construir()
    {
        if (PlayerSettings.companyName != "DefaultCompany") PlayerSettings.companyName = "DefaultCompany";
        if (PlayerSettings.productName != "ArenaDeCartas") PlayerSettings.productName = "ArenaDeCartas";

        // Celular (Android e iPhone): identificador do app, tela em pé e código nativo de 64 bits
        const string id = "com.kaiokelven.arenadecartas";
        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != id)
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, id);
        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) != id)
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, id);
        if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    }

    [MenuItem("Arena de Cartas/Gerar .exe (Windows)")]
    public static void GerarWindows()
    {
        string pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Windows"));
        Directory.CreateDirectory(pasta);
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 405;
        PlayerSettings.defaultScreenHeight = 720;   // cabe em telas de 768 px de altura
        PlayerSettings.resizableWindow = true;

        // Ícone do jogo (emblema 3D original)
        var icone = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Icones/icone.png");
        if (icone != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icone }, IconKind.Any);

        var opcoes = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = Path.Combine(pasta, "ArenaDeCartas.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport rel = BuildPipeline.BuildPlayer(opcoes);
        if (rel.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Jogo gerado em {pasta} ({rel.summary.totalSize / (1024 * 1024)} MB)");
            EditorUtility.RevealInFinder(Path.Combine(pasta, "ArenaDeCartas.exe"));
        }
        else Debug.LogError("Falha ao gerar o jogo: " + rel.summary.result);
    }

    // Menu "Arena de Cartas > Gerar .apk (Android)": monta o jogo para celular em Build/Android/ArenaDeCartas.apk
    [MenuItem("Arena de Cartas/Gerar .apk (Android)")]
    public static void GerarAndroid()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            EditorUtility.DisplayDialog("Falta o módulo Android",
                "Para gerar o .apk, instale o módulo \"Android Build Support\" (com OpenJDK e Android SDK & NDK) " +
                "no Unity Hub: Installs > engrenagem da versão 6000.3 > Add modules.", "OK");
            return;
        }
        string pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Android"));
        Directory.CreateDirectory(pasta);
        var icone = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Icones/icone.png");
        if (icone != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icone }, IconKind.Any);

        var opcoes = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = Path.Combine(pasta, "ArenaDeCartas.apk"),
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        BuildReport rel = BuildPipeline.BuildPlayer(opcoes);
        if (rel.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"APK gerado em {pasta} ({rel.summary.totalSize / (1024 * 1024)} MB)");
            EditorUtility.RevealInFinder(Path.Combine(pasta, "ArenaDeCartas.apk"));
        }
        else Debug.LogError("Falha ao gerar o .apk: " + rel.summary.result);
    }
}
#endif
