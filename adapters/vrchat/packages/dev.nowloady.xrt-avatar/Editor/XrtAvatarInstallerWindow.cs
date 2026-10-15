using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace XrtAvatarPlugin.Editor
{
    public sealed class XrtAvatarInstallerWindow : EditorWindow
    {
        private const string GuideUrl = "https://github.com/NowLoadY/XRT-Avatar-Plugin/blob/main/docs/adapters/vrchat/installation.md";
        [SerializeField] private VRCAvatarDescriptor avatar;
        [SerializeField] private bool showInGame;
        private XrtAvatarInstaller.InstallationStatus status;
        private Vector2 scroll;
        private string message;
        private MessageType messageType;

        [MenuItem("Tools/XRT-Avatar-Plugin/Avatar Controls")]
        private static void Open() => GetWindow<XrtAvatarInstallerWindow>("XRT · VRChat");

        private void OnEnable()
        {
            minSize = new Vector2(360, 230);
            if (avatar == null)
                avatar = SelectedAvatar();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RefreshStatus();
        }

        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        private void OnPlayModeStateChanged(PlayModeStateChange state) => RefreshStatus();
        private void OnFocus() => RefreshStatus();
        private void OnProjectChange() => RefreshStatus();
        private void OnHierarchyChange() => RefreshStatus();
        private void OnSelectionChange() => Repaint();

        private void OnGUI()
        {
            if (status == null)
                RefreshStatus();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("VRChat", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (IconButton("Refresh", "Check this avatar", EditorStyles.toolbarButton))
                {
                    message = null;
                    RefreshStatus();
                    GUIUtility.ExitGUI();
                }
                using (new EditorGUI.DisabledScope(!CanSaveScene()))
                    if (IconButton("SaveActive", "Save avatar scene", EditorStyles.toolbarButton))
                        Run(() => EditorSceneManager.SaveScene(avatar.gameObject.scene)
                            ? "Scene saved." : "Scene was not saved.");
                if (IconButton("_Help", "Open setup guide", EditorStyles.toolbarButton))
                    Application.OpenURL(GuideUrl);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Avatar", GUILayout.Width(44));
                EditorGUI.BeginChangeCheck();
                var selected = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                    avatar, typeof(VRCAvatarDescriptor), true);
                if (EditorGUI.EndChangeCheck())
                    ChooseAvatar(selected);
                using (new EditorGUI.DisabledScope(SelectedAvatar() == null))
                    if (IconButton("Avatar Icon", "Use the avatar selected in the Hierarchy", EditorStyles.miniButton))
                        ChooseAvatar(SelectedAvatar());
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Translation menu", EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(avatar == null || EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        using (new EditorGUI.DisabledScope(!status.CanInstall))
                            if (IconButton("Toolbar Plus", "Add translation menu", EditorStyles.miniButtonLeft))
                                Run(() => XrtAvatarInstaller.Install(avatar));
                        using (new EditorGUI.DisabledScope(!status.CanRemove))
                            if (IconButton("Toolbar Minus", "Remove translation menu", EditorStyles.miniButtonRight))
                                Run(() => XrtAvatarInstaller.Restore(avatar));
                    }
                }
                EditorGUILayout.LabelField(status.Title, EditorStyles.miniLabel);
                bool showMessage = message != null &&
                    (messageType == MessageType.Error || status.Severity != MessageType.Warning);
                string detail = showMessage ? message : status.Detail;
                MessageType severity = showMessage ? messageType : status.Severity;
                if (severity == MessageType.None)
                    EditorGUILayout.LabelField(detail, EditorStyles.wordWrappedLabel);
                else
                    EditorGUILayout.HelpBox(detail, severity);
            }

            EditorGUILayout.Space(6);
            showInGame = EditorGUILayout.Foldout(showInGame, "In game", true);
            if (showInGame)
                EditorGUILayout.LabelField(
                    "Upload this avatar, then turn on OSC > Enabled in VRChat. In XRTranslate, enable Avatar translation controls and select VRChat. Use Expressions > XRT > Translation to start or stop.",
                    EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private static bool IconButton(string name, string tooltip, GUIStyle style)
        {
            var content = new GUIContent(EditorGUIUtility.IconContent(name).image, tooltip);
            return GUILayout.Button(content, style, GUILayout.Width(28), GUILayout.Height(22));
        }

        private bool CanSaveScene() => avatar != null && !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorUtility.IsPersistent(avatar) && PrefabStageUtility.GetPrefabStage(avatar.gameObject) == null &&
            avatar.gameObject.scene.IsValid() && (avatar.gameObject.scene.isDirty ||
                string.IsNullOrEmpty(avatar.gameObject.scene.path));

        private static VRCAvatarDescriptor SelectedAvatar() => Selection.activeGameObject == null ? null :
            Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();

        private void ChooseAvatar(VRCAvatarDescriptor selected)
        {
            avatar = selected;
            message = null;
            RefreshStatus();
            GUIUtility.ExitGUI();
        }

        private void RefreshStatus()
        {
            status = XrtAvatarInstaller.Inspect(avatar);
            Repaint();
        }

        private void Run(Func<string> operation)
        {
            try
            {
                message = operation();
                messageType = MessageType.None;
            }
            catch (Exception exception)
            {
                message = exception is InvalidOperationException ? exception.Message :
                    "Could not save this change. Check the Unity Console for details.";
                messageType = MessageType.Error;
                Debug.LogException(exception);
            }
            finally
            {
                RefreshStatus();
            }
            GUIUtility.ExitGUI();
        }
    }
}
