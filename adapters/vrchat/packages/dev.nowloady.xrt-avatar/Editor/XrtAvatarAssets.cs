using System;
using System.Collections.Generic;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using XrtAvatarPlugin.Presentation;

namespace XrtAvatarPlugin.Editor
{
    internal static class XrtAvatarAssets
    {
        // Saved scenes and Undo can still reference the old graph. Every edit gets
        // its own working assets, even when another avatar is not currently loaded.
        internal static void Edit(VRCAvatarDescriptor avatar, string action,
            Action<XrtAvatarInstallationRecord, XrtCaptionInstallationRecord> edit)
        {
            var controls = XrtAvatarInstaller.FindAssignedRecord(avatar);
            var caption = XrtCaptionInstaller.FindRecord(avatar);
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(action);
            string folder = null;
            bool complete = false;
            var copies = new AssetCopies();
            try
            {
                var menus = new HashSet<VRCExpressionsMenu>();
                AddMenu(copies, avatar.expressionsMenu, menus);
                copies.Add(avatar.expressionParameters);
                if (controls != null)
                {
                    copies.Add(controls);
                    AddMenu(copies, controls.installedMenu, menus);
                    AddMenu(copies, controls.xrtMenu, menus);
                    copies.Add(controls.installedParameters);
                }
                if (caption != null)
                {
                    copies.Add(caption);
                    copies.Add(caption.originalParameters);
                    copies.Add(caption.installedParameters);
                    copies.Add(caption.installedController);
                    foreach (var clip in caption.bindingClips) copies.Add(clip);
                    foreach (var item in caption.objects)
                    {
                        copies.Add(item.material);
                        copies.Add(item.mesh);
                    }
                }
                copies.Remap();
                var workingControls = copies.Get(controls);
                var workingCaption = copies.Get(caption);
                folder = CreateFolder("Edit-");
                copies.Save(folder);
                copies.RemapScene(avatar.gameObject, action);
                if (workingCaption != null)
                    XrtCaptionInstaller.UpdatePlacement(avatar, workingCaption);
                edit(workingControls, workingCaption);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(undo);
                copies.Commit();
                complete = true;
            }
            catch
            {
                // Restore scene references before disposing their failed copies.
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undo);
                throw;
            }
            finally
            {
                copies.Dispose();
                if (!complete && folder != null && AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            }
        }

        internal static string CreateFolder(string prefix)
        {
            const string root = "Assets/XRT-Avatar-Plugin";
            if (!AssetDatabase.IsValidFolder(root)) AssetDatabase.CreateFolder("Assets", "XRT-Avatar-Plugin");
            string name = prefix + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder(root, name);
            return root + "/" + name;
        }

        private static void AddMenu(AssetCopies copies, VRCExpressionsMenu menu, HashSet<VRCExpressionsMenu> visited)
        {
            if (menu == null || !visited.Add(menu)) return;
            copies.Add(menu);
            if (menu.controls != null)
                foreach (var control in menu.controls) AddMenu(copies, control?.subMenu, visited);
        }
    }
}
