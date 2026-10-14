using System;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace XrtAvatarPlugin.Editor
{
    public sealed class XrtAvatarInstallerWindow : EditorWindow
    {
        [SerializeField] private VRCAvatarDescriptor avatar;
        private string message;
        private MessageType messageType = MessageType.Info;

        [MenuItem("Tools/XRT-Avatar-Plugin/Avatar Controls")]
        private static void Open()
        {
            GetWindow<XrtAvatarInstallerWindow>("XRT Avatar Controls");
        }

        private void OnEnable()
        {
            if (avatar == null && Selection.activeGameObject != null)
                avatar = Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("XRT-Avatar-Plugin", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Development preview. Adds XRT > Translation to this avatar's Expressions menu. " +
                "The XRTranslate host adapter is not yet integrated; this preview installs control assets only.",
                MessageType.Info);
            avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                "Avatar", avatar, typeof(VRCAvatarDescriptor), true);
            EditorGUILayout.HelpBox(
                "Use an avatar in a saved scene with Custom Expressions enabled and saved menu/parameter assets assigned. " +
                "Original menu and parameter assets are preserved. " +
                "Restore removes the installation only when its assets and descriptor references are unchanged. " +
                "Save the scene after installing or restoring.", MessageType.None);

            using (new EditorGUI.DisabledScope(avatar == null || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Install / Check Installation"))
                    Run(() => XrtAvatarInstaller.Install(avatar));
                if (GUILayout.Button("Restore Original Expressions"))
                    Run(() => XrtAvatarInstaller.Restore(avatar));
            }
            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, messageType);
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
        }
    }
}
