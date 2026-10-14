using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace XrtAvatarPlugin.ChilloutVR.Editor
{
    internal static class XrtChilloutInstaller
    {
        private const string Root = "Assets/XRT-Avatar-Plugin/ChilloutVR";
        private static readonly string[] Parameters =
            { XrtChilloutProtocol.Enabled, XrtChilloutProtocol.Status, XrtChilloutProtocol.VersionParameter };

        [MenuItem("Tools/XRT-Avatar-Plugin/ChilloutVR/Install on selected avatar")]
        private static void InstallSelected() => Run(Install);

        [MenuItem("Tools/XRT-Avatar-Plugin/ChilloutVR/Remove from selected avatar")]
        private static void RemoveSelected() => Run(Remove);

        private static void Run(Func<CVRAvatar, string> action)
        {
            try
            {
                CVRAvatar avatar = Selection.activeGameObject == null ? null :
                    Selection.activeGameObject.GetComponentInParent<CVRAvatar>();
                EditorUtility.DisplayDialog("XRT-Avatar-Plugin", action(avatar), "OK");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorUtility.DisplayDialog("XRT-Avatar-Plugin", error.Message, "OK");
            }
        }

        internal static string Install(CVRAvatar avatar)
        {
            string id = ValidateAvatar(avatar);
            XrtChilloutInstallationRecord existing = FindInstalled(id);
            if (existing != null)
            {
                ValidateInstallation(avatar, existing);
                return "XRT Translation is already installed.";
            }

            RuntimeAnimatorController source = avatar.overrides;
            if (source == null)
                source = avatar.GetComponent<Animator>()?.runtimeAnimatorController;
            if (source == null)
                source = avatar.avatarSettings?.baseController;
            AnimatorController controller = BaseController(source);
            if (controller == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(controller)))
                throw new InvalidOperationException(
                    "Configure the avatar's animator first. In the CCK, create and attach its Animator Override Controller, then install XRT.");
            if (controller.parameters.Any(p => Parameters.Contains(p.name)) ||
                (avatar.avatarSettings?.settings?.Any(e => e != null && Parameters.Contains(e.machineName)) ?? false))
                throw new InvalidOperationException("The avatar already contains XRT parameters. Remove the existing integration before installing.");

            string folder = null;
            AnimatorOverrideController originalOverride = avatar.overrides;
            CVRAdvancedAvatarSettings originalSettings = avatar.avatarSettings;
            bool originalUsesSettings = avatar.avatarUsesAdvancedSettings;
            CVRAdvancedSettingsEntry entry = null;
            bool touched = false;
            try
            {
                EnsureFolder("Assets/XRT-Avatar-Plugin");
                EnsureFolder(Root);
                folder = Root + "/" + Guid.NewGuid().ToString("N");
                EnsureFolder(folder);
                string controllerPath = folder + "/Avatar.controller";
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(controller), controllerPath))
                    throw new IOException("Could not copy the avatar controller.");
                AnimatorController installedController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (installedController == null) throw new IOException("Could not load the copied controller.");
                installedController.AddParameter(new AnimatorControllerParameter
                    { name = XrtChilloutProtocol.Enabled, type = AnimatorControllerParameterType.Bool, defaultBool = false });
                installedController.AddParameter(new AnimatorControllerParameter
                    { name = XrtChilloutProtocol.Status, type = AnimatorControllerParameterType.Int, defaultInt = 0 });
                installedController.AddParameter(new AnimatorControllerParameter
                    { name = XrtChilloutProtocol.VersionParameter, type = AnimatorControllerParameterType.Int, defaultInt = XrtChilloutProtocol.Version });
                EditorUtility.SetDirty(installedController);

                var installedOverride = new AnimatorOverrideController(installedController);
                installedOverride.name = "XRT Avatar Overrides";
                ApplyOverrideChain(source, installedOverride, controller, installedController);
                AssetDatabase.CreateAsset(installedOverride, folder + "/Overrides.overrideController");

                entry = new CVRAdvancedSettingsEntry
                {
                    name = "XRT Translation",
                    machineName = XrtChilloutProtocol.Enabled,
                    unlinkNameFromMachineName = true,
                    type = CVRAdvancedSettingsEntry.SettingsType.Toggle,
                    toggleSettings = new CVRAdvancesAvatarSettingGameObjectToggle
                    {
                        defaultValue = false,
                        usedType = CVRAdvancesAvatarSettingBase.ParameterType.Bool
                    }
                };
                var record = ScriptableObject.CreateInstance<XrtChilloutInstallationRecord>();
                record.avatarGlobalId = id;
                record.installed = false;
                record.hadOriginalOverride = originalOverride != null;
                record.originalOverride = originalOverride;
                record.originalUsesAdvancedSettings = originalUsesSettings;
                record.originalHadSettings = originalSettings != null;
                record.installedOverride = installedOverride;
                record.installedController = installedController;
                AssetDatabase.CreateAsset(record, folder + "/Installation.asset");
                AssetDatabase.SaveAssets();
                record.controllerHash = Hash(installedController);
                record.overrideHash = Hash(installedOverride);
                EditorUtility.SetDirty(record);
                AssetDatabase.SaveAssets();

                Undo.RecordObjects(new Object[] { avatar, record }, "Install XRT Translation");
                record.installed = true;
                EditorUtility.SetDirty(record);
                touched = true;
                if (avatar.avatarSettings == null) avatar.avatarSettings = new CVRAdvancedAvatarSettings();
                if (avatar.avatarSettings.settings == null)
                    avatar.avatarSettings.settings = new List<CVRAdvancedSettingsEntry>();
                avatar.avatarSettings.settings.Add(entry);
                avatar.avatarUsesAdvancedSettings = true;
                avatar.overrides = installedOverride;
                MarkChanged(avatar);
                AssetDatabase.SaveAssets();
                Selection.activeObject = record;
                return "Installed XRT Translation in Advanced Avatar Settings. Save the scene, then upload with the CCK.\nAssets: " + folder;
            }
            catch
            {
                if (touched)
                {
                    avatar.overrides = originalOverride;
                    avatar.avatarUsesAdvancedSettings = originalUsesSettings;
                    avatar.avatarSettings?.settings?.Remove(entry);
                    avatar.avatarSettings = originalSettings;
                    MarkChanged(avatar);
                }
                if (folder != null && AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
                throw;
            }
        }

        internal static string Remove(CVRAvatar avatar)
        {
            string id = ValidateAvatar(avatar);
            XrtChilloutInstallationRecord record = FindInstalled(id);
            if (record == null) return "No XRT installation is registered for this avatar.";
            ValidateInstallation(avatar, record);
            Undo.RecordObjects(new Object[] { avatar, record }, "Remove XRT Translation");
            avatar.overrides = record.originalOverride;
            avatar.avatarSettings.settings.RemoveAll(e => e != null && e.machineName == XrtChilloutProtocol.Enabled);
            if (avatar.avatarSettings.settings.Count == 0)
            {
                avatar.avatarUsesAdvancedSettings = record.originalUsesAdvancedSettings;
                if (!record.originalHadSettings && avatar.avatarSettings.baseController == null &&
                    avatar.avatarSettings.baseOverrideController == null)
                    avatar.avatarSettings = null;
            }
            record.installed = false;
            EditorUtility.SetDirty(record);
            MarkChanged(avatar);
            AssetDatabase.SaveAssets();
            return "Removed XRT Translation. Save the scene before uploading. Generated assets are retained for recovery.";
        }

        private static string ValidateAvatar(CVRAvatar avatar)
        {
            if (avatar == null) throw new InvalidOperationException("Select an avatar with a CVRAvatar component.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.IsPersistent(avatar) ||
                !avatar.gameObject.scene.IsValid() || string.IsNullOrEmpty(avatar.gameObject.scene.path) ||
                PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Select an avatar in a saved scene outside Play mode and Prefab mode.");
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(avatar);
            if (id.identifierType != 2 || id.targetObjectId == 0)
                throw new InvalidOperationException("Save the scene so this avatar has a persistent scene identity.");
            return id.ToString();
        }

        private static AnimatorController BaseController(RuntimeAnimatorController source)
        {
            var visited = new HashSet<RuntimeAnimatorController>();
            while (source is AnimatorOverrideController overrides)
            {
                if (!visited.Add(source)) throw new InvalidOperationException("The avatar has a circular override controller reference.");
                source = overrides.runtimeAnimatorController;
            }
            return source as AnimatorController;
        }

        private static void ApplyOverrideChain(RuntimeAnimatorController source, AnimatorOverrideController destination,
            AnimatorController originalController, AnimatorController copiedController)
        {
            var chain = new Stack<AnimatorOverrideController>();
            while (source is AnimatorOverrideController overrides)
            {
                chain.Push(overrides);
                source = overrides.runtimeAnimatorController;
            }
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            while (chain.Count > 0)
            {
                pairs.Clear();
                chain.Pop().GetOverrides(pairs);
                // A null override inherits the value from the next controller.
                destination.ApplyOverrides(pairs.Where(p => p.Value != null).Select(p =>
                    new KeyValuePair<AnimationClip, AnimationClip>(
                        RemapEmbeddedClip(p.Key, originalController, copiedController),
                        RemapEmbeddedClip(p.Value, originalController, copiedController))).ToList());
            }
        }

        private static AnimationClip RemapEmbeddedClip(AnimationClip clip,
            AnimatorController original, AnimatorController copied)
        {
            if (clip == null || AssetDatabase.GetAssetPath(clip) != AssetDatabase.GetAssetPath(original))
                return clip;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string _, out long localId))
                throw new InvalidOperationException("Could not identify an embedded animation clip.");
            // CopyAsset preserves subasset local IDs while assigning a new asset GUID.
            foreach (AnimationClip candidate in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(copied)).OfType<AnimationClip>())
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out string _, out long candidateId) && candidateId == localId)
                    return candidate;
            throw new InvalidOperationException("Could not preserve an embedded animation override.");
        }

        private static XrtChilloutInstallationRecord FindInstalled(string id)
        {
            var records = AssetDatabase.FindAssets("t:XrtChilloutInstallationRecord")
                .Select(g => AssetDatabase.LoadAssetAtPath<XrtChilloutInstallationRecord>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r => r != null && r.installed && r.avatarGlobalId == id).ToArray();
            if (records.Length > 1) throw new InvalidOperationException("Multiple XRT installation records refer to this avatar.");
            return records.FirstOrDefault();
        }

        private static void ValidateInstallation(CVRAvatar avatar, XrtChilloutInstallationRecord record)
        {
            var entries = avatar.avatarSettings?.settings?.Where(e => e != null && e.machineName == XrtChilloutProtocol.Enabled).ToArray();
            if (record.recordVersion != 1 || record.installedController == null || record.installedOverride == null ||
                (record.hadOriginalOverride && record.originalOverride == null) ||
                avatar.overrides != record.installedOverride || entries == null || entries.Length != 1 ||
                !IsControlEntry(entries[0]) ||
                Hash(record.installedController) != record.controllerHash || Hash(record.installedOverride) != record.overrideHash)
                throw new InvalidOperationException(
                    "The installed XRT menu or controller has changed. Preserve your changes and restore the recorded installation before removing or reinstalling it.");
        }

        private static bool IsControlEntry(CVRAdvancedSettingsEntry entry)
        {
            var toggle = entry.toggleSettings;
            return entry.type == CVRAdvancedSettingsEntry.SettingsType.Toggle && toggle != null &&
                toggle.usedType == CVRAdvancesAvatarSettingBase.ParameterType.Bool && !toggle.defaultValue &&
                !toggle.useAnimationClip && toggle.animationClip == null && toggle.offAnimationClip == null &&
                (toggle.gameObjectTargets == null || toggle.gameObjectTargets.Count == 0);
        }

        private static string Hash(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return string.Empty;
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent) || string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path))))
                throw new IOException("Could not create " + path);
        }

        private static void MarkChanged(CVRAvatar avatar)
        {
            EditorUtility.SetDirty(avatar);
            PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
            EditorSceneManager.MarkSceneDirty(avatar.gameObject.scene);
        }
    }
}
