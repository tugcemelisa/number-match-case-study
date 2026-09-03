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
        changed |= EnsureComponent<GameHud>("Game Hud");
        changed |= EnsureDragDropController();
        changed |= EnsureRevealEffects();
        changed |= FixRevealBurstPlayOnAwake();
        changed |= NormalizeGridSize();
        changed |= RemoveOrphanedTestObjects();
        changed |= DarkenEnvironment();
        changed |= EnsureGradientBackground();
        changed |= RemoveBoardFrame();
        changed |= EnsureBoardPlatformFrame();
        changed |= EnsureTrayPlatform();
        changed |= EnsureShowcaseSourceImage();

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

    // The burst particle system used to be left at ParticleSystem's default
    // playOnAwake=true, so it fired once, unprompted, the instant Play mode
    // started - looking like a phantom reveal before the player did anything.
    static bool FixRevealBurstPlayOnAwake()
    {
        RevealEffects effects = Object.FindAnyObjectByType<RevealEffects>();
        if (effects == null)
            return false;

        ParticleSystem burst = effects.GetComponentInChildren<ParticleSystem>(true);
        if (burst == null || !burst.main.playOnAwake)
            return false;

        ParticleSystem.MainModule main = burst.main;
        main.playOnAwake = false;
        Debug.Log("SceneBootstrap: fixed Reveal Effects burst particle system firing on scene load (playOnAwake was true).");
        return true;
    }

    static ParticleSystem CreateBurstParticleSystem(Transform parent)
    {
        var go = new GameObject("Burst");
        go.transform.SetParent(parent);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
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

    // Superseded by grid lines baked directly into the board shader (see
    // BoardInstanced.shader's _GridLineColor) - this just clears out the
    // separate frame plane object from earlier in development.
    static bool RemoveBoardFrame()
    {
        GameObject existing = GameObject.Find("Board Frame");
        if (existing == null)
            return false;

        Object.DestroyImmediate(existing);
        Debug.Log("SceneBootstrap: removed the old Board Frame plane object (replaced by shader grid lines).");
        return true;
    }

    static bool EnsureBoardPlatformFrame()
    {
        if (Object.FindAnyObjectByType<BoardPlatformFrame>() != null)
            return false;

        Shader shader = Shader.Find("Custom/BoardFrameBevel");
        if (shader == null)
        {
            Debug.LogError("SceneBootstrap: could not find Custom/BoardFrameBevel shader.");
            return false;
        }

        var material = new Material(shader) { name = "BoardFrameBevel" };

        var root = new GameObject("Board Platform Frame");
        Transform top = CreatePlatformStrip("Top", root.transform, material);
        Transform bottom = CreatePlatformStrip("Bottom", root.transform, material);
        Transform left = CreatePlatformStrip("Left", root.transform, material);
        Transform right = CreatePlatformStrip("Right", root.transform, material);

        BoardPlatformFrame frame = root.AddComponent<BoardPlatformFrame>();
        var serialized = new SerializedObject(frame);
        serialized.FindProperty("top").objectReferenceValue = top;
        serialized.FindProperty("bottom").objectReferenceValue = bottom;
        serialized.FindProperty("left").objectReferenceValue = left;
        serialized.FindProperty("right").objectReferenceValue = right;
        serialized.ApplyModifiedProperties();

        Debug.Log("SceneBootstrap: added Board Platform Frame (fake-3D beveled border) around the board.");
        return true;
    }

    static Transform CreatePlatformStrip(string name, Transform parent, Material material)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Plane);
        strip.name = name;
        strip.transform.SetParent(parent);
        Object.DestroyImmediate(strip.GetComponent<Collider>());
        strip.GetComponent<MeshRenderer>().sharedMaterial = material;
        return strip.transform;
    }

    static bool EnsureTrayPlatform()
    {
        if (Object.FindAnyObjectByType<TrayPlatform>() != null)
            return false;

        Shader shader = Shader.Find("Custom/BoardFrameBevel");
        if (shader == null)
        {
            Debug.LogError("SceneBootstrap: could not find Custom/BoardFrameBevel shader.");
            return false;
        }

        var material = new Material(shader) { name = "TrayPlatformBevel" };

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plate.name = "Tray Platform";
        Object.DestroyImmediate(plate.GetComponent<Collider>());
        plate.GetComponent<MeshRenderer>().sharedMaterial = material;
        plate.AddComponent<TrayPlatform>();

        Debug.Log("SceneBootstrap: added Tray Platform backing plate under the tray pieces.");
        return true;
    }

    const string ShowcaseImagePath = "Assets/Textures/showcase.png";

    // The project shipped with a placeholder "rainbow.png" source image
    // that's literally solid horizontal stripes - one color per row - so
    // every masked cell in an entire row shares the same number, which is
    // exactly why the board reads as a debug matrix instead of a puzzle.
    // The board-generation/color-quantization code is untouched here;
    // swapping in an image with actual spatial variation is enough to
    // make the *same* procedural system produce a mixed, varied layout.
    static bool EnsureShowcaseSourceImage()
    {
        LevelSettings settings = Object.FindAnyObjectByType<LevelSettings>();
        if (settings == null || (settings.SourceImage != null && settings.SourceImage.name == "showcase"))
            return false;

        Texture2D showcase = AssetDatabase.LoadAssetAtPath<Texture2D>(ShowcaseImagePath);
        if (showcase == null)
        {
            Directory.CreateDirectory("Assets/Textures");
            Texture2D generated = GenerateShowcaseTexture(256);
            File.WriteAllBytes(ShowcaseImagePath, generated.EncodeToPNG());
            Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(ShowcaseImagePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(ShowcaseImagePath);
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            showcase = AssetDatabase.LoadAssetAtPath<Texture2D>(ShowcaseImagePath);
        }

        var serialized = new SerializedObject(settings);
        serialized.FindProperty("SourceImage").objectReferenceValue = showcase;
        serialized.ApplyModifiedProperties();

        Debug.Log("SceneBootstrap: switched LevelSettings.SourceImage from the striped rainbow.png placeholder to a generated showcase pattern with varied regions.");
        return true;
    }

    static Texture2D GenerateShowcaseTexture(int size)
    {
        Color[] palette =
        {
            new Color(0.90f, 0.20f, 0.25f),
            new Color(0.95f, 0.45f, 0.15f),
            new Color(0.95f, 0.80f, 0.15f),
            new Color(0.60f, 0.85f, 0.20f),
            new Color(0.20f, 0.75f, 0.35f),
            new Color(0.15f, 0.75f, 0.65f),
            new Color(0.20f, 0.70f, 0.90f),
            new Color(0.25f, 0.45f, 0.90f),
            new Color(0.45f, 0.30f, 0.85f),
            new Color(0.65f, 0.25f, 0.80f),
            new Color(0.90f, 0.25f, 0.70f),
            new Color(0.95f, 0.55f, 0.75f),
        };

        var tex = new Texture2D(size, size, TextureFormat.RGB24, false) { name = "showcase" };
        var pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;
        int ringCount = palette.Length;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
                float angle = Mathf.Atan2(dy, dx);

                // A gentle angle-based wobble on the ring boundaries so the
                // pattern reads as an organic mandala/target shape rather
                // than perfectly mathematical circles.
                float wobble = Mathf.Sin(angle * 5f) * 0.025f;
                int ring = Mathf.Clamp(Mathf.FloorToInt((dist + wobble) * ringCount), 0, ringCount - 1);
                pixels[y * size + x] = palette[ring];
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
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
