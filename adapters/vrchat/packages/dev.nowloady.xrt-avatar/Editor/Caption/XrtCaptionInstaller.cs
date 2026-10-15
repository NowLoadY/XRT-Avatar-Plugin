using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using XrtAvatarPlugin.Presentation;
using Object = UnityEngine.Object;

namespace XrtAvatarPlugin.Editor
{
    internal static class XrtCaptionInstaller
    {
        internal enum Preset { Horizontal, Portrait, Board }
        private const string Root = "Assets/XRT-Avatar-Plugin";
        internal sealed class Status
        {
            internal string Title;
            internal string Detail;
            internal bool CanAdd;
            internal bool CanRemove;
            internal int ByteCount;
            internal XrtCaptionInstallationRecord Record;
            internal MeshRenderer Renderer;
        }

        internal static Status Inspect(VRCAvatarDescriptor avatar)
        {
            try
            {
                ValidateAvatar(avatar);
                var record = FindRecord(avatar);
                if (record != null)
                {
                    ValidateInstalled(avatar, record);
                    var renderers = FindRenderers(avatar, record);
                    return new Status
                    {
                        Title = "Added", CanRemove = true, Record = record,
                        ByteCount = record.byteCount, Renderer = renderers.FirstOrDefault(),
                        Detail = renderers.Length == 0 ? "Caption object is missing. Remove, then add it again." :
                            "Move, rotate and scale the object in the Scene view."
                    };
                }
                var menu = XrtAvatarInstaller.Inspect(avatar);
                if (!menu.CanInstall && !menu.CanRemove)
                    throw new InvalidOperationException(menu.Detail);
                ValidateController(avatar);
                int bytes = AvailableBytes(avatar.expressionParameters, avatar.customExpressions);
                return new Status
                {
                    Title = "Not added", CanAdd = true, ByteCount = bytes,
                    Detail = "128 characters · " + ((bytes + 1) * 8) + " synced bits"
                };
            }
            catch (Exception failure)
            {
                return new Status { Title = "Check avatar", Detail = failure.Message };
            }
        }

