// Assets/Editor/SceneBackgroundEditor.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public class SceneBackgroundEditor : EditorWindow
{
    private Texture2D texture;
    private bool showBackground = false;
    private bool liveFollow = true;
    private bool lockScale = true;

    private float opacity = 1f;
    private float scaleX = 1f;
    private float scaleY = 1f;
    private float offsetX = 0f;
    private float offsetY = 0f;

    private int pixelsPerUnitIndex = 3; // default = 100
    private readonly int[] pixelsPerUnitOptions = { 16, 32, 64, 100, 128, 256 };

    private const string BG_NAME = "__EditorSceneBackground";
    private const int BG_SORTING_ORDER = -32767;

    [MenuItem("Tools/Scene Background (Editor)")]
    public static void OpenWindow()
    {
        GetWindow<SceneBackgroundEditor>("Scene BG Editor").minSize = new Vector2(360, 180);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    private void OnGUI()
    {
        GUILayout.Label("Editor Scene Background", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Texture selection
        var newTex = (Texture2D)EditorGUILayout.ObjectField("Background Texture", texture, typeof(Texture2D), false);
        if (newTex != texture)
        {
            texture = newTex;
            if (texture != null && showBackground)
                CreateOrUpdateBackground();
        }

        // Pixels per unit dropdown
        pixelsPerUnitIndex = EditorGUILayout.Popup("Pixels Per Unit", pixelsPerUnitIndex,
            pixelsPerUnitOptions.Select(v => v.ToString()).ToArray());

        // Toggle show/hide background (acts as create/remove)
        bool newShowBackground = EditorGUILayout.Toggle(
            new GUIContent("Show Background in SceneView", "Toggles background visibility. When off, removes the background GameObject."),
            showBackground);

        if (newShowBackground != showBackground)
        {
            showBackground = newShowBackground;
            if (showBackground && texture != null)
                CreateOrUpdateBackground();
            else
                RemoveBackgroundObject();
        }

        // Follow camera
        liveFollow = EditorGUILayout.Toggle("Live follow SceneView camera", liveFollow);

        // Opacity
        opacity = EditorGUILayout.Slider("Opacity", opacity, 0f, 1f);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Transform Controls", EditorStyles.boldLabel);

        // Position sliders
        offsetX = EditorGUILayout.Slider("Offset X", offsetX, -50f, 50f);
        offsetY = EditorGUILayout.Slider("Offset Y", offsetY, -50f, 50f);

        // Scale sliders + lock
        EditorGUILayout.BeginHorizontal();
        lockScale = GUILayout.Toggle(lockScale, "🔒 Lock Scale", "Button", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        float oldScaleX = scaleX;
        scaleX = EditorGUILayout.Slider("Scale X", scaleX, 1f, 3f);
        if (lockScale && !Mathf.Approximately(oldScaleX, scaleX))
            scaleY = scaleX;
        float oldScaleY = scaleY;
        scaleY = EditorGUILayout.Slider("Scale Y", scaleY, 1f, 3f);
        if (lockScale && !Mathf.Approximately(oldScaleY, scaleY))
            scaleX = scaleY;

        if (GUI.changed)
        {
            UpdateBackgroundForAllSceneViews();
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI(SceneView sv)
    {
        if (!showBackground) return;
        if (liveFollow)
            UpdateBackgroundForSceneView(sv);
    }

    private void OnHierarchyChanged()
    {
        if (showBackground)
        {
            var bg = FindBackgroundObject();
            if (bg == null)
                showBackground = false;
        }
    }

    private void CreateOrUpdateBackground()
    {
        if (texture == null) return;

        GameObject bgObj = FindBackgroundObject();
        if (bgObj == null)
        {
            bgObj = new GameObject(BG_NAME);
            Undo.RegisterCreatedObjectUndo(bgObj, "Create Editor Scene Background");
        }

        bgObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

        SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = bgObj.AddComponent<SpriteRenderer>();

        Sprite sprite = GetOrCreateSprite(texture);
        sr.sprite = sprite;
        sr.sortingOrder = BG_SORTING_ORDER;
        sr.color = new Color(1f, 1f, 1f, opacity);

        UpdateBackgroundForAllSceneViews();
    }

    private void RemoveBackgroundObject()
    {
        GameObject bg = FindBackgroundObject();
        if (bg != null)
            Undo.DestroyObjectImmediate(bg);
        SceneView.RepaintAll();
    }

    private GameObject FindBackgroundObject()
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name == BG_NAME && (go.hideFlags & HideFlags.DontSave) != 0);
    }

    private Sprite GetOrCreateSprite(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        Sprite srcSprite = subAssets.OfType<Sprite>().FirstOrDefault();

        int ppu = pixelsPerUnitOptions[pixelsPerUnitIndex];
        if (srcSprite != null)
        {
            Rect rect = srcSprite.rect;
            return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), ppu);
        }

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                             new Vector2(0.5f, 0.5f), ppu);
    }

    private void UpdateBackgroundForAllSceneViews()
    {
        foreach (SceneView sv in SceneView.sceneViews)
            UpdateBackgroundForSceneView(sv);
    }

    private void UpdateBackgroundForSceneView(SceneView sv)
    {
        GameObject bgObj = FindBackgroundObject();
        if (bgObj == null || texture == null) return;

        SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        Camera cam = sv.camera;
        if (cam == null) return;

        float zFromCam = cam.farClipPlane - 1f;
        Vector3 worldPos = cam.transform.position + cam.transform.forward * zFromCam;

        float heightWorld = cam.orthographic
            ? 2f * cam.orthographicSize
            : 2f * zFromCam * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float widthWorld = heightWorld * cam.aspect;

        float spriteWorldWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
        float spriteWorldHeight = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;

        float scaleXAuto = (widthWorld / spriteWorldWidth) * 1.01f;
        float scaleYAuto = (heightWorld / spriteWorldHeight) * 1.01f;

        bgObj.transform.position = worldPos + new Vector3(offsetX, offsetY, 0f);
        bgObj.transform.localScale = new Vector3(scaleXAuto * scaleX, scaleYAuto * scaleY, 1f);

        sr.color = new Color(1f, 1f, 1f, opacity);
        sr.sortingOrder = BG_SORTING_ORDER;
    }
}
