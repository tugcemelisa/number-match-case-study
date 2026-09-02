#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only convenience: keeps required manager GameObjects and shared
// assets present in the project without manual wiring through the Editor
// UI. Runs idempotently after every script recompile; a no-op once
// everything it checks for already exists.
[InitializeOnLoad]
static class SceneBootstrap
{
    const string BoardMaterialPath = "Assets/Resources/BoardInstanced.mat";

    static SceneBootstrap()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        EnsureBoardMaterial();

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "Game")
            return;

        bool changed = false;
        changed |= EnsureComponent<BoardCameraFitter>("Board Camera Fitter");

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    static bool EnsureComponent<T>(string gameObjectName) where T : Component
    {
        if (Object.FindAnyObjectByType<T>() != null)
            return false;

        var go = new GameObject(gameObjectName);
        go.AddComponent<T>();
        Debug.Log($"SceneBootstrap: added {gameObjectName} ({typeof(T).Name}) to the scene.");
        return true;
    }

    static void EnsureBoardMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(BoardMaterialPath) != null)
            return;

        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("SceneBootstrap: could not find Universal Render Pipeline/Lit shader.");
            return;
        }

        Material material = new Material(shader) { enableInstancing = true };
        AssetDatabase.CreateAsset(material, BoardMaterialPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"SceneBootstrap: created {BoardMaterialPath}.");
    }
}
#endif