        internal static void Install(VRCAvatarDescriptor avatar, Transform parent, Preset preset, string preview)
        {
            ValidateAvatar(avatar);
            if (FindRecord(avatar) != null)
                throw new InvalidOperationException("A caption object is already added to this avatar.");
            if (parent == null) parent = avatar.transform;
            if (parent != avatar.transform && !parent.IsChildOf(avatar.transform))
                throw new InvalidOperationException("Choose an attachment inside this avatar.");
            var sourceController = ValidateController(avatar);
            AvailableBytes(avatar.expressionParameters, avatar.customExpressions);
            if (CaptionAssets.LoadShader() == null)
                throw new InvalidOperationException("Reimport the XRT package to restore its caption assets.");

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add XRT Caption");
            var paths = new List<string>();
            var originals = Snapshot(avatar);
            GameObject board = null;
            try
            {
                XrtAvatarInstaller.Install(avatar);
                var sourceParameters = avatar.expressionParameters;
                int bytes = AvailableBytes(sourceParameters, true);
                int columns = preset == Preset.Portrait ? XrtCaptionProtocol.PortraitColumns : XrtCaptionProtocol.HorizontalColumns;
                EnsureRoot(paths);
                string folder = Root + "/Caption-" + Guid.NewGuid().ToString("N");
                AssetDatabase.CreateFolder(Root, Path.GetFileName(folder));
                paths.Add(folder);

                var material = CaptionAssets.CreateMaterial();
                material.name = "XRT Caption";
                CaptionAuthoring.ConfigureMaterial(material, preset.ToString());
                CaptionAuthoring.SetPreview(material, preview, columns);
                Save(material, folder + "/Caption.mat", paths);
                var mesh = CaptionAssets.CreateMesh();
                Save(mesh, folder + "/Caption.asset", paths);
                board = new GameObject("XRT Caption");
                Undo.RegisterCreatedObjectUndo(board, "Add XRT Caption");
                Undo.SetTransformParent(board.transform, parent, "Attach XRT Caption");
                GameObjectUtility.EnsureUniqueNameForSibling(board);
                board.transform.localPosition = parent == avatar.transform
                    ? new Vector3(0, Mathf.Max(0.5f, avatar.ViewPosition.y * 0.65f), 0.3f)
                    : Vector3.zero;
                board.transform.localRotation = Quaternion.identity;
                board.transform.localScale = CaptionAuthoring.Dimensions(columns);
                board.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = board.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                string rendererPath = AnimationUtility.CalculateTransformPath(board.transform, avatar.transform);
                var controller = CloneController(sourceController, folder + "/FX.controller", paths);
                XrtCaptionAnimator.Build(controller, rendererPath, bytes, columns);
                var parameters = Object.Instantiate(sourceParameters);
                parameters.name = "XRT Caption Parameters";
                var entries = new List<VRCExpressionParameters.Parameter>(parameters.parameters ??
                    Array.Empty<VRCExpressionParameters.Parameter>());
                entries.Add(Parameter(XrtCaptionProtocol.VersionParameter, XrtCaptionProtocol.Version, false));
                entries.Add(Parameter(XrtCaptionProtocol.ColumnsParameter, columns, false));
                entries.Add(Parameter(XrtCaptionProtocol.BytesParameter, bytes, false));
                entries.Add(Parameter(XrtCaptionProtocol.Pointer, XrtCaptionProtocol.ClearPointer, true));
                for (int i = 0; i < bytes; i++) entries.Add(Parameter(XrtCaptionProtocol.BytePrefix + i, 0, true));
                parameters.parameters = entries.ToArray();
                Save(parameters, folder + "/Parameters.asset", paths);

                var record = ScriptableObject.CreateInstance<XrtCaptionInstallationRecord>();
                record.name = "XRT Caption Installation";
                record.avatarGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(avatar).ToString();
                record.objectName = rendererPath;
                record.rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString();
                record.originalCustomExpressions = avatar.customExpressions;
                record.originalParameters = sourceParameters;
                record.originalCustomizeAnimationLayers = avatar.customizeAnimationLayers;
                record.originalBaseLayers = CloneLayers(avatar.baseAnimationLayers);
                record.originalSpecialLayers = CloneLayers(avatar.specialAnimationLayers);
                record.installedSpecialLayers = avatar.customizeAnimationLayers
                    ? CloneLayers(avatar.specialAnimationLayers) : DefaultLayers(avatar.specialAnimationLayers);
                record.installedController = controller;
                record.installedParameters = parameters;
                record.material = material;
                record.columns = columns;
                record.byteCount = bytes;
                record.parametersHash = Hash(parameters);
                record.controllerHash = Hash(controller);
                record.installedBaseLayers = WithFx(avatar.baseAnimationLayers, controller, avatar.customizeAnimationLayers);
                Save(record, folder + "/Installation.asset", paths);
                Undo.RecordObject(avatar, "Add XRT Caption");
                avatar.expressionParameters = parameters;
                avatar.customExpressions = true;
                avatar.baseAnimationLayers = CloneLayers(record.installedBaseLayers);
                avatar.specialAnimationLayers = CloneLayers(record.installedSpecialLayers);
                avatar.customizeAnimationLayers = true;
                Changed(avatar);
                Undo.CollapseUndoOperations(undo);
                Selection.activeGameObject = board;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                RestoreSnapshot(avatar, originals);
                if (board != null) Object.DestroyImmediate(board);
                for (int i = paths.Count - 1; i >= 0; i--)
                    if (!AssetDatabase.IsValidFolder(paths[i]) ||
                        !Directory.EnumerateFileSystemEntries(AssetFilePath(paths[i])).Any())
                        AssetDatabase.DeleteAsset(paths[i]);
                throw;
            }
        }

        internal static void Remove(VRCAvatarDescriptor avatar)
        {
            ValidateAvatar(avatar);
            var record = FindRecord(avatar);
            if (record == null) return;
            ValidateInstalled(avatar, record);
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove XRT Caption");
            Undo.RecordObject(avatar, "Remove XRT Caption");
            avatar.expressionParameters = record.originalParameters;
            avatar.customExpressions = record.originalCustomExpressions;
            avatar.baseAnimationLayers = CloneLayers(record.originalBaseLayers);
            avatar.specialAnimationLayers = CloneLayers(record.originalSpecialLayers);
            avatar.customizeAnimationLayers = record.originalCustomizeAnimationLayers;
            foreach (var renderer in FindRenderers(avatar, record))
            {
                var gameObject = renderer.gameObject;
                if (gameObject.transform.childCount == 0 && gameObject.GetComponents<Component>().Length == 3)
                    Undo.DestroyObjectImmediate(gameObject);
                else
                    Undo.DestroyObjectImmediate(renderer);
            }
            // Asset records are retained for Undo/Redo and previously saved scenes.
            Changed(avatar);
            Undo.CollapseUndoOperations(undo);
        }

