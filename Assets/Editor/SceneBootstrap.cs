#if UNITY_EDITOR
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

// Editor-only convenience: keeps required manager GameObjects and shared
// assets present in the project without manual wiring through the Editor
// UI. Runs idempotently after every script recompile; a no-op once
// everything it checks for already exists.
[InitializeOnLoad]
static class SceneBootstrap
{
    const string BoardMaterialPath = "Assets/Resources/BoardInstanced.mat";
    const string BackgroundMaterialPath = "Assets/Resources/RadialGradientBackground.mat";

    static SceneBootstrap()
    {
        EditorApplication.delayCall += Run;
    }

    const string NumberFontPath = "Assets/Resources/Bangers SDF.asset";
    const string SourceFontPath = "Assets/Fonts/Bangers-Regular.ttf";

    static void Run()
    {
        EnsureBoardMaterial();
        EnsureNumberFont();
        EnsurePiecePrefabFont();
        EnsureMobileOrientation();

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "Game")
            return;

        bool changed = false;
        changed |= EnsureComponent<BoardCameraFitter>("Board Camera Fitter");
        changed |= EnsureComponent<FpsCounter>("Fps Counter");
        changed |= EnsureDragDropController();
        changed |= EnsureRevealEffects();
        changed |= NormalizeGridSize();
        changed |= RemoveOrphanedTestObjects();
        changed |= DarkenEnvironment();
        changed |= EnsureGradientBackground();

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

    static bool EnsureDragDropController()
    {
        if (Object.FindAnyObjectByType<DragDropController>() != null)
            return false;

        PixelPaintGrid grid = Object.FindAnyObjectByType<PixelPaintGrid>();
        if (grid == null)
            return false;

        var go = new GameObject("Drag Drop Controller");
        DragDropController controller = go.AddComponent<DragDropController>();

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("grid").objectReferenceValue = grid;
        serialized.ApplyModifiedProperties();

        Debug.Log("SceneBootstrap: added Drag Drop Controller to the scene.");
        return true;
    }

    static bool EnsureRevealEffects()
    {
        if (Object.FindAnyObjectByType<RevealEffects>() != null)
            return false;

        PixelPaintGrid grid = Object.FindAnyObjectByType<PixelPaintGrid>();
        if (grid == null)
            return false;

        var go = new GameObject("Reveal Effects");
        RevealEffects effects = go.AddComponent<RevealEffects>();

        ParticleSystem burst = CreateBurstParticleSystem(go.transform);
        CinemachineImpulseSource impulseSource = go.AddComponent<CinemachineImpulseSource>();
        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        var serialized = new SerializedObject(effects);
        serialized.FindProperty("burst").objectReferenceValue = burst;
        serialized.FindProperty("impulseSource").objectReferenceValue = impulseSource;
        serialized.FindProperty("audioSource").objectReferenceValue = audioSource;
        serialized.ApplyModifiedProperties();

        var gridSerialized = new SerializedObject(grid);
        gridSerialized.FindProperty("revealEffects").objectReferenceValue = effects;
        gridSerialized.ApplyModifiedProperties();

        CinemachineCamera vcam = Object.FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null && vcam.GetComponent<CinemachineImpulseListener>() == null)
            vcam.gameObject.AddComponent<CinemachineImpulseListener>();

        Debug.Log("SceneBootstrap: added Reveal Effects (particles + camera shake + audio hook) to the scene.");
        return true;
    }

    static ParticleSystem CreateBurstParticleSystem(Transform parent)
    {
        var go = new GameObject("Burst");
        go.transform.SetParent(parent);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = 0.6f;
        main.startSpeed = 4f;
        main.startSize = 0.18f;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.6f;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        ps.Stop();
        return ps;
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

    static readonly Color OuterBackgroundColor = new Color(0.03f, 0.03f, 0.06f, 1f);
    static readonly Color InnerBackgroundColor = new Color(0.13f, 0.11f, 0.24f, 1f);

    static bool DarkenEnvironment()
    {
        bool changed = false;

        Camera main = Camera.main;
        if (main != null && (main.clearFlags != CameraClearFlags.SolidColor || main.backgroundColor != OuterBackgroundColor))
        {
            main.clearFlags = CameraClearFlags.SolidColor;
            main.backgroundColor = OuterBackgroundColor;
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
            Debug.Log("SceneBootstrap: set camera background to a dark tone and hid the ground plane's renderer (collider kept for drag raycasts) so masked gray cells read clearly against the environment.");

        return changed;
    }

    static bool EnsureGradientBackground()
    {
        if (GameObject.Find("Background") != null)
            return false;

        Shader shader = Shader.Find("Custom/RadialGradientBackground");
        if (shader == null)
        {
            Debug.LogError("SceneBootstrap: could not find Custom/RadialGradientBackground shader.");
            return false;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(BackgroundMaterialPath);
        if (material == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();

            material = new Material(shader);
            material.SetColor("_InnerColor", InnerBackgroundColor);
            material.SetColor("_OuterColor", OuterBackgroundColor);
            material.SetFloat("_Radius", 30f);
            AssetDatabase.CreateAsset(material, BackgroundMaterialPath);
            AssetDatabase.SaveAssets();
        }

        LevelSettings settings = Object.FindAnyObjectByType<LevelSettings>();
        float centerX = settings != null ? (settings.GridWidth - 1) * settings.PieceSize * 0.5f : 7.5f;
        float centerZ = settings != null ? (settings.GridHeight - 1) * settings.PieceSize * 0.5f : 7.5f;
        material.SetVector("_Center", new Vector4(centerX, 0f, centerZ, 0f));

        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Plane);
        background.name = "Background";
        Object.DestroyImmediate(background.GetComponent<Collider>());
        background.transform.SetPositionAndRotation(new Vector3(centerX, -0.3f, centerZ), Quaternion.identity);
        background.transform.localScale = new Vector3(30f, 1f, 30f);
        background.GetComponent<MeshRenderer>().sharedMaterial = material;

        Debug.Log("SceneBootstrap: added radial gradient Background plane to the scene.");
        return true;
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

    static void EnsureNumberFont()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NumberFontPath) != null)
            return;

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"SceneBootstrap: source font not found at {SourceFontPath}, skipping TMP font asset creation.");
            return;
        }

        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);

        if (fontAsset == null)
        {
            Debug.LogError("SceneBootstrap: TMP_FontAsset.CreateFontAsset returned null - check the source font's import settings.");
            return;
        }

        AssetDatabase.CreateAsset(fontAsset, NumberFontPath);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        AssetDatabase.SaveAssets();
        Debug.Log($"SceneBootstrap: created {NumberFontPath} from {SourceFontPath}.");
    }

    static void EnsurePiecePrefabFont()
    {
        TMP_FontAsset numberFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NumberFontPath);
        if (numberFont == null)
            return;

        string[] guids = AssetDatabase.FindAssets("Piece t:Prefab", new[] { "Assets/Prefabs" });
        if (guids.Length == 0)
            return;

        string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        TextMeshPro tmp = prefabRoot.GetComponentInChildren<TextMeshPro>(true);

        if (tmp != null && tmp.font != numberFont)
        {
            tmp.font = numberFont;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"SceneBootstrap: updated {prefabPath} to use {numberFont.name}.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
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
