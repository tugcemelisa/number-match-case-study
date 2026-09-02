#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only convenience: keeps required manager GameObjects present in the
// Game scene without manual wiring through the Editor UI. Runs idempotently
// after every script recompile; a no-op once everything it checks for
// already exists.
[InitializeOnLoad]
static class SceneBootstrap
{
    static SceneBootstrap()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
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
}
#endif
