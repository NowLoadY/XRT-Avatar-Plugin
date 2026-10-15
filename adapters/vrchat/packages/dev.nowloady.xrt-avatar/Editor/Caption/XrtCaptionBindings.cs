using System;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;
using Object = UnityEngine.Object;

namespace XrtAvatarPlugin.Editor
{
    // Renaming or moving the object keeps its generated animation bindings attached.
    [InitializeOnLoad]
    internal static class XrtCaptionBindings
    {
        private static bool queued;
        static XrtCaptionBindings()
        {
            EditorApplication.hierarchyChanged += Queue;
            Undo.undoRedoPerformed += Queue;
            Queue();
        }
        private static void Queue()
        {
            if (queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            queued = true;
            EditorApplication.delayCall += Refresh;
        }
        private static void Refresh()
        {
            queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            RefreshAll();
        }

        private static void RefreshAll()
        {
            foreach (var avatar in Object.FindObjectsOfType<VRCAvatarDescriptor>(true))
            {
                try { XrtCaptionInstaller.RefreshPlacement(avatar); }
                catch (Exception failure)
                {
                    Debug.LogWarning("XRT Caption: " + failure.Message, avatar);
                }
            }
        }
    }

    public sealed class XrtCaptionBuildCallback : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => int.MinValue;
        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                XrtCaptionInstaller.RefreshBuildPlacement(avatarGameObject.GetComponent<VRCAvatarDescriptor>());
                return true;
            }
            catch (Exception failure)
            {
                Debug.LogError("XRT Caption: " + failure.Message, avatarGameObject);
                return false;
            }
        }
    }
}
