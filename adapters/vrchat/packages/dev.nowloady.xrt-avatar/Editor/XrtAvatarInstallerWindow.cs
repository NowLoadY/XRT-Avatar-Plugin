using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace XrtAvatarPlugin.Editor
{
    public sealed class XrtAvatarInstallerWindow : EditorWindow
    {
        [SerializeField] private VRCAvatarDescriptor avatar;
        private XrtAvatarInstaller.InstallationStatus status;
        private Vector2 scroll;
        private string message;
        private MessageType messageType = MessageType.Info;

        [MenuItem("Tools/XRT-Avatar-Plugin/Avatar Controls")]
        private static void Open()
        {
            GetWindow<XrtAvatarInstallerWindow>("XRT Avatar Controls");
        }

        private void OnEnable()
        {
            minSize = new Vector2(420, 380);
            if (avatar == null)
                avatar = SelectedAvatar();
            RefreshStatus();
        }

        private void OnFocus() => RefreshStatus();
        private void OnProjectChange() => RefreshStatus();
        private void OnHierarchyChange() => RefreshStatus();

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("XRT-Avatar-Plugin", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Control XRTranslate from your avatar's Expressions menu.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            var selected = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                "Avatar", avatar, typeof(VRCAvatarDescriptor), true);
            if (EditorGUI.EndChangeCheck())
            {
                avatar = selected;
                message = null;
                RefreshStatus();
                GUIUtility.ExitGUI();
            }
            using (new EditorGUI.DisabledScope(SelectedAvatar() == null))
                if (GUILayout.Button("Use Selected Avatar"))
                {
                    avatar = SelectedAvatar();
                    message = null;
                    RefreshStatus();
                    GUIUtility.ExitGUI();
                }

            if (status == null)
                RefreshStatus();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(status.Title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(status.Detail, status.Severity);

            using (new EditorGUI.DisabledScope(avatar == null || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                using (new EditorGUI.DisabledScope(!status.CanInstall))
                    if (GUILayout.Button(status.InstallLabel, GUILayout.Height(30)))
                        Run(() => XrtAvatarInstaller.Install(avatar));
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Check Installation"))
                    {
                        message = null;
                        RefreshStatus();
                        GUIUtility.ExitGUI();
                    }
                    using (new EditorGUI.DisabledScope(!status.CanRemove))
                        if (GUILayout.Button("Remove Translation Menu"))
                            Run(() => XrtAvatarInstaller.Restore(avatar));
                }
                if (avatar != null && avatar.gameObject.scene.IsValid() &&
                    (avatar.gameObject.scene.isDirty || string.IsNullOrEmpty(avatar.gameObject.scene.path)))
                    if (GUILayout.Button("Save Avatar Scene"))
                        Run(() => EditorSceneManager.SaveScene(avatar.gameObject.scene)
                            ? "Avatar scene saved." : "Scene was not saved.");
            }
            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, messageType);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("After uploading", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Enable OSC in VRChat and the VRChat Avatar connector in XRTranslate. " +
                "Open Expressions > XRT > Translation in VRChat to start or stop translation.",
                EditorStyles.wordWrappedLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Setup Guide"))
                    Application.OpenURL("https://github.com/NowLoadY/XRT-Avatar-Plugin/blob/main/docs/adapters/vrchat/installation.md");
                if (GUILayout.Button("Get XRTranslate"))
                    Application.OpenURL("https://github.com/NowLoadY/XRTranslate/releases/tag/v0.3.4-preview.1");
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Preview release. Unity and in-game verification are pending.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private static VRCAvatarDescriptor SelectedAvatar()
        {
            return Selection.activeGameObject == null ? null :
                Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
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
                messageType = MessageType.Info;
            }
            catch (Exception exception)
            {
                message = exception.Message;
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
