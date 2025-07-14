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

            DrawManagerSection(manager);
            DrawCurrentPathwaySection(manager);
            DrawAllPathwaysSection(manager);
            DrawActionButtons(manager);
        }

        private void DrawManagerSection(PathwaysManager manager)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Pathway Manager", headerStyle);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Current Pathway: {manager.CurrentPathway?.PathwayId ?? "None"}");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Auto-Save: {(manager.CanAutoSave ? "Enabled" : "Disabled")}",
                GUILayout.Width(150)
            );
            EditorGUILayout.LabelField($"Slots: {manager.AutoSaveSlots}", GUILayout.Width(50));
            EditorGUILayout.LabelField($"Interval: {manager.AutoSaveInterval} seconds");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Use Unscaled Time: {manager.UseUnscaledTime}");

            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentPathwaySection(PathwaysManager manager)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Current Pathway", headerStyle);
            EditorGUILayout.Space();

            Pathway currentPathway = manager.CurrentPathway;

            if (currentPathway != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Pathway:", GUILayout.Width(100));
                EditorGUILayout.SelectableLabel(currentPathway.PathwayId, monoStyle, GUILayout.Height(18));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Auto-Save Slots:", GUILayout.Width(100));
                EditorGUILayout.LabelField($"{currentPathway.AutoSaveSlots}", monoStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Files:", GUILayout.Width(100));
                EditorGUILayout.LabelField($"{currentPathway.FileCount}", monoStyle, GUILayout.Width(40));

                int maxRecentFileLength = 40;
                string recentFileName = currentPathway.RecentFile?.Name ?? "None";
                if (recentFileName.Length > maxRecentFileLength)
                    recentFileName = "..." + recentFileName[^(maxRecentFileLength - 3)..];

                EditorGUILayout.LabelField($"Recent: {recentFileName}", monoStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Path:", GUILayout.Width(100));
                EditorGUILayout.SelectableLabel(currentPathway.Path, monoStyle, GUILayout.Height(18));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No active pathway.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAllPathwaysSection(PathwaysManager manager)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("All Pathways", headerStyle);
            EditorGUILayout.Space();

            Pathway[] pathways = manager.GetAllPathways();

            if (pathways.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    $"No pathways found at:\n{PathwaysGlobalConfigs.StorageLocation}",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Pathway ID", EditorStyles.miniBoldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField("Files", EditorStyles.miniBoldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Recent File", EditorStyles.miniBoldLabel);
                EditorGUILayout.EndHorizontal();

                Rect rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, Color.gray);

                EditorGUILayout.Space(2);

                foreach (Pathway pathway in pathways)
                {
                    EditorGUILayout.BeginHorizontal();

                    Color originalColor = GUI.color;
                    if (manager.CurrentPathway?.PathwayId == pathway.PathwayId)
                    {
                        GUI.color = Color.cyan;
                    }

                    if (GUILayout.Button(pathway.PathwayId, monoStyle, GUILayout.Width(200), GUILayout.Height(18)))
                    {
                        manager.SetCurrentPathway(pathway.PathwayId);
                    }

                    GUI.color = originalColor;

                    EditorGUILayout.LabelField($"{pathway.FileCount}", monoStyle, GUILayout.Width(50));

                    int maxRecentFileLength = 40;
                    string recentFile = pathway.RecentFile?.Name ?? "None";
                    if (recentFile.Length > maxRecentFileLength)
                        recentFile = "..." + recentFile[^(maxRecentFileLength - 3)..];

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

            if (GUILayout.Button("Refresh Pathway", buttonStyle, GUILayout.Height(28)))
            {
                manager.RefreshCurrentPathway();
                Repaint();
            }

            if (GUILayout.Button("Open Storage Folder", buttonStyle, GUILayout.Height(28)))
            {
                string path = PathwaysGlobalConfigs.StorageLocation;
                if (Directory.Exists(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    Application.OpenURL(path);
                }
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
                    if (GUILayout.Button("Get Manual Save Path", buttonStyle))
                    {
                        string path = manager.GetManualSavePath();
                        Debug.Log($"Copied to clipboard! Manual save path: {path}");
                        EditorGUIUtility.systemCopyBuffer = path;
                    }

                    if (GUILayout.Button("Get Auto Save Path", buttonStyle))
                    {
                        string path = manager.GetAutoSavePath();
                        Debug.Log($"Copied to clipboard! Auto save path: {path}");
                        EditorGUIUtility.systemCopyBuffer = path;
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Storage: {PathwaysGlobalConfigs.StorageLocation}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Extension: *.{PathwaysGlobalConfigs.SaveExtension}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
    }
}
