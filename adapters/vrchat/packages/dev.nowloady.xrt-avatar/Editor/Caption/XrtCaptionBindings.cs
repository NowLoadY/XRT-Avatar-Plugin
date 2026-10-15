using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace XrtAvatarPlugin.Editor
{
    public sealed class XrtCaptionBuildCallback : IVRCSDKPreprocessAvatarCallback, IVRCSDKPostprocessAvatarCallback
    {
        private static readonly List<string> folders = new List<string>();
        public int callbackOrder => int.MinValue;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            string folder = null;
            try
            {
                var avatar = avatarGameObject.GetComponent<VRCAvatarDescriptor>();
                if (XrtCaptionInstaller.FindRecord(avatar) == null) return true;
                folder = XrtAvatarAssets.CreateFolder("Build-");
                XrtCaptionInstaller.RefreshBuildPlacement(avatar, folder);
                folders.Add(folder);
                return true;
            }
            catch (Exception failure)
            {
                if (folder != null) AssetDatabase.DeleteAsset(folder);
                Debug.LogError("XRT Caption: " + failure.Message, avatarGameObject);
                return false;
            }
        }

        public void OnPostprocessAvatar()
        {
            foreach (string folder in folders) AssetDatabase.DeleteAsset(folder);
            folders.Clear();
        }
    }
}
