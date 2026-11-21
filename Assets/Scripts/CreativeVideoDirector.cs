using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Choreographs high-retention Creative Video Hook Moments directly inside the real Number Match game:
/// - Hook 1 (0.2s–2.0s): Instant first tile drag -> full group radial wave dissolve + particle burst + camera shake.
/// - Hook 2 (2.0s–4.0s): Intentional wrong number drop -> red cell flash + reject shake & bounce back.
/// - Hook 3 (4.0s–11.5s): Rapid combo streak (x2 -> x3 -> x4 -> x5) triggering 3 consecutive group wave reveals.
/// When launched with -nm-record <outDir> (or recordFrames = true), captures 1080x1920 frames at a locked 30 FPS.
/// </summary>
public class CreativeVideoDirector : MonoBehaviour
{
    public bool autoPlayHooks = false;
    public bool recordFrames = false;
    public string outputDir = "CreativeAd_AfterEffects/RawFrames";
    public int recordFps = 30;
    public int totalFramesToRecord = 420; // 14.0s of deliberate, natural-paced gameplay hook footage

    PixelPaintGrid _grid;
    RevealEffects _effects;
    Camera _cam;
    int _capturedCount;
    bool _running;
    Vector2 _lastFingerScreen;
    readonly HashSet<PaintPiece> _usedPieces = new HashSet<PaintPiece>();
    readonly List<string> _sfxLog = new List<string>();

    void LogSfx(string evtName)
    {
        float t = _capturedCount / (float)Mathf.Max(1, recordFps);
        _sfxLog.Add($"{t:F3}:{evtName}");
    }

