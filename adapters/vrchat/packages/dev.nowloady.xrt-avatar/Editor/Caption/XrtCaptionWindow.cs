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
        private CaptionPreview preview;
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
            preview?.Dispose();
            preview = null;
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
                preset = (XrtCaptionInstaller.Preset)CaptionAuthoring.DrawPresets((int)preset);
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

        private void DrawPreview()
        {
            Rect rect = GUILayoutUtility.GetRect(80, 145, GUILayout.ExpandWidth(true));
            if (preview == null) preview = new CaptionPreview();
            preview.Draw(rect, preset.ToString(), previewText, status.Record?.material);
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
