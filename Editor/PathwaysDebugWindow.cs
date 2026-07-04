using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tirt.Pathways.Editor
{
    public class PathwaysDebugWindow : EditorWindow
    {
        private Font monoFont;
        private GUIStyle monoStyle;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private GUIStyle activeStyle;
        private GUIStyle dimStyle;
        private GUIStyle deleteButtonStyle;
        private bool stylesInitialized;

        private Vector2 scrollPosition;
        private Vector2 filesScrollPosition;

        [MenuItem("Tools/Pathways/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<PathwaysDebugWindow>("Pathways Debug");
            window.minSize = new Vector2(420, 500);
            window.Show();
        }

        private void OnEnable()
        {
            monoFont = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/My Packages/Pathways/Editor/Fonts/Roboto_Mono/RobotoMono-VariableFont_wght.ttf"
            );
            stylesInitialized = false;
        }

        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;

            bool dark = EditorGUIUtility.isProSkin;

            monoStyle = new GUIStyle(EditorStyles.label)
            {
                font = monoFont,
                fontSize = 11,
                normal = { textColor = dark ? new Color(0.78f, 0.85f, 0.95f) : new Color(0.15f, 0.15f, 0.15f) }
            };

            activeStyle = new GUIStyle(monoStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = dark ? new Color(0.4f, 0.9f, 1f) : new Color(0f, 0.45f, 0.7f) }
            };

            dimStyle = new GUIStyle(monoStyle)
            {
                normal = { textColor = dark ? new Color(0.5f, 0.5f, 0.55f) : new Color(0.55f, 0.55f, 0.55f) }
            };

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = dark ? new Color(1f, 1f, 1f, 0.92f) : Color.black }
            };

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = dark ? new Color(0.75f, 0.8f, 0.9f) : new Color(0.2f, 0.2f, 0.2f) }
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(4, 4, 2, 2)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                fixedHeight = 22
            };

            deleteButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                fixedHeight = 18,
                padding = new RectOffset(4, 4, 2, 2)
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(8);
            DrawActiveProfileSection();
            EditorGUILayout.Space(6);
            DrawAllProfilesSection();
            EditorGUILayout.Space(6);
            DrawActionsSection();
            EditorGUILayout.Space(6);
            DrawConfigSection();

            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            if (Application.isPlaying)
                Repaint();
        }

        private void DrawActiveProfileSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Active Profile", headerStyle);

            SaveProfile active = Pathways.ActiveProfile;

            if (active == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "No active profile loaded. Call Pathways.LoadProfile(\"id\") at runtime.",
                    MessageType.Info
                );
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(4);

            // ── Profile summary row ──
            DrawFieldRow("Profile ID", active.ProfileId, activeStyle);

            string autoStatus = Pathways.IsAutoSaveEnabled
                ? $"Enabled  ({Pathways.AutoSaveSlots} slots, {Pathways.AutoSaveInterval}s)"
                : "Disabled";
            DrawFieldRow("Auto-Save", autoStatus, monoStyle);
            DrawFieldRow("Directory", active.FullPath, dimStyle, selectable: true);

            EditorGUILayout.Space(6);
            DrawSeparator();
            EditorGUILayout.Space(4);

            // ── Files sub-section ──
            EditorGUILayout.LabelField($"Save Files  ({active.FileCount})", subHeaderStyle);
            EditorGUILayout.Space(4);

            FileInfo[] files = active.GetSaves();

            if (files.Length == 0)
            {
                EditorGUILayout.LabelField("No save files in this profile yet.", dimStyle);
            }
            else
            {
                // Column headers
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name", EditorStyles.miniBoldLabel, GUILayout.MinWidth(220));
                EditorGUILayout.LabelField("Type", EditorStyles.miniBoldLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Modified", EditorStyles.miniBoldLabel, GUILayout.Width(110));
                GUILayout.Space(72); // reserve space for buttons
                EditorGUILayout.EndHorizontal();

                DrawSeparator();
                EditorGUILayout.Space(2);

                // Scrollable file list (caps at ~180px before scrolling)
                float listHeight = Mathf.Min(files.Length * 20f, 180f);
                filesScrollPosition = EditorGUILayout.BeginScrollView(
                    filesScrollPosition,
                    GUILayout.Height(listHeight + 4)
                );

                foreach (FileInfo file in files)
                {
                    EditorGUILayout.BeginHorizontal();

                    // Name (truncated)
                    string displayName = file.Name;
                    if (displayName.Length > 50)
                        displayName = displayName[..24] + "…" + displayName[^24..];

                    EditorGUILayout.LabelField(displayName, monoStyle, GUILayout.MinWidth(220), GUILayout.Height(18));

                    // Type badge
                    string fileType = ClassifyFile(active, file.Name);
                    EditorGUILayout.LabelField(fileType, monoStyle, GUILayout.Width(50), GUILayout.Height(18));

                    // Date
                    string date = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                    EditorGUILayout.LabelField(date, dimStyle, GUILayout.Width(110), GUILayout.Height(18));

                    // Copy button
                    if (GUILayout.Button("📋", GUILayout.Width(28), GUILayout.Height(18)))
                    {
                        EditorGUIUtility.systemCopyBuffer = file.FullName;
                        Debug.Log($"Copied to clipboard: {file.FullName}");
                    }

                    // Delete button (subtle tint)
                    Color prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
                    if (GUILayout.Button("✕", deleteButtonStyle, GUILayout.Width(28)))
                    {
                        if (
                            EditorUtility.DisplayDialog(
                                "Delete Save File",
                                $"Permanently delete '{file.Name}'?",
                                "Delete",
                                "Cancel"
                            )
                        )
                        {
                            active.DeleteFile(file.Name);
                            GUI.backgroundColor = prevBg;
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndScrollView();
                            EditorGUILayout.EndVertical();
                            return; // exit early, layout will rebuild next frame
                        }
                    }
                    GUI.backgroundColor = prevBg;

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAllProfilesSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("All Profiles", headerStyle);
            EditorGUILayout.Space(4);

            SaveProfile[] profiles = Pathways.GetAllProfiles();

            if (profiles.Length == 0)
            {
                EditorGUILayout.LabelField("No profiles found in storage location.", dimStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            // Column headers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", EditorStyles.miniBoldLabel, GUILayout.Width(130));
            EditorGUILayout.LabelField("Files", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            EditorGUILayout.LabelField("Recent", EditorStyles.miniBoldLabel, GUILayout.MinWidth(240));
            GUILayout.Space(36);
            EditorGUILayout.EndHorizontal();

            DrawSeparator();
            EditorGUILayout.Space(2);

            foreach (SaveProfile profile in profiles)
            {
                bool isActive = Pathways.ActiveProfile?.ProfileId == profile.ProfileId;
                GUIStyle rowStyle = isActive ? activeStyle : monoStyle;

                EditorGUILayout.BeginHorizontal();

                // Clickable profile name
                if (GUILayout.Button(profile.ProfileId, rowStyle, GUILayout.Width(130), GUILayout.Height(18)))
                {
                    Pathways.SetActiveProfile(profile.ProfileId);
                }

                EditorGUILayout.LabelField(
                    $"{profile.FileCount}",
                    monoStyle,
                    GUILayout.Width(40),
                    GUILayout.Height(18)
                );

                string recentFile = profile.RecentFile?.Name ?? "—";
                if (recentFile.Length > 60)
                    recentFile = recentFile[..28] + "…" + recentFile[^28..];

                EditorGUILayout.LabelField(recentFile, dimStyle, GUILayout.MinWidth(240), GUILayout.Height(18));

                // Delete profile button
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
                if (GUILayout.Button("✕", deleteButtonStyle, GUILayout.Width(28)))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Delete Profile",
                            $"Permanently delete profile '{profile.ProfileId}' and ALL its save files?",
                            "Delete",
                            "Cancel"
                        )
                    )
                    {
                        Pathways.DeleteProfile(profile.ProfileId);
                        GUI.backgroundColor = prevBg;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        return;
                    }
                }
                GUI.backgroundColor = prevBg;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Actions", headerStyle);
            EditorGUILayout.Space(4);

            // Row 1: General utilities
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh", buttonStyle))
            {
                Pathways.Refresh();
                Repaint();
            }

            if (GUILayout.Button("Reveal Storage", buttonStyle))
            {
                string path = Pathways.StorageLocation;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                EditorUtility.RevealInFinder(path);
            }

            if (GUILayout.Button("Create Test Profile", buttonStyle))
            {
                string testId = $"TestProfile_{DateTime.Now:HHmmss}";
                Pathways.LoadProfile(testId);
                string testPath = Pathways.GetPath(SaveType.Manual);
                File.WriteAllText(testPath, "{}");
                Pathways.ActiveProfile.Refresh();
            }

            EditorGUILayout.EndHorizontal();

            // Row 2: Save triggers (only when a profile is active)
            if (Pathways.ActiveProfile != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Trigger AutoSave", buttonStyle))
                {
                    string path = Pathways.RequestAutoSavePath();
                    if (!string.IsNullOrEmpty(path))
                        Debug.Log($"[Pathways] AutoSave triggered → {path}");
                    else
                        Debug.LogWarning(
                            "[Pathways] AutoSave triggered but no path was generated. Is a profile active?"
                        );
                }

                if (GUILayout.Button("Copy Latest Path", buttonStyle))
                {
                    string latest = Pathways.GetLatest();
                    if (!string.IsNullOrEmpty(latest))
                    {
                        EditorGUIUtility.systemCopyBuffer = latest;
                        Debug.Log($"Copied to clipboard: {latest}");
                    }
                    else
                    {
                        Debug.LogWarning("[Pathways] No save files exist in the active profile.");
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Configuration", headerStyle);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Set via code at startup (e.g. Pathways.StorageLocation = ...)", dimStyle);
            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Storage Location", Pathways.StorageLocation);
            EditorGUILayout.TextField("Save Extension", Pathways.SaveExtension);
            EditorGUILayout.TextField("Auto-Save Prefix", Pathways.AutoSavePrefix);
            EditorGUILayout.TextField("QuickSave Filename", Pathways.QuickSaveFileName);
            EditorGUILayout.IntField("Auto-Save Slots", Pathways.AutoSaveSlots);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawFieldRow(string label, string value, GUIStyle valueStyle, bool selectable = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(90));
            if (selectable)
                EditorGUILayout.SelectableLabel(value, valueStyle, GUILayout.Height(18));
            else
                EditorGUILayout.LabelField(value, valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSeparator()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        private static string ClassifyFile(SaveProfile profile, string fileName)
        {
            if (
                fileName.StartsWith(
                    $"{profile.ProfileId}_{Pathways.AutoSavePrefix}",
                    StringComparison.OrdinalIgnoreCase
                ) || fileName.StartsWith(Pathways.AutoSavePrefix, StringComparison.OrdinalIgnoreCase)
            )
                return "Auto";

            if (
                fileName.Equals(
                    $"{profile.ProfileId}_{Pathways.QuickSaveFileName}.{Pathways.SaveExtension}",
                    StringComparison.OrdinalIgnoreCase
                )
                || fileName.Equals(
                    $"{Pathways.QuickSaveFileName}.{Pathways.SaveExtension}",
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return "Quick";

            return "Manual";
        }
    }
}