        internal static VRCExpressionParameters ParametersBeforeCaption(VRCAvatarDescriptor avatar)
        {
            var record = FindRecord(avatar);
            return record != null && avatar.expressionParameters == record.installedParameters
                ? record.originalParameters : avatar.expressionParameters;
        }

        internal static bool IsInstalled(VRCAvatarDescriptor avatar) => FindRecord(avatar) != null;

        internal static void RefreshPlacement(VRCAvatarDescriptor avatar)
        {
            var record = FindRecord(avatar);
            if (record == null) return;
            var renderer = FindRenderers(avatar, record).FirstOrDefault();
            if (renderer == null) return;
            string path = AnimationUtility.CalculateTransformPath(renderer.transform, avatar.transform);
            if (path == record.objectName) return;
            ValidateInstalled(avatar, record);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(record.installedController)))
            {
                if (!(asset is AnimationClip clip)) continue;
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (binding.path != record.objectName || binding.type != typeof(MeshRenderer) ||
                        !binding.propertyName.StartsWith("material._", StringComparison.Ordinal)) continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    var moved = binding;
                    moved.path = path;
                    AnimationUtility.SetEditorCurve(clip, moved, curve);
                }
            }
            EditorUtility.SetDirty(record.installedController);
            AssetDatabase.SaveAssetIfDirty(record.installedController);
            record.objectName = path;
            record.controllerHash = Hash(record.installedController);
            EditorUtility.SetDirty(record);
            AssetDatabase.SaveAssetIfDirty(record);
        }

        internal static void RefreshBuildPlacement(VRCAvatarDescriptor target)
        {
            if (target == null || target.expressionParameters == null) return;
            foreach (string guid in AssetDatabase.FindAssets("t:XrtCaptionInstallationRecord", new[] { "Assets" }))
            {
                var record = AssetDatabase.LoadAssetAtPath<XrtCaptionInstallationRecord>(AssetDatabase.GUIDToAssetPath(guid));
                if (record == null || record.installedParameters != target.expressionParameters) continue;
                if (!GlobalObjectId.TryParse(record.avatarGlobalId, out var identifier) ||
                    !(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(identifier) is VRCAvatarDescriptor source))
                    throw new InvalidOperationException("Reopen the avatar's saved scene before uploading its caption object.");
                RefreshPlacement(source);
                return;
            }
        }

        private static int AvailableBytes(VRCExpressionParameters parameters, bool custom)
        {
            var entries = custom && parameters != null
                ? parameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>()
                : VRC.SDK3.Editor.VRCExpressionParametersEditor.GetDefaultParameters();
            if (parameters != null && EditorUtility.IsDirty(parameters))
                throw new InvalidOperationException("Save the Expressions Parameters asset first.");
            if (entries.Any(p => p != null && p.name != null && p.name.StartsWith(XrtCaptionAnimator.Prefix)))
                throw new InvalidOperationException("This avatar already contains caption parameters.");
            int used = entries.Where(p => p != null && p.networkSynced && !string.IsNullOrEmpty(p.name))
                .Sum(p => p.valueType == VRCExpressionParameters.ValueType.Bool ? 1 : 8);
            foreach (int bytes in XrtCaptionProtocol.BusByteCounts)
                if (used + (bytes + 1) * 8 <= 256 && entries.Length + bytes + 4 <= 8192) return bytes;
            throw new InvalidOperationException("Caption objects need at least 24 free synced bits in Expressions Parameters.");
        }

        private static void ValidateAvatar(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) throw new InvalidOperationException("Choose an avatar from the Hierarchy.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before changing caption objects.");
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                throw new InvalidOperationException("Switch the project to Windows to add caption objects.");
            if (EditorUtility.IsPersistent(avatar) || PrefabStageUtility.GetPrefabStage(avatar.gameObject) != null)
                throw new InvalidOperationException("Open the avatar in a scene and leave Prefab Mode.");
            if (string.IsNullOrEmpty(avatar.gameObject.scene.path) ||
                GlobalObjectId.GetGlobalObjectIdSlow(avatar).targetObjectId == 0)
                throw new InvalidOperationException("Save the avatar's scene first.");
        }

        private static RuntimeAnimatorController ValidateController(VRCAvatarDescriptor avatar)
        {
            var layer = (avatar.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                .FirstOrDefault(value => value.type == VRCAvatarDescriptor.AnimLayerType.FX);
            var controller = avatar.customizeAnimationLayers && !layer.isDefault ? layer.animatorController : null;
            if (controller == null)
            {
                const string defaultGuid = "d40be620cf6c698439a2f0a5144919fe";
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(defaultGuid));
                if (controller == null)
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                        "Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/Animation/Controllers/vrc_AvatarV3FaceLayer.controller");
                if (controller == null)
                    throw new InvalidOperationException("Reimport the VRChat Avatars SDK to restore its default FX controller.");
            }
            var current = controller;
            var visited = new HashSet<RuntimeAnimatorController>();
            while (current != null)
            {
                if (!visited.Add(current)) throw new InvalidOperationException("The FX controller contains a circular override.");
                if (!EditorUtility.IsPersistent(current) ||
                    AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(current)).Any(EditorUtility.IsDirty))
                    throw new InvalidOperationException("Save the FX controller before adding caption objects.");
                if (current is AnimatorOverrideController)
                    throw new InvalidOperationException("Use an Animator Controller for the avatar FX layer before adding caption objects.");
                else if (current is AnimatorController) break;
                else throw new InvalidOperationException("Assign an Animator Controller to the FX layer first.");
            }
            return controller;
        }

        private static AnimatorController CloneController(RuntimeAnimatorController source, string path, List<string> paths)
        {
            if (source == null)
            {
                paths.Add(path);
                return AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path))
                throw new InvalidOperationException("Could not copy the avatar's FX controller.");
            paths.Add(path);
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }

        private static XrtCaptionInstallationRecord FindRecord(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return null;
            string owner = GlobalObjectId.GetGlobalObjectIdSlow(avatar).ToString();
            XrtCaptionInstallationRecord result = null;
            foreach (string guid in AssetDatabase.FindAssets("t:XrtCaptionInstallationRecord", new[] { "Assets" }))
            {
                var record = AssetDatabase.LoadAssetAtPath<XrtCaptionInstallationRecord>(AssetDatabase.GUIDToAssetPath(guid));
                if (record == null || record.avatarGlobalId != owner) continue;
                if (avatar.expressionParameters != record.installedParameters &&
                    !(avatar.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                        .Any(layer => layer.animatorController == record.installedController)) continue;
                if (result != null) throw new InvalidOperationException("More than one caption setup is assigned to this avatar.");
                result = record;
            }
            return result;
        }

        private static void ValidateInstalled(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record)
        {
            if (record.recordVersion != 1 || record.originalParameters == null)
                throw new InvalidOperationException("Restore this caption setup's original assets from your project backup.");
            if (avatar.expressionParameters != record.installedParameters || !avatar.customExpressions ||
                !avatar.customizeAnimationLayers || !LayersEqual(avatar.baseAnimationLayers, record.installedBaseLayers) ||
                !LayersEqual(avatar.specialAnimationLayers, record.installedSpecialLayers) ||
                !Unchanged(record.installedParameters, record.parametersHash) ||
                !Unchanged(record.installedController, record.controllerHash))
                throw new InvalidOperationException("The caption setup's FX or Parameters have changed. Keep your changes and restore the previous assets before removing it.");
        }

        private static bool LayersEqual(VRCAvatarDescriptor.CustomAnimLayer[] first,
            VRCAvatarDescriptor.CustomAnimLayer[] second) =>
            first != null && second != null && first.SequenceEqual(second);

        private static VRCAvatarDescriptor.CustomAnimLayer[] WithFx(
            VRCAvatarDescriptor.CustomAnimLayer[] original, AnimatorController controller, bool custom)
        {
            var layers = new List<VRCAvatarDescriptor.CustomAnimLayer>(original ??
                Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>());
            if (!custom)
                layers = new List<VRCAvatarDescriptor.CustomAnimLayer>(DefaultLayers(layers.ToArray()));
            int index = layers.FindIndex(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX);
            var fx = index >= 0 ? layers[index] : new VRCAvatarDescriptor.CustomAnimLayer
            { type = VRCAvatarDescriptor.AnimLayerType.FX };
            fx.animatorController = controller;
            fx.isDefault = false;
            fx.isEnabled = true;
            if (index >= 0) layers[index] = fx;
            else layers.Add(fx);
            return layers.ToArray();
        }

        private static VRCAvatarDescriptor.CustomAnimLayer[] DefaultLayers(VRCAvatarDescriptor.CustomAnimLayer[] original)
        {
            var layers = CloneLayers(original);
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i].isDefault = true;
                layers[i].isEnabled = true;
                layers[i].animatorController = null;
                layers[i].mask = null;
            }
            return layers;
        }

        private static VRCAvatarDescriptor.CustomAnimLayer[] CloneLayers(VRCAvatarDescriptor.CustomAnimLayer[] layers) =>
            layers == null ? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>() :
                (VRCAvatarDescriptor.CustomAnimLayer[])layers.Clone();

        private sealed class DescriptorSnapshot
        {
            internal VRCExpressionsMenu Menu;
            internal VRCExpressionParameters Parameters;
            internal bool CustomExpressions, CustomLayers;
            internal VRCAvatarDescriptor.CustomAnimLayer[] Layers, SpecialLayers;
        }
        private static DescriptorSnapshot Snapshot(VRCAvatarDescriptor avatar) => new DescriptorSnapshot
        {
            Menu = avatar.expressionsMenu, Parameters = avatar.expressionParameters,
            CustomExpressions = avatar.customExpressions, CustomLayers = avatar.customizeAnimationLayers,
            Layers = CloneLayers(avatar.baseAnimationLayers), SpecialLayers = CloneLayers(avatar.specialAnimationLayers)
        };
        private static void RestoreSnapshot(VRCAvatarDescriptor avatar, DescriptorSnapshot state)
        {
            avatar.expressionsMenu = state.Menu;
            avatar.expressionParameters = state.Parameters;
            avatar.customExpressions = state.CustomExpressions;
            avatar.customizeAnimationLayers = state.CustomLayers;
            avatar.baseAnimationLayers = state.Layers;
            avatar.specialAnimationLayers = state.SpecialLayers;
            Changed(avatar);
        }
        private static void Changed(VRCAvatarDescriptor avatar)
        {
            EditorUtility.SetDirty(avatar);
            if (PrefabUtility.IsPartOfPrefabInstance(avatar)) PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
            EditorSceneManager.MarkSceneDirty(avatar.gameObject.scene);
        }
        private static MeshRenderer[] FindRenderers(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record)
        {
            if (GlobalObjectId.TryParse(record.rendererGlobalId, out var identifier))
            {
                var renderer = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(identifier) as MeshRenderer;
                if (renderer != null && renderer.transform.IsChildOf(avatar.transform))
                    return new[] { renderer };
            }
            return Array.Empty<MeshRenderer>();
        }

        private static VRCExpressionParameters.Parameter Parameter(string name, int value, bool synced) =>
            new VRCExpressionParameters.Parameter
            {
                name = name, valueType = VRCExpressionParameters.ValueType.Int,
                defaultValue = value, saved = false, networkSynced = synced
            };
        private static void EnsureRoot(List<string> paths)
        {
            if (AssetDatabase.IsValidFolder(Root)) return;
            AssetDatabase.CreateFolder("Assets", "XRT-Avatar-Plugin");
            paths.Add(Root);
        }
        private static void Save(Object asset, string path, List<string> paths)
        {
            paths.Add(path);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
        private static string AssetFilePath(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new IOException("Expected an asset in this project: " + path);
            return Path.Combine(Application.dataPath, path.Substring("Assets/".Length));
        }
        private static string Hash(Object asset)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(AssetFilePath(AssetDatabase.GetAssetPath(asset)))));
        }
        private static bool Unchanged(Object asset, string hash) => asset != null &&
            AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset)).All(value => !EditorUtility.IsDirty(value)) &&
            !string.IsNullOrEmpty(hash) && Hash(asset) == hash;
    }
}
