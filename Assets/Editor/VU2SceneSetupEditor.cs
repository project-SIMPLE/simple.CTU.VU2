using UnityEditor;
using UnityEngine;

// =============================================================================
// VU2SceneSetupEditor - Editor menu tool for verifying/previewing scene setup.
// VU2SceneSetupEditor - Công cụ menu Editor để kiểm tra/xem trước cài đặt scene.
//
// Menu: Tools → VU2 → Scene Setup
// =============================================================================
public class VU2SceneSetupEditor : EditorWindow
{
    [MenuItem("Tools/VU2/Scene Setup Status")]
    public static void ShowWindow()
    {
        GetWindow<VU2SceneSetupEditor>("VU2 Scene Setup");
    }

    private Vector2 _scrollPos;

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        EditorGUILayout.LabelField("VU2 Scene Setup Status", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Check each required component.
        DrawStatus("RulesoftheGame_VU2_1", Object.FindObjectOfType<RulesoftheGame_VU2_1>() != null);
        DrawStatus("VU2SceneBootstrapper", Object.FindObjectOfType<VU2SceneBootstrapper>() != null,
            "Auto-added at runtime by RulesoftheGame_VU2_1.Awake()");
        DrawStatus("TidalClockManager", TidalClockManager.Instance != null || Object.FindObjectOfType<TidalClockManager>() != null,
            "Auto-created by bootstrapper at StartGame()");
        DrawStatus("TidalClockUI", Object.FindObjectOfType<TidalClockUI>() != null,
            "Auto-created by bootstrapper at StartGame()");
        DrawStatus("EnemySpawner", Object.FindObjectOfType<EnemySpawner>() != null,
            "Auto-created by bootstrapper at StartGame()");
        DrawStatus("EnemyController (in prefab)", HasSaltyWaterPrefab(),
            "Resources/Prefabs/SaltyWater");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Scene Objects", EditorStyles.boldLabel);

        // Count trees.
        Tree[] trees = Object.FindObjectsOfType<Tree>();
        DrawStatus($"Tree objects: {trees.Length}", trees.Length > 0);

        // Check NavMesh.
        bool hasNavMesh = UnityEngine.AI.NavMesh.SamplePosition(Vector3.zero, 
            out UnityEngine.AI.NavMeshHit hit, 100f, UnityEngine.AI.NavMesh.AllAreas);
        DrawStatus("NavMesh baked", hasNavMesh, 
            hasNavMesh ? "OK" : "Not baked — enemies will use fallback movement (Transform.MoveTowards)");

        // Check water target on RulesoftheGame.
        var rules = Object.FindObjectOfType<RulesoftheGame_VU2_1>();
        if (rules != null)
        {
            DrawStatus("Water target assigned", rules.target != null);
            DrawStatus($"monthDuration: {rules.monthDuration}s", true);
            DrawStatus($"totalGameDuration: {rules.totalGameDuration}s", true);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "All missing components are auto-created at runtime by VU2SceneBootstrapper.\n" +
            "The bootstrapper is auto-added in RulesoftheGame_VU2_1.Awake() and\n" +
            "Bootstrap() is called in StartGame().\n\n" +
            "No manual scene setup is required — just press Play!",
            MessageType.Info);

        // Manual bake NavMesh button.
        EditorGUILayout.Space(5);
        if (!hasNavMesh)
        {
            EditorGUILayout.HelpBox(
                "NavMesh is NOT baked. Enemies will still work via fallback movement,\n" +
                "but NavMesh gives better pathfinding. Use Window → AI → Navigation to bake.",
                MessageType.Warning);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawStatus(string label, bool ok, string hint = null)
    {
        EditorGUILayout.BeginHorizontal();
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = ok ? new Color(0.1f, 0.7f, 0.1f) : new Color(0.9f, 0.6f, 0.1f);
        EditorGUILayout.LabelField(ok ? "✓" : "○", style, GUILayout.Width(20));
        EditorGUILayout.LabelField(label);
        if (!string.IsNullOrEmpty(hint))
        {
            GUIStyle hintStyle = new GUIStyle(EditorStyles.miniLabel);
            hintStyle.normal.textColor = Color.gray;
            EditorGUILayout.LabelField(hint, hintStyle);
        }
        EditorGUILayout.EndHorizontal();
    }

    private bool HasSaltyWaterPrefab()
    {
        return Resources.Load<GameObject>("Prefabs/SaltyWater") != null;
    }
}
