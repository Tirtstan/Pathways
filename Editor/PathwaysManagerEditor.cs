using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pathways.Editor
{
    [CustomEditor(typeof(PathwaysManager))]
    public class PathwaysManagerEditor : UnityEditor.Editor
    {
        [SerializeField]
        private Font monoFont;
        private static GUIStyle monoStyle;
        private static GUIStyle headerStyle;
        private static GUIStyle boxStyle;
        private static GUIStyle buttonStyle;
        private static bool stylesInitialized = false;

        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;

            monoStyle = new GUIStyle(EditorStyles.label)
            {
                font = monoFont,
                fontSize = 11,
                normal = { textColor = new Color(0.8f, 0.9f, 1f, 1f) }
            };

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(1f, 1f, 1f, 0.9f) }
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(5, 5, 2, 2)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                fixedHeight = 25
            };

            stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();
            DrawDefaultInspector();
            EditorGUILayout.Space(10);

            PathwaysManager manager = target as PathwaysManager;

            DrawCurrentPathwaySection(manager);
            DrawAllPathwaysSection(manager);
            DrawActionButtons(manager);
            DrawConfigInfo();
        }

        private void DrawCurrentPathwaySection(PathwaysManager manager)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Current Pathway", headerStyle);
            EditorGUILayout.Space();

            Pathway currentPathway = manager.CurrentPathway;

            if (currentPathway != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("ID:", GUILayout.Width(80));
                EditorGUILayout.SelectableLabel(currentPathway.PathwayId, monoStyle, GUILayout.Height(18));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                string autoSaveStatus = manager.CanAutoSave() ? "ON" : "OFF";
                EditorGUILayout.LabelField("Auto-Save:", GUILayout.Width(80));
                EditorGUILayout.LabelField(
                    $"{autoSaveStatus} ({manager.AutoSaveSlots} slots, {manager.AutoSaveInterval}s)",
                    monoStyle
                );
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("File Count:", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{currentPathway.FileCount}", monoStyle, GUILayout.Width(30));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                int maxFileCount = 35;
                string recentFileName = currentPathway.RecentFile?.Name ?? "None";
                if (recentFileName.Length > maxFileCount)
                    recentFileName = "..." + recentFileName[^(maxFileCount - 3)..];

                EditorGUILayout.LabelField($"Recent:", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{recentFileName}", monoStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Path:", GUILayout.Width(80));
                EditorGUILayout.SelectableLabel(currentPathway.FullPath, monoStyle, GUILayout.Height(18));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No active pathway selected.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAllPathwaysSection(PathwaysManager manager)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Available Pathways", headerStyle);
            EditorGUILayout.Space();

            Pathway[] pathways = manager.GetAllPathways();

            if (pathways.Length == 0)
            {
                EditorGUILayout.HelpBox("No pathways found.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📁 Pathway ID", EditorStyles.miniBoldLabel, GUILayout.Width(150));
                EditorGUILayout.LabelField("Files", EditorStyles.miniBoldLabel, GUILayout.Width(40));
                EditorGUILayout.LabelField("📂 Recent File", EditorStyles.miniBoldLabel);
                EditorGUILayout.EndHorizontal();

                Rect rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, Color.gray);
                EditorGUILayout.Space(2);

                foreach (Pathway pathway in pathways)
                {
                    EditorGUILayout.BeginHorizontal();

                    Color originalColor = GUI.color;
                    if (manager.CurrentPathway?.PathwayId == pathway.PathwayId)
                        GUI.color = Color.cyan;

                    if (GUILayout.Button(pathway.PathwayId, monoStyle, GUILayout.Width(150), GUILayout.Height(18)))
                        manager.SetCurrentPathway(pathway.PathwayId);

                    GUI.color = originalColor;

                    EditorGUILayout.LabelField($"{pathway.FileCount}", monoStyle, GUILayout.Width(40));

                    string recentFile = pathway.RecentFile?.Name ?? "None";
                    if (recentFile.Length > 30)
                        recentFile = "..." + recentFile[^27..];

                    EditorGUILayout.LabelField(recentFile, monoStyle);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons(PathwaysManager manager)
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", buttonStyle, GUILayout.Height(28)))
            {
                manager.Refresh();
                Repaint();
            }

            if (GUILayout.Button("Open Storage Folder", buttonStyle, GUILayout.Height(28)))
            {
                string path = PathwaysGlobalConfigs.StorageLocation;
                if (Directory.Exists(path))
                    EditorUtility.RevealInFinder(path);
                else
                    Application.OpenURL(path);
            }
            EditorGUILayout.EndHorizontal();

            if (Debug.isDebugBuild)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.LabelField("Debug Tools", headerStyle);
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Test Pathway", buttonStyle))
                {
                    string testPathwayId = $"TestPathway_{System.DateTime.Now:HHmmss}";
                    manager.SetCurrentPathway(testPathwayId);
                }

                if (manager.CurrentPathway != null)
                {
                    if (GUILayout.Button("Copy Recent Save Path", buttonStyle))
                    {
                        string path = manager.GetOrCreateRecentSavePath();
                        EditorGUIUtility.systemCopyBuffer = path;
                        Debug.Log($"Copied recent save path to clipboard: {path}");
                    }

                    if (GUILayout.Button("Copy Auto Save Path", buttonStyle))
                    {
                        string path = manager.GetAutoSavePath();
                        EditorGUIUtility.systemCopyBuffer = path;
                        Debug.Log($"Copied auto save path to clipboard: {path}");
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawConfigInfo()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Storage: {PathwaysGlobalConfigs.StorageLocation}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Extension: *.{PathwaysGlobalConfigs.SaveExtension}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
    }
}
