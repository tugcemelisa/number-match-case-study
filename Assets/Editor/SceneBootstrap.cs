#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

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
        // delayCall can fire after Play has already started (e.g. a
        // recompile queued this call right as Play was entered) - scene
        // mutation APIs like MarkSceneDirty/SaveScene throw once that
        // happens, and the resulting exception was derailing PixelPaintGrid's
        // own Init() for that session. Bail out entirely rather than risk
        // a half-applied change while playing.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

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
        changed |= EnsureComponent<TouchCursor>("Touch Cursor");
        changed |= EnsureDragDropController();
        changed |= EnsureRevealEffects();
        changed |= FixRevealBurstPlayOnAwake();
        changed |= NormalizeGridSize();
        changed |= RemoveOrphanedTestObjects();
        changed |= RemoveMissingScripts();
        changed |= DarkenEnvironment();
        changed |= EnsureGradientBackground();
        changed |= RemoveBoardFrame();
        changed |= EnsureBoardPlatformFrame();
        changed |= EnsureTrayPlatform();
        changed |= SyncTrayGap();
        changed |= EnsureShowcaseSourceImage();
        changed |= EnsureFingerCursorSprites();

        // isPlayingOrWillChangePlaymode above closes most of the race, but
        // Play can still start in the gap between that check and this
        // point (the Ensure* calls above aren't instantaneous) - swallow
        // rather than let a mid-transition exception surface, since the
        // in-memory changes above already happened either way and the
        // scene write will simply retry cleanly on the next edit-mode pass.
        if (changed)
        {
            try
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            catch (InvalidOperationException)
            {
                Debug.Log("SceneBootstrap: skipped saving scene changes - Play mode started mid-pass; will retry next edit-mode recompile.");
            }
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

    static bool ForceGridSizeForStressTest()
    {
        LevelSettings settings = Object.FindAnyObjectByType<LevelSettings>();
        if (settings == null || (settings.GridWidth == 64 && settings.GridHeight == 64))
            return false;

        Debug.Log($"SceneBootstrap: LevelSettings grid was {settings.GridWidth}x{settings.GridHeight}, forcing 64x64 for stress test.");
        settings.GridWidth = 64;
        settings.GridHeight = 64;
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

    const float BoardFramePaddingMultiplier = 0.5f;
    const float BoardFrameThickness = 0.45f;

    static bool EnsureBoardPlatformFrame()
    {
        BoardPlatformFrame existingFrame = Object.FindAnyObjectByType<BoardPlatformFrame>();
        if (existingFrame != null)
        {
            // These tuning values only take effect on newly-created
            // components - an instance already serialized into the scene
            // keeps whatever value it was first created with even after
            // the field's own C# default changes, so every tuning pass has
            // to explicitly re-push the current values onto it too.
            return SyncFloatField(existingFrame, "paddingMultiplier", BoardFramePaddingMultiplier)
                | SyncFloatField(existingFrame, "thickness", BoardFrameThickness);
        }

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
        serialized.FindProperty("paddingMultiplier").floatValue = BoardFramePaddingMultiplier;
        serialized.FindProperty("thickness").floatValue = BoardFrameThickness;
        serialized.ApplyModifiedProperties();

        Debug.Log("SceneBootstrap: added Board Platform Frame (fake-3D beveled border) around the board.");
        return true;
    }

    // Pushes a float value onto a private [SerializeField] on an
    // already-existing component if it doesn't already match - the shared
    // mechanism EnsureBoardPlatformFrame/EnsureTrayPlatform use to keep
    // live scene instances in sync with each tuning pass's code values.
    static bool SyncFloatField(Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || Mathf.Approximately(property.floatValue, value))
            return false;

        property.floatValue = value;
        serialized.ApplyModifiedProperties();
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

    static readonly Color TrayWellColor = new Color(0.16f, 0.14f, 0.22f, 1f);
    const float TrayPaddingMultiplier = 0.5f;
    const float TrayFrameThickness = 0.4f;

    static bool EnsureTrayPlatform()
    {
        TrayPlatform existing = Object.FindAnyObjectByType<TrayPlatform>();
        if (existing != null)
        {
            // An older version of this component was a single flat plate
            // with no frame-strip/well children - rebuild it into the
            // current frame+well structure instead of leaving the stale
            // one (and its now-unused MeshRenderer/Collider) in place.
            var existingSerialized = new SerializedObject(existing);
            if (existingSerialized.FindProperty("innerWell").objectReferenceValue != null)
            {
                return SyncFloatField(existing, "paddingMultiplier", TrayPaddingMultiplier)
                    | SyncFloatField(existing, "frameThickness", TrayFrameThickness);
            }

            Object.DestroyImmediate(existing.gameObject);
        }

        Shader frameShader = Shader.Find("Custom/BoardFrameBevel");
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (frameShader == null || litShader == null)
        {
            Debug.LogError("SceneBootstrap: could not find a required shader for the Tray Platform.");
            return false;
        }

        var frameMaterial = new Material(frameShader) { name = "TrayPlatformBevel" };
        var wellMaterial = new Material(litShader) { name = "TrayPlatformWell" };
        wellMaterial.SetColor("_BaseColor", TrayWellColor);
        wellMaterial.SetFloat("_Smoothness", 0.1f);

        var root = new GameObject("Tray Platform");
        Transform top = CreatePlatformStrip("Top", root.transform, frameMaterial);
        Transform bottom = CreatePlatformStrip("Bottom", root.transform, frameMaterial);
        Transform left = CreatePlatformStrip("Left", root.transform, frameMaterial);
        Transform right = CreatePlatformStrip("Right", root.transform, frameMaterial);

        GameObject well = GameObject.CreatePrimitive(PrimitiveType.Plane);
        well.name = "Inner Well";
        well.transform.SetParent(root.transform);
        Object.DestroyImmediate(well.GetComponent<Collider>());
        well.GetComponent<MeshRenderer>().sharedMaterial = wellMaterial;

        TrayPlatform tray = root.AddComponent<TrayPlatform>();
        var serialized = new SerializedObject(tray);
        serialized.FindProperty("top").objectReferenceValue = top;
        serialized.FindProperty("bottom").objectReferenceValue = bottom;
        serialized.FindProperty("left").objectReferenceValue = left;
        serialized.FindProperty("right").objectReferenceValue = right;
        serialized.FindProperty("innerWell").objectReferenceValue = well.transform;
        serialized.FindProperty("paddingMultiplier").floatValue = TrayPaddingMultiplier;
        serialized.FindProperty("frameThickness").floatValue = TrayFrameThickness;
        serialized.ApplyModifiedProperties();

        Debug.Log("SceneBootstrap: added Tray Platform (beveled frame + recessed well) under the tray pieces.");
        return true;
    }

    const float PlatformGapMultiplier = 0.25f;

    static bool SyncTrayGap()
    {
        PixelPaintGrid grid = Object.FindAnyObjectByType<PixelPaintGrid>();
        if (grid == null)
            return false;

        return SyncFloatField(grid, "platformGapMultiplier", PlatformGapMultiplier);
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

    const string FingerIdlePath = "Assets/Textures/94a2ff76-326d-4e06-a6c1-d6af865c7471.png";
    const string FingerPressedPath = "Assets/Textures/10d001dd-1dff-44c8-b9b3-122635241856.png";

    // The two finger-cursor images were dropped straight into the project
    // by hand, so they still have the default "Texture" import type - this
    // gets them into proper Sprite mode and wires them onto the
    // TouchCursor instance's serialized fields, exactly like every other
    // asset this file wires up automatically instead of by hand through
    // the Editor UI.
    static bool EnsureFingerCursorSprites()
    {
        TouchCursor cursor = Object.FindAnyObjectByType<TouchCursor>();
        if (cursor == null)
            return false;

        var serialized = new SerializedObject(cursor);
        SerializedProperty idleProp = serialized.FindProperty("idleSprite");
        SerializedProperty pressedProp = serialized.FindProperty("pressedSprite");
        if (idleProp.objectReferenceValue != null && pressedProp.objectReferenceValue != null)
            return false;

        RemoveWhiteBackground(FingerIdlePath);
        Sprite idleSprite = EnsureSpriteImportSettings(FingerIdlePath);
        Sprite pressedSprite = EnsureSpriteImportSettings(FingerPressedPath);
        if (idleSprite == null || pressedSprite == null)
            return false;

        idleProp.objectReferenceValue = idleSprite;
        pressedProp.objectReferenceValue = pressedSprite;
        serialized.ApplyModifiedProperties();

        Debug.Log("SceneBootstrap: wired the provided finger-cursor images onto TouchCursor.");
        return true;
    }

    static Sprite EnsureSpriteImportSettings(string assetPath)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer == null)
            return null;

        if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency || !importer.isReadable)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // The idle finger image was exported on a flat opaque white canvas
    // instead of a transparent one, which would otherwise show as a solid
    // white square floating over the board. Flood-filling from the four
    // edges (rather than a blanket near-white threshold) only clears the
    // actual background - it can't leak past the hand's dark outline
    // stroke into the white highlights/fingernail inside the shape.
    static void RemoveWhiteBackground(string assetPath)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer == null)
            return;

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
            return;

        Color32[] pixels = texture.GetPixels32();
        int width = texture.width, height = texture.height;

        if (pixels[0].a < 250)
            return; // already processed

        var visited = new bool[pixels.Length];
        var queue = new System.Collections.Generic.Queue<int>();

        void TryEnqueue(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;
            int i = y * width + x;
            if (visited[i])
                return;
            Color32 p = pixels[i];
            if (p.r > 235 && p.g > 235 && p.b > 235)
            {
                visited[i] = true;
                queue.Enqueue(i);
            }
        }

        for (int x = 0; x < width; x++)
        {
            TryEnqueue(x, 0);
            TryEnqueue(x, height - 1);
        }
        for (int y = 0; y < height; y++)
        {
            TryEnqueue(0, y);
            TryEnqueue(width - 1, y);
        }

        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            pixels[i].a = 0;
            int x = i % width, y = i / width;
            TryEnqueue(x - 1, y);
            TryEnqueue(x + 1, y);
            TryEnqueue(x, y - 1);
            TryEnqueue(x, y + 1);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        Debug.Log($"SceneBootstrap: removed the flat white background from '{assetPath}'.");
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

    // Strips broken MonoBehaviour references left over on real scene
    // objects (e.g. a since-deleted Editor diagnostic script that used to
    // be attached to one for a one-off test) without destroying the
    // GameObject itself, unlike RemoveOrphanedTestObjects above.
    static bool RemoveMissingScripts()
    {
        bool removedAny = false;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (removed > 0)
                {
                    Debug.Log($"SceneBootstrap: removed {removed} missing script reference(s) from '{t.gameObject.name}'.");
                    removedAny = true;
                }
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