    void Awake()
    {
        totalFramesToRecord = 420;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-nm-autoplay")
                autoPlayHooks = true;
            if (args[i] == "-nm-record")
            {
                autoPlayHooks = true;
                recordFrames = true;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    outputDir = args[i + 1];
            }
        }
    }

    void Start()
    {
        if (autoPlayHooks)
            StartCoroutine(RunChoreography());
    }

    IEnumerator RunChoreography()
    {
        _running = true;
        Screen.SetResolution(540, 960, FullScreenMode.Windowed);
        Application.targetFrameRate = recordFps;
        if (recordFrames)
        {
            Time.captureDeltaTime = 1f / recordFps;
            Directory.CreateDirectory(outputDir);
        }

        // Hide any FPS counter and static GameHud (replaced by dynamic animated Creative HUD)
        FpsCounter fps = FindAnyObjectByType<FpsCounter>();
        if (fps != null) fps.enabled = false;
        GameHud hud = FindAnyObjectByType<GameHud>();
        if (hud != null) hud.gameObject.SetActive(false);

        yield return new WaitUntil(() =>
        {
            _grid = FindAnyObjectByType<PixelPaintGrid>();
            return _grid != null && _grid.Board != null && _grid.LeftoverPieces.Count > 0;
        });

        _cam = Camera.main;
        _effects = FindAnyObjectByType<RevealEffects>();

        // Pre-prime the board so the first 4 target groups only need 1 or 2 pieces each -> rapid wave reveals!
        PrimeBoardForFastHookReveals();

        if (recordFrames)
            StartCoroutine(FrameCaptureLoop());

        _lastFingerScreen = new Vector2(Screen.width * 0.55f, Screen.height * 0.18f);
        TouchCursor.SetSimulatedPointer(true, _lastFingerScreen, false);
        yield return new WaitForSeconds(0.28f);

        int moveIndex = 0;
        bool didWrongDropHook = false;

        while (_running)
        {
            PaintPiece piece = PickBestPiece(out int targetCell);
            if (piece == null || targetCell < 0)
                break;

            Vector3 startWorld = piece.transform.position;
            Vector2 startScreen = _cam.WorldToScreenPoint(startWorld);

            // Smoothly glide finger from its previous position to the tray piece
            float moveDur = !didWrongDropHook ? 0.40f : 0.46f;
            yield return MoveFingerScreen(startScreen, moveDur, false);
            TouchCursor.SetSimulatedPointer(true, startScreen, true);
            _effects?.PlayPickup();
            LogSfx("pickup");
            piece.SetColliderEnabled(false);
            yield return new WaitForSeconds(0.12f);

            // OPENING HOOK MOMENT (First 3 Seconds, very first move): Intentional wrong-number drop!
            if (!didWrongDropHook)
            {
                didWrongDropHook = true;
                int wrongCell = FindWrongEmptyCell(piece.Number);
                if (wrongCell >= 0)
                {
                    Vector3 wrongWorld = _grid.transform.position + _grid.Board.GetCellLocalPosition(wrongCell);
                    yield return DragPieceTo(piece, startWorld, wrongWorld, 0.68f);
                    _lastFingerScreen = _cam.WorldToScreenPoint(wrongWorld);
                    TouchCursor.SetSimulatedPointer(true, _lastFingerScreen, false);
                    _grid.FlashWrongCell(wrongCell);
                    GameHud.Instance?.RegisterWrongPlacement();
                    piece.PlayRejectAndReturn();
                    _effects?.PlayReject();
                    LogSfx("reject");
                    piece.SetColliderEnabled(true);
                    yield return new WaitForSeconds(1.45f);
                    continue;
                }
            }

            // Thereafter: 100% successful, deliberate, natural-speed human gameplay!
            _usedPieces.Add(piece);
            Vector3 targetWorld = _grid.transform.position + _grid.Board.GetCellLocalPosition(targetCell);
            float dragDur = 0.62f;
            yield return DragPieceTo(piece, piece.transform.position, targetWorld, dragDur);

            _lastFingerScreen = _cam.WorldToScreenPoint(targetWorld);
            TouchCursor.SetSimulatedPointer(true, _lastFingerScreen, false);
            bool placed = _grid.TryPlacePiece(targetCell, piece.Number);
            if (placed)
            {
                piece.PlayCorrectPlacement(targetWorld);
                GameHud.Instance?.RegisterCorrectPlacement();
                LogSfx("place");
            }

            moveIndex++;
            bool groupDone = _grid.Board.IsGroupComplete(piece.Number);
            if (groupDone)
                LogSfx("reveal");
            float postPause = groupDone ? 0.85f : 0.52f;
            yield return new WaitForSeconds(postPause);
        }

        TouchCursor.SetSimulatedPointer(false, Vector2.zero, false);
    }

    void PrimeBoardForFastHookReveals()
    {
        BoardData board = _grid.Board;
        var piecesByNum = new Dictionary<int, List<PaintPiece>>();
        foreach (PaintPiece p in _grid.LeftoverPieces)
        {
            if (p == null) continue;
            if (!piecesByNum.TryGetValue(p.Number, out var list))
                piecesByNum[p.Number] = list = new List<PaintPiece>();
            list.Add(p);
        }

        // For each group, if it has more than 2 missing cells, auto-fill the extras so groups reveal fast on camera!
        int groupIdx = 0;
        foreach (var kv in board.NumberToCellIndices)
        {
            int num = kv.Key;
            List<int> cellIndices = kv.Value;
            var unfilled = new List<int>();
            foreach (int ci in cellIndices)
                if (!board.Cells[ci].filled) unfilled.Add(ci);

            int keepUnfilled = groupIdx < 3 ? 1 : (groupIdx < 8 ? 2 : 3);
            if (unfilled.Count > keepUnfilled && piecesByNum.TryGetValue(num, out var pieceList))
            {
                int toAutoFill = unfilled.Count - keepUnfilled;
                for (int k = 0; k < toAutoFill && pieceList.Count > keepUnfilled; k++)
                {
                    int ci = unfilled[k];
                    _grid.TryPlacePiece(ci, num);
                    PaintPiece extraPiece = pieceList[pieceList.Count - 1];
                    pieceList.RemoveAt(pieceList.Count - 1);
                    if (extraPiece != null)
                    {
                        _usedPieces.Add(extraPiece);
                        Destroy(extraPiece.gameObject);
                    }
                }
            }
            groupIdx++;
        }
    }

    PaintPiece PickBestPiece(out int targetCell)
    {
        targetCell = -1;
        BoardData board = _grid.Board;
        PaintPiece best = null;
        int bestRemaining = int.MaxValue;

        foreach (PaintPiece p in _grid.LeftoverPieces)
        {
            if (p == null || _usedPieces.Contains(p) || !p.gameObject.activeInHierarchy) continue;
            int rem = 0;
            int firstCell = -1;
            foreach (int ci in board.NumberToCellIndices[p.Number])
            {
                if (!board.Cells[ci].filled)
                {
                    rem++;
                    if (firstCell < 0) firstCell = ci;
                }
            }
            if (firstCell >= 0 && rem < bestRemaining)
            {
                bestRemaining = rem;
                best = p;
                targetCell = firstCell;
            }
        }
        return best;
    }

    int FindWrongEmptyCell(int pieceNumber)
    {
        BoardData board = _grid.Board;
        int startIdx = board.Cells.Length / 3;
        for (int offset = 0; offset < board.Cells.Length; offset++)
        {
            int i = (startIdx + offset) % board.Cells.Length;
            if (!board.Cells[i].filled && board.Cells[i].number != pieceNumber)
                return i;
        }
        return -1;
    }

    IEnumerator MoveFingerScreen(Vector2 targetScreen, float duration, bool pressed)
    {
        Vector2 startScreen = _lastFingerScreen;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            TouchCursor.SetSimulatedPointer(true, Vector2.Lerp(startScreen, targetScreen, k), pressed);
            yield return null;
        }
        _lastFingerScreen = targetScreen;
        TouchCursor.SetSimulatedPointer(true, targetScreen, pressed);
    }

    IEnumerator DragPieceTo(PaintPiece piece, Vector3 fromWorld, Vector3 toWorld, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (piece == null) yield break;
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            Vector3 pos = Vector3.Lerp(fromWorld, toWorld, k);
            pos.y = 0.6f + Mathf.Sin(k * Mathf.PI) * 0.35f;
            piece.transform.position = pos;
            TouchCursor.SetSimulatedPointer(true, _cam.WorldToScreenPoint(pos), true);
            yield return null;
        }
        if (piece != null)
            piece.transform.position = new Vector3(toWorld.x, 0.6f, toWorld.z);
    }

    IEnumerator FrameCaptureLoop()
    {
        while (_capturedCount < totalFramesToRecord)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(outputDir, $"frame_{_capturedCount:D4}.png");
            ScreenCapture.CaptureScreenshot(path, 2);
            _capturedCount++;
        }
        _running = false;
        Time.captureDeltaTime = 0f;
        File.WriteAllLines(Path.Combine(outputDir, "sfx_events.txt"), _sfxLog);
        Debug.Log($"[CreativeVideoDirector] Captured {_capturedCount} frames to {outputDir}");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        if (Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(), "-nm-quit") >= 0)
            UnityEditor.EditorApplication.Exit(0);
#else
        Application.Quit();
#endif
    }
}
