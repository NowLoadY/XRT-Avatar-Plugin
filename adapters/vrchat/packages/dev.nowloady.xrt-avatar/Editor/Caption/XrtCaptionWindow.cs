using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using XrtAvatarPlugin.Presentation;

namespace XrtAvatarPlugin.Editor
{
    internal sealed class XrtCaptionWindow : EditorWindow
    {
        [SerializeField] private VRCAvatarDescriptor avatar;
        [SerializeField] private Transform attachment;
        [SerializeField] private XrtCaptionInstaller.Preset preset;
        [SerializeField] private string previewText = "The entrance is on the right.\nWe'll wait near the stairs.";
        private string error;
        private PreviewRenderUtility preview;
        private Material previewMaterial;
        private Mesh previewMesh;
        private XrtCaptionInstaller.Status status;

        [MenuItem("Tools/XRT-Avatar-Plugin/Caption Objects")]
        internal static void Open()
        {
            var window = GetWindow<XrtCaptionWindow>();
            window.titleContent = new GUIContent("XRT · Captions");
            window.minSize = new Vector2(360, 430);
            window.Show();
        }

        [MenuItem("CONTEXT/VRCAvatarDescriptor/XRT Caption Objects")]
        private static void OpenFromAvatar(MenuCommand command)
        {
            Open();
            var window = GetWindow<XrtCaptionWindow>();
            window.SelectAvatar(command.context as VRCAvatarDescriptor);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Refresh;
            EditorApplication.playModeStateChanged += OnPlayMode;
            FollowSelection();
            Refresh();
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Refresh;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            preview?.Cleanup();
            preview = null;
            if (previewMaterial != null) DestroyImmediate(previewMaterial);
            if (previewMesh != null) DestroyImmediate(previewMesh);
        }
        private void OnSelectionChange() { FollowSelection(); Refresh(); }
        private void OnHierarchyChange() => Refresh();
        private void OnProjectChange() => Refresh();
        private void OnFocus() => Refresh();
        private void OnPlayMode(PlayModeStateChange _) => Refresh();
        private void Refresh()
        {
            error = null;
            status = XrtCaptionInstaller.Inspect(avatar);
            if (status.Record != null)
            {
                preset = status.Record.columns == 8 ? XrtCaptionInstaller.Preset.Portrait :
                    status.Record.material != null && status.Record.material.GetColor("_BackgroundColor").a == 0
                    ? XrtCaptionInstaller.Preset.Board : XrtCaptionInstaller.Preset.Horizontal;
                if (status.Renderer != null) attachment = status.Renderer.transform.parent;
            }
            Repaint();
        }
        private void FollowSelection()
        {
            var selected = Selection.activeGameObject == null ? null :
                Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
            if (selected != null && selected != avatar) SelectAvatar(selected);
        }
        private void SelectAvatar(VRCAvatarDescriptor selected)
        {
            avatar = selected;
            attachment = avatar == null ? null : avatar.transform;
            Refresh();
        }

