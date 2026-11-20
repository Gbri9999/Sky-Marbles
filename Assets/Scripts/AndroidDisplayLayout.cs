using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Keep the same 16:9 composition and touch coordinates on Android screens.
public sealed class AndroidDisplayLayout : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const float TargetAspect = 16f / 9f;
    private const int UiSortingOffset = 1000;
    private Camera sceneCamera;
    private readonly List<CanvasEntry> canvases = new List<CanvasEntry>();
    private int lastWidth;
    private int lastHeight;
    private Rect lastSafeArea;

    private struct CanvasEntry
    {
        public Canvas canvas;
        public CanvasScaler scaler;
        public Vector2 referenceResolution;
        public float match;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        GameObject root = new GameObject("Android Display Layout");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidDisplayLayout>();
    }

    private void Awake()
    {
        GameObject background = new GameObject("Display Letterbox Camera");
        background.transform.SetParent(transform, false);
        Camera camera = background.AddComponent<Camera>();
        camera.depth = -100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneCamera = Camera.main;
        canvases.Clear();
        if (sceneCamera == null) return;

        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay ||
                canvas.gameObject.scene != scene) continue;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
            canvases.Add(new CanvasEntry
            {
                canvas = canvas,
                scaler = scaler,
                referenceResolution = scaler.referenceResolution,
                match = scaler.matchWidthOrHeight
            });
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            // Camera-space canvases share sorting with sprites and tilemaps.
            // Preserve the overlay canvases' relative order above the game world.
            canvas.sortingOrder += UiSortingOffset;
            canvas.worldCamera = sceneCamera;
            canvas.planeDistance = 1f;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }
        ApplyLayout();
    }

    private void Update()
    {
        if (sceneCamera != null && (Screen.width != lastWidth || Screen.height != lastHeight ||
                                    Screen.safeArea != lastSafeArea))
            ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (sceneCamera == null || Screen.width <= 0 || Screen.height <= 0) return;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastSafeArea = Screen.safeArea;
        Rect safe = lastSafeArea;
        if (safe.width <= 0f || safe.height <= 0f)
            safe = new Rect(0f, 0f, lastWidth, lastHeight);

        float width = Mathf.Min(safe.width, safe.height * TargetAspect);
        float height = Mathf.Min(safe.height, safe.width / TargetAspect);
        float x = safe.x + (safe.width - width) * 0.5f;
        float y = safe.y + (safe.height - height) * 0.5f;
        sceneCamera.rect = new Rect(x / lastWidth, y / lastHeight, width / lastWidth, height / lastHeight);

        foreach (CanvasEntry entry in canvases)
        {
            if (entry.canvas == null || entry.scaler == null) continue;
            // Match CanvasScaler's logarithmic width/height interpolation using the viewport.
            float widthScale = width / entry.referenceResolution.x;
            float heightScale = height / entry.referenceResolution.y;
            entry.scaler.scaleFactor = Mathf.Pow(widthScale, 1f - entry.match) * Mathf.Pow(heightScale, entry.match);
        }
    }
#endif
}
