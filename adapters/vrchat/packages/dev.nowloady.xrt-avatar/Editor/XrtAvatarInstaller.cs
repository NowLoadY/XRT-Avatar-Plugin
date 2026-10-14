using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace XrtAvatarPlugin.Editor
{
    internal static class XrtAvatarInstaller
    {
        private const string OutputRoot = "Assets/XRT-Avatar-Plugin";
        private const string MenuIconGuid = "8be9a42c601f4fd6a61294b074e7d3a1";
        private const int MaxMenuControls = 8;
        private const int MaxExpressionParameters = 8192;
        private static readonly string[] ReservedParameters =
        {
            XrtAvatarProtocol.Enabled, XrtAvatarProtocol.Status, XrtAvatarProtocol.VersionParameter
        };

        internal sealed class InstallationStatus
        {
            internal string Title;
            internal string Detail;
            internal bool CanInstall;
            internal bool CanRemove;
            internal MessageType Severity = MessageType.None;
        }

        internal static InstallationStatus Inspect(VRCAvatarDescriptor avatar)
        {
            if (avatar == null)
                return new InstallationStatus
                {
                    Title = "No avatar selected",
                    Detail = "Drag your avatar from the Hierarchy into the Avatar field."
                };
            try
            {
                string ownerId = ValidateAvatar(avatar);
                XrtAvatarInstallationRecord record = FindRecord(ownerId);
                if (record != null)
                {
                    ValidateRecord(record);
                    ValidateOwnedAssets(record);
                    if (UsesInstalledExpressions(avatar, record))
                        return new InstallationStatus
                        {
                            Title = "Added",
                            Detail = "XRT > Translation is ready on this avatar. " +
                                (avatar.gameObject.scene.isDirty ? "Save the scene before uploading." :
                                    "Upload the avatar with the VRChat SDK when ready."),
                            CanRemove = true
                        };
                    if (!UsesOriginalExpressions(avatar, record))
                        throw ExternalChanges(record);
                    ValidateOriginalSourceUnchanged(record);
                    return new InstallationStatus
                    {
                        Title = "Not added",
                        Detail = "Your original menu is restored. You can add translation again.",
                        CanInstall = true
                    };
                }
                ValidateSourceAssets(avatar);
                return new InstallationStatus
                {
                    Title = "Not added",
                    Detail = avatar.expressionsMenu.controls != null &&
                        avatar.expressionsMenu.controls.Count == MaxMenuControls
                        ? "Your menu is full. Its controls will stay together under Avatar, beside the new XRT menu."
                        : "Adds XRT > Translation. Your existing controls are kept.",
                    CanInstall = true
                };
            }
            catch (Exception exception)
            {
                return new InstallationStatus
                {
                    Title = "Check avatar",
                    Detail = exception is InvalidOperationException ? exception.Message :
                        "Could not read this avatar's setup. Check the setup guide for help.",
                    Severity = MessageType.Warning
                };
            }
        }

        internal static string Install(VRCAvatarDescriptor avatar)
        {
            string ownerId = ValidateAvatar(avatar);
            XrtAvatarInstallationRecord existing = FindRecord(ownerId);
            if (existing != null)
            {
                ValidateRecord(existing);
                ValidateOwnedAssets(existing);
                if (UsesInstalledExpressions(avatar, existing))
                    return "Translation menu is already added.";
                if (!UsesOriginalExpressions(avatar, existing))
                    throw ExternalChanges(existing);
                ValidateOriginalSourceUnchanged(existing);

                // Recover a previous installation whose scene assignment was not saved.
                ApplyExpressions(avatar, existing.installedMenu, existing.installedParameters, true);
                return "Translation menu added again. Save the scene before uploading.";
            }

            ValidateSourceAssets(avatar);
            Texture2D menuIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(MenuIconGuid));
            if (menuIcon == null)
                throw new InvalidOperationException("The translation menu icon is missing. Reimport the XRT package, then try again.");
            var createdPaths = new List<string>();
            var transientObjects = new List<Object>();
            VRCExpressionsMenu originalMenu = avatar.expressionsMenu;
            VRCExpressionParameters originalParameters = avatar.expressionParameters;
            bool originalCustomExpressions = avatar.customExpressions;
            bool descriptorTouched = false;
            try
            {
                EnsureFolder("Assets", "XRT-Avatar-Plugin", createdPaths);
                string folderName = Guid.NewGuid().ToString("N");
                string folder = EnsureFolder(OutputRoot, folderName, createdPaths);

                var submenu = NewAsset<VRCExpressionsMenu>(transientObjects);
                submenu.name = "XRT";
                submenu.controls = new List<VRCExpressionsMenu.Control>
                {
                    new VRCExpressionsMenu.Control
                    {
                        name = "Translation",
                        icon = menuIcon,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRCExpressionsMenu.Control.Parameter { name = XrtAvatarProtocol.Enabled },
                        value = 1
                    }
                };
                bool wrapOriginalMenu = originalMenu.controls != null &&
                    originalMenu.controls.Count == MaxMenuControls;
                var menu = wrapOriginalMenu
                    ? NewAsset<VRCExpressionsMenu>(transientObjects)
                    : CloneOrCreate(originalMenu, transientObjects);
                menu.name = "XRT Root";
                if (menu.controls == null)
                    menu.controls = new List<VRCExpressionsMenu.Control>();
                if (wrapOriginalMenu)
                    menu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = "Avatar",
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = originalMenu,
                        parameter = new VRCExpressionsMenu.Control.Parameter { name = string.Empty }
                    });
                menu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "XRT",
                    icon = menuIcon,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = submenu,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = string.Empty }
                });
                var parameters = CloneOrCreate(originalParameters, transientObjects);
                parameters.name = "XRT Parameters";
                var entries = new List<VRCExpressionParameters.Parameter>(
                    originalParameters == null ? Array.Empty<VRCExpressionParameters.Parameter>() :
                    parameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>());
                entries.Add(Parameter(XrtAvatarProtocol.Enabled, VRCExpressionParameters.ValueType.Bool, 0));
                entries.Add(Parameter(XrtAvatarProtocol.Status, VRCExpressionParameters.ValueType.Int, 0));
                entries.Add(Parameter(XrtAvatarProtocol.VersionParameter, VRCExpressionParameters.ValueType.Int,
                    XrtAvatarProtocol.Version));
                parameters.parameters = entries.ToArray();
                Persist(parameters, folder + "/Parameters.asset", createdPaths);
                // The SDK's menu inspector uses this reference for parameter selection.
                // Set it before saving so opening the inspector does not mutate owned assets.
                menu.Parameters = parameters;
                submenu.Parameters = parameters;
                Persist(submenu, folder + "/XRT.asset", createdPaths);
                Persist(menu, folder + "/Expressions.asset", createdPaths);

                var record = NewAsset<XrtAvatarInstallationRecord>(transientObjects);
                record.name = "XRT Installation";
                record.avatarGlobalId = ownerId;
                record.originalCustomExpressions = originalCustomExpressions;
                record.hadOriginalMenu = originalMenu != null;
                record.hadOriginalParameters = originalParameters != null;
                record.originalMenu = originalMenu;
                record.originalParameters = originalParameters;
                record.originalMenuSourceHash = SourceHash(originalMenu);
                record.originalParametersSourceHash = SourceHash(originalParameters);
                record.installedMenu = menu;
                record.installedParameters = parameters;
                record.xrtMenu = submenu;
                record.installedMenuHash = AssetHash(menu);
                record.installedParametersHash = AssetHash(parameters);
                record.xrtMenuHash = AssetHash(submenu);
                Persist(record, folder + "/Installation.asset", createdPaths);

                descriptorTouched = true;
                ApplyExpressions(avatar, menu, parameters, true);
                return "Added XRT > Translation. Save the scene, then upload this avatar with the VRChat SDK.";
            }
            catch (Exception failure)
            {
                var rollbackErrors = new List<string>();
                if (descriptorTouched)
                {
                    try
                    {
                        ApplyExpressions(avatar, originalMenu, originalParameters, originalCustomExpressions);
                    }
                    catch (Exception rollbackFailure)
                    {
                        rollbackErrors.Add("Could not restore descriptor: " + rollbackFailure.Message);
                    }
                }
                // If descriptor restoration failed, preserve recovery assets and the record.
                if (rollbackErrors.Count == 0)
                    RollBackAssets(createdPaths, rollbackErrors);
                if (rollbackErrors.Count > 0)
                    throw new InvalidOperationException(failure.Message + "\nRecovery required: " +
                        string.Join("\n", rollbackErrors), failure);
                throw;
            }
            finally
            {
                foreach (Object value in transientObjects)
                    if (value != null && !EditorUtility.IsPersistent(value))
                        Object.DestroyImmediate(value);
            }
        }

        internal static string Restore(VRCAvatarDescriptor avatar)
        {
            string ownerId = ValidateAvatar(avatar);
            XrtAvatarInstallationRecord record = FindRecord(ownerId);
            if (record == null)
                return "Translation menu is not added to this avatar.";
            ValidateRecord(record);
            bool alreadyRestored = UsesOriginalExpressions(avatar, record);
            if (!alreadyRestored)
            {
                if (!UsesInstalledExpressions(avatar, record))
                    throw ExternalChanges(record);
                ValidateOwnedAssets(record);
            }

            // Retain the record even after restoration. The scene and asset database are saved
            // separately; an "uninstalled" flag could otherwise strand an unsaved scene.
            if (!alreadyRestored)
                ApplyExpressions(avatar, record.originalMenu, record.originalParameters,
                    record.originalCustomExpressions);
            return "Translation menu removed. Save the scene, then upload this avatar again.";
        }

        private static string ValidateAvatar(VRCAvatarDescriptor avatar)
        {
            if (avatar == null)
                throw new InvalidOperationException("Choose a VRChat avatar from the Hierarchy.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before changing this avatar.");
            if (EditorUtility.IsPersistent(avatar) || PrefabStageUtility.GetPrefabStage(avatar.gameObject) != null)
                throw new InvalidOperationException("Open the avatar in a scene and leave Prefab Mode.");
            if (!avatar.gameObject.scene.IsValid() || string.IsNullOrEmpty(avatar.gameObject.scene.path))
                throw new InvalidOperationException("Save the avatar's scene before installing or restoring.");
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(avatar);
            if (id.identifierType != 2 || id.targetObjectId == 0)
                throw new InvalidOperationException("Save the scene, then select the avatar again.");
            return id.ToString();
        }

        private static void ValidateSourceAssets(VRCAvatarDescriptor avatar)
        {
            if (!avatar.customExpressions)
                throw new InvalidOperationException("Set up this avatar's Expressions menu first. Open the setup guide with the help icon for the steps.");
            if (avatar.expressionsMenu == null || avatar.expressionParameters == null)
                throw new InvalidOperationException("Assign Menu and Parameters in VRC Avatar Descriptor > Expressions, then save them. The setup guide has the steps.");
            RequirePersistentSource(avatar.expressionsMenu);
            RequirePersistentSource(avatar.expressionParameters);
            var controls = avatar.expressionsMenu != null ? avatar.expressionsMenu.controls : null;
            if (controls != null && controls.Count > MaxMenuControls)
                throw new InvalidOperationException("The existing root menu exceeds VRChat's limit of 8 controls. " +
                    "Move extra controls into submenus before adding XRT.");
            if (controls != null && controls.Any(control => control != null && control.name == "XRT"))
                throw new InvalidOperationException("This avatar already has an XRT menu. Follow the recovery steps in the setup guide before adding it again.");
            var parameters = avatar.expressionParameters != null ? avatar.expressionParameters.parameters : null;
            if (parameters == null)
                return;
            if (parameters.Length + ReservedParameters.Length > MaxExpressionParameters)
                throw new InvalidOperationException("This avatar has no room for the translation controls. Free space in its Expressions Parameters before adding XRT.");
            foreach (var parameter in parameters)
                if (parameter != null && ReservedParameters.Contains(parameter.name))
                    throw new InvalidOperationException("This avatar already has XRT controls. Follow the recovery steps in the setup guide before adding them again.");
        }

        private static void RequirePersistentSource(Object asset)
        {
            if (asset != null && (!EditorUtility.IsPersistent(asset) || EditorUtility.IsDirty(asset)))
                throw new InvalidOperationException("Save the existing Expressions assets before installing.");
        }

        private static void ValidateOriginalSourceUnchanged(XrtAvatarInstallationRecord record)
        {
            RequirePersistentSource(record.originalMenu);
            RequirePersistentSource(record.originalParameters);
            if (SourceHash(record.originalMenu) != record.originalMenuSourceHash ||
                SourceHash(record.originalParameters) != record.originalParametersSourceHash)
                throw new InvalidOperationException("Your original Expressions have changed. Follow the recovery steps in the setup guide before adding XRT again.");
        }

        private static string SourceHash(Object asset)
        {
            return asset == null ? string.Empty :
                AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(asset)).ToString();
        }

        private static XrtAvatarInstallationRecord FindRecord(string ownerId)
        {
            XrtAvatarInstallationRecord result = null;
            foreach (string guid in AssetDatabase.FindAssets("t:XrtAvatarInstallationRecord", new[] { "Assets" }))
            {
                var record = AssetDatabase.LoadAssetAtPath<XrtAvatarInstallationRecord>(AssetDatabase.GUIDToAssetPath(guid));
                if (record == null || record.avatarGlobalId != ownerId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException("More than one XRT setup was found for this avatar. Follow the recovery steps in the setup guide.");
                result = record;
            }
            return result;
        }

        private static void ValidateRecord(XrtAvatarInstallationRecord record)
        {
            if (record.recordVersion != 1)
                throw new InvalidOperationException("This setup uses a different XRT version. Update the plugin before continuing.");
            if ((record.hadOriginalMenu && record.originalMenu == null) ||
                (record.hadOriginalParameters && record.originalParameters == null))
                throw new InvalidOperationException("An original Expressions asset is missing. Restore it from your project backup before continuing.");
        }

        private static bool UsesInstalledExpressions(VRCAvatarDescriptor avatar, XrtAvatarInstallationRecord record)
        {
            return record.installedMenu != null && record.installedParameters != null && avatar.customExpressions &&
                avatar.expressionsMenu == record.installedMenu && avatar.expressionParameters == record.installedParameters;
        }

        private static bool UsesOriginalExpressions(VRCAvatarDescriptor avatar, XrtAvatarInstallationRecord record)
        {
            return avatar.customExpressions == record.originalCustomExpressions &&
                avatar.expressionsMenu == record.originalMenu && avatar.expressionParameters == record.originalParameters;
        }

        private static void ValidateOwnedAssets(XrtAvatarInstallationRecord record)
        {
            if (!Unchanged(record.installedMenu, record.installedMenuHash) ||
                !Unchanged(record.installedParameters, record.installedParametersHash) ||
                !Unchanged(record.xrtMenu, record.xrtMenuHash))
                throw ExternalChanges(record);
        }

        private static bool Unchanged(Object asset, string hash)
        {
            // Unsaved edits must also prevent restoration; a disk hash alone would miss them.
            return asset != null && !EditorUtility.IsDirty(asset) && !string.IsNullOrEmpty(hash) &&
                string.Equals(hash, AssetHash(asset), StringComparison.Ordinal);
        }

        private static InvalidOperationException ExternalChanges(XrtAvatarInstallationRecord record)
        {
            return new InvalidOperationException("This avatar's XRT setup has been edited. Keep your changes and follow the recovery steps in the setup guide.");
        }

        private static VRCExpressionParameters.Parameter Parameter(string name, VRCExpressionParameters.ValueType type,
            float defaultValue)
        {
            return new VRCExpressionParameters.Parameter
            {
                name = name, valueType = type, defaultValue = defaultValue, saved = false, networkSynced = false
            };
        }

        private static T NewAsset<T>(List<Object> transientObjects) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            transientObjects.Add(asset);
            return asset;
        }

        private static T CloneOrCreate<T>(T original, List<Object> transientObjects) where T : ScriptableObject
        {
            if (original == null)
                return NewAsset<T>(transientObjects);
            T clone = Object.Instantiate(original);
            clone.hideFlags = HideFlags.None;
            transientObjects.Add(clone);
            return clone;
        }

        private static void Persist(Object asset, string path, List<string> createdPaths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(AbsoluteAssetPath(path)))
                throw new InvalidOperationException("Refusing to overwrite an existing asset: " + path);
            createdPaths.Add(path);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            if (!EditorUtility.IsPersistent(asset))
                throw new IOException("Could not create asset: " + path);
        }

        private static string EnsureFolder(string parent, string name, List<string> createdPaths)
        {
            string path = parent + "/" + name;
            if (AssetDatabase.IsValidFolder(path))
                return path;
            if (Directory.Exists(AbsoluteAssetPath(path)) || File.Exists(AbsoluteAssetPath(path)))
                throw new IOException("An unimported path already exists: " + path);
            createdPaths.Add(path);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name)))
                throw new IOException("Could not create folder: " + path);
            return path;
        }

        private static string AssetHash(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(AbsoluteAssetPath(path))))
                    .Replace("-", string.Empty);
        }

        private static string AbsoluteAssetPath(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new IOException("Expected an asset inside the project's Assets folder: " + path);
            return Path.Combine(Application.dataPath, path.Substring("Assets/".Length));
        }

        private static void ApplyExpressions(VRCAvatarDescriptor avatar, VRCExpressionsMenu menu,
            VRCExpressionParameters parameters, bool custom)
        {
            VRCExpressionsMenu previousMenu = avatar.expressionsMenu;
            VRCExpressionParameters previousParameters = avatar.expressionParameters;
            bool previousCustom = avatar.customExpressions;
            try
            {
                avatar.expressionsMenu = menu;
                avatar.expressionParameters = parameters;
                avatar.customExpressions = custom;
                RecordDescriptorChange(avatar);
            }
            catch
            {
                avatar.expressionsMenu = previousMenu;
                avatar.expressionParameters = previousParameters;
                avatar.customExpressions = previousCustom;
                RecordDescriptorChange(avatar);
                throw;
            }
        }

        private static void RecordDescriptorChange(VRCAvatarDescriptor avatar)
        {
            EditorUtility.SetDirty(avatar);
            if (PrefabUtility.IsPartOfPrefabInstance(avatar))
                PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
            EditorSceneManager.MarkSceneDirty(avatar.gameObject.scene);
        }

        private static void RollBackAssets(List<string> createdPaths, List<string> errors)
        {
            for (int i = createdPaths.Count - 1; i >= 0; i--)
            {
                string path = createdPaths[i];
                try
                {
                    string absolutePath = AbsoluteAssetPath(path);
                    if (Directory.Exists(absolutePath) && Directory.EnumerateFileSystemEntries(absolutePath).Any())
                        continue; // Never delete anything another operation put in a created folder.
                    if ((File.Exists(absolutePath) || Directory.Exists(absolutePath)) && !AssetDatabase.DeleteAsset(path))
                        errors.Add("Could not remove newly created asset: " + path);
                }
                catch (Exception failure)
                {
                    errors.Add(path + ": " + failure.Message);
                }
            }
        }
    }
}
