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
        EnsureMobileOrientation();

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "Game")
            return;

        bool changed = false;
        changed |= EnsureComponent<BoardCameraFitter>("Board Camera Fitter");
        changed |= NormalizeGridSize();
        changed |= RemoveOrphanedTestObjects();
        changed |= DarkenEnvironment();

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

    static bool NormalizeGridSize()
    {
        LevelSettings settings = Object.FindAnyObjectByType<LevelSettings>();
        if (settings == null || (settings.GridWidth == 16 && settings.GridHeight == 16))
            return false;

        Debug.Log($"SceneBootstrap: LevelSettings grid was {settings.GridWidth}x{settings.GridHeight}, resetting to 16x16.");
        settings.GridWidth = 16;
        settings.GridHeight = 16;
        return true;
    }

    static void EnsureMobileOrientation()
    {
        if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait)
            return;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        Debug.Log("SceneBootstrap: locked default interface orientation to Portrait for mobile.");
    }

    static bool DarkenEnvironment()
    {
        bool changed = false;

        Camera main = Camera.main;
        if (main != null && (main.clearFlags != CameraClearFlags.SolidColor || main.backgroundColor != Color.black))
        {
            main.clearFlags = CameraClearFlags.SolidColor;
            main.backgroundColor = Color.black;
            changed = true;
        }

        GameObject plane = GameObject.Find("Plane");
        if (plane != null)
        {
            MeshRenderer planeRenderer = plane.GetComponent<MeshRenderer>();
            if (planeRenderer != null && planeRenderer.enabled)
            {
                planeRenderer.enabled = false;
                changed = true;
            }
        }

        if (changed)
            Debug.Log("SceneBootstrap: set camera background to black and hid the ground plane's renderer (collider kept for drag raycasts) so masked gray cells read clearly against the environment.");

        return changed;
    }

    static bool RemoveOrphanedTestObjects()
    {
        bool removedAny = false;
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name.StartsWith("~") || HasMissingScript(go))
            {
                Debug.Log($"SceneBootstrap: removing orphaned test object '{go.name}'.");
                Object.DestroyImmediate(go);
                removedAny = true;
            }
        }
        return removedAny;
    }

    static bool HasMissingScript(GameObject go)
    {
        foreach (Component component in go.GetComponents<Component>())
            if (component == null)
                return true;
        return false;
    }

    static void EnsureBoardMaterial()
    {
        Shader shader = Shader.Find("Custom/BoardInstanced");
        if (shader == null)
        {
            Debug.LogError("SceneBootstrap: could not find Custom/BoardInstanced shader.");
            return;
        }

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(BoardMaterialPath);
        if (existing != null)
        {
            if (existing.shader != shader)
            {
                existing.shader = shader;
                existing.enableInstancing = true;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Debug.Log($"SceneBootstrap: updated {BoardMaterialPath} to use {shader.name}.");
            }
            return;
        }

        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();

        Material material = new Material(shader) { enableInstancing = true };
        AssetDatabase.CreateAsset(material, BoardMaterialPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"SceneBootstrap: created {BoardMaterialPath}.");
    }
}
#endif