        private void OnGUI()
        {
            if (status == null) status = XrtCaptionInstaller.Inspect(avatar);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("VRChat", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (Icon("Refresh", "Refresh", EditorStyles.toolbarButton)) Refresh();
                using (new EditorGUI.DisabledScope(avatar == null || !avatar.gameObject.scene.isDirty))
                    if (Icon("SaveActive", "Save scene", EditorStyles.toolbarButton))
                        Run(() => EditorSceneManager.SaveScene(avatar.gameObject.scene));
                if (Icon("_Help", "Open guide", EditorStyles.toolbarButton))
                    Application.OpenURL("https://github.com/NowLoadY/XRT-Avatar-Plugin/blob/main/docs/adapters/vrchat/README.md");
            }
            GUILayout.Space(8);
            EditorGUI.BeginChangeCheck();
            var selectedAvatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatar,
                typeof(VRCAvatarDescriptor), true);
            if (EditorGUI.EndChangeCheck()) SelectAvatar(selectedAvatar);
            using (new EditorGUI.DisabledScope(status.Record != null))
            {
                attachment = (Transform)EditorGUILayout.ObjectField("Attach to", attachment, typeof(Transform), true);
                GUILayout.Space(8);
                DrawPresets();
            }
            GUILayout.Space(8);
            DrawPreview();
            GUILayout.Label("Preview text", EditorStyles.miniLabel);
            previewText = EditorGUILayout.TextArea(previewText, GUILayout.Height(48));
            GUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(status.Title, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(!status.CanAdd))
                        if (Icon("Toolbar Plus", "Add caption object"))
                            Run(() => XrtCaptionInstaller.Install(avatar, attachment, preset, previewText));
                    using (new EditorGUI.DisabledScope(!status.CanRemove))
                        if (Icon("Toolbar Minus", "Remove caption object"))
                            Run(() => XrtCaptionInstaller.Remove(avatar));
                    using (new EditorGUI.DisabledScope(status.Renderer == null))
                        if (Icon("ViewToolZoom", "Select caption object"))
                        {
                            Selection.activeGameObject = status.Renderer.gameObject;
                            SceneView.lastActiveSceneView?.FrameSelected();
                        }
                }
                GUILayout.Label(status.Detail, EditorStyles.wordWrappedMiniLabel);
            }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        private void DrawPresets()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < 3; i++)
                {
                    var value = (XrtCaptionInstaller.Preset)i;
                    Rect area = GUILayoutUtility.GetRect(80, 66, GUILayout.ExpandWidth(true));
                    bool selected = preset == value;
                    if (GUI.Toggle(area, selected, GUIContent.none, "Button") && !selected) preset = value;
                    var shape = value == XrtCaptionInstaller.Preset.Portrait
                        ? new Rect(area.center.x - 8, area.y + 9, 16, 25)
                        : new Rect(area.center.x - 20, area.y + 15, 40, 15);
                    Handles.BeginGUI();
                    Handles.color = EditorGUIUtility.isProSkin ? new Color(.8f, .8f, .8f) : new Color(.35f, .4f, .42f);
                    Handles.DrawAAPolyLine(1.5f, new Vector3(shape.x, shape.y), new Vector3(shape.xMax, shape.y),
                        new Vector3(shape.xMax, shape.yMax), new Vector3(shape.x, shape.yMax), new Vector3(shape.x, shape.y));
                    for (int row = 0; row < (value == XrtCaptionInstaller.Preset.Portrait ? 4 : 2); row++)
                    {
                        float y = shape.y + 5 + row * 4;
                        Handles.DrawAAPolyLine(1, new Vector3(shape.x + 4, y), new Vector3(shape.xMax - 4, y));
                    }
                    if (value == XrtCaptionInstaller.Preset.Board)
                        Handles.DrawAAPolyLine(1.5f, new Vector3(shape.x + 7, shape.yMax + 3),
                            new Vector3(shape.x + 12, shape.yMax - 2), new Vector3(shape.x + 18, shape.yMax + 3));
                    Handles.EndGUI();
                    GUI.Label(new Rect(area.x, area.yMax - 23, area.width, 18), value.ToString(),
                        new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
                }
            }
        }

        private void DrawPreview()
        {
            Rect rect = GUILayoutUtility.GetRect(80, 145, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) return;
            try
            {
                if (preview == null) preview = new PreviewRenderUtility();
                if (previewMesh == null) previewMesh = CaptionAssets.CreateMesh();
                if (previewMaterial == null) previewMaterial = CaptionAssets.CreateMaterial();
                int columns = status.Record != null ? status.Record.columns :
                    preset == XrtCaptionInstaller.Preset.Portrait ? 8 : 32;
                if (status.Record != null && status.Record.material != null)
                    previewMaterial.CopyPropertiesFromMaterial(status.Record.material);
                else
                {
                    previewMaterial.SetColor("_BackgroundColor", preset == XrtCaptionInstaller.Preset.Board
                        ? Color.clear : new Color(.98f, .98f, .98f, .92f));
                }
                previewMaterial.SetFloat("_Columns", columns);
                XrtCaptionInstaller.SetPreview(previewMaterial, previewText, columns);
                preview.BeginPreview(rect, GUIStyle.none);
                var camera = preview.camera;
                camera.orthographic = true;
                Vector3 size = columns == 8 ? new Vector3(.24f, .60f, 1) : new Vector3(.64f, .10f, 1);
                camera.aspect = rect.width / rect.height;
                camera.orthographicSize = Mathf.Max(size.y * .5f, size.x * .5f / camera.aspect) * 1.15f;
                camera.transform.position = new Vector3(0, 0, -2);
                camera.transform.rotation = Quaternion.identity;
                camera.clearFlags = CameraClearFlags.Color;
                camera.backgroundColor = new Color(.91f, .94f, .94f);
                preview.DrawMesh(previewMesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, size), previewMaterial, 0);
                camera.Render();
                GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
            }
            catch (Exception failure)
            {
                GUI.Label(rect, failure.Message, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static bool Icon(string name, string tooltip, GUIStyle style = null)
        {
            var content = EditorGUIUtility.IconContent(name);
            content.tooltip = tooltip;
            return GUILayout.Button(content, style ?? GUI.skin.button, GUILayout.Width(28), GUILayout.Height(24));
        }
        private void Run(Action operation)
        {
            try { operation(); Refresh(); }
            catch (Exception failure) { error = failure.Message; }
            GUIUtility.ExitGUI();
        }
    }
}
