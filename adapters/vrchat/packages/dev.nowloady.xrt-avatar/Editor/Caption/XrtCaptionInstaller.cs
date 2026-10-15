using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            internal MeshRenderer[] Renderers = Array.Empty<MeshRenderer>();
        }

        internal static Status Inspect(VRCAvatarDescriptor avatar)
        {
            try
            {
                ValidateAvatar(avatar);
                var record = FindRecord(avatar);
                if (record != null)
                {
                    var renderers = FindRenderers(avatar, record);
                    return new Status
                    {
                        Title = "Added", CanAdd = ReceiverAssigned(avatar, record), CanRemove = true, Record = record,
                        ByteCount = record.byteCount, Renderers = renderers,
                        Detail = !ReceiverAssigned(avatar, record)
                            ? "Restore the caption FX controller and parameters before adding more text objects."
                            : renderers.Length == 0 ? "Add a text object or remove this caption setup."
                            : "Move, rotate and scale each object in the Scene view."
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

        internal static void Install(VRCAvatarDescriptor avatar, Transform parent, Preset preset)
        {
            ValidateAvatar(avatar);
            var existing = FindRecord(avatar);
            if (parent == null) parent = avatar.transform;
            if (parent != avatar.transform && !parent.IsChildOf(avatar.transform))
                throw new InvalidOperationException("Choose an attachment inside this avatar.");
            if (existing != null)
            {
                AddObject(avatar, parent, preset, existing);
                return;
            }
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

                var renderer = CreateObject(avatar, parent, preset, columns, folder, paths);
                board = renderer.gameObject;
                string rendererPath = AnimationUtility.CalculateTransformPath(board.transform, avatar.transform);
                var controller = CloneController(sourceController, folder + "/FX.controller", paths);
                XrtCaptionAnimator.Build(controller, rendererPath, bytes, columns,
                    out AnimatorStateMachine[] ownedLayers, out AnimationClip[] bindingClips);
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
                record.objects.Add(ObjectRecord(renderer, avatar));
                record.originalParameters = sourceParameters;
                var specialLayers = avatar.customizeAnimationLayers
                    ? CloneLayers(avatar.specialAnimationLayers) : DefaultLayers(avatar.specialAnimationLayers);
                record.installedController = controller;
                record.installedParameters = parameters;
                record.ownedLayers = ownedLayers;
                record.bindingClips = bindingClips;
                record.expressionParameters = ExpressionNames(bytes);
                record.animatorParameters = AnimatorNames(bytes);
                record.columns = columns;
                record.byteCount = bytes;
                var baseLayers = WithFx(avatar.baseAnimationLayers, controller, avatar.customizeAnimationLayers);
                Save(record, folder + "/Installation.asset", paths);
                Undo.RecordObject(avatar, "Add XRT Caption");
                avatar.expressionParameters = parameters;
                avatar.customExpressions = true;
                avatar.baseAnimationLayers = baseLayers;
                avatar.specialAnimationLayers = specialLayers;
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

        private static MeshRenderer CreateObject(VRCAvatarDescriptor avatar, Transform parent,
            Preset preset, int columns, string folder, List<string> paths)
        {
            var material = CaptionAssets.CreateMaterial();
            CaptionAuthoring.ConfigureMaterial(material, preset.ToString(), columns);
            CaptionAuthoring.SetPreview(material, string.Empty, columns);
            Save(material, folder + "/Caption.mat", paths);
            var mesh = CaptionAssets.CreateMesh();
            Save(mesh, folder + "/Caption.asset", paths);
            var text = new GameObject("XRT Caption");
            Undo.RegisterCreatedObjectUndo(text, "Add XRT Caption");
            Undo.SetTransformParent(text.transform, parent, "Attach XRT Caption");
            GameObjectUtility.EnsureUniqueNameForSibling(text);
            text.transform.localPosition = parent == avatar.transform
                ? new Vector3(0, Mathf.Max(0.5f, avatar.ViewPosition.y * 0.65f), 0.3f) : Vector3.zero;
            text.transform.localRotation = Quaternion.identity;
            text.transform.localScale = CaptionAuthoring.Dimensions(columns);
            text.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = text.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return renderer;
        }

        private static XrtCaptionInstallationRecord.TextObject ObjectRecord(MeshRenderer renderer,
            VRCAvatarDescriptor avatar) => new XrtCaptionInstallationRecord.TextObject
        {
            rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString(),
            path = AnimationUtility.CalculateTransformPath(renderer.transform, avatar.transform),
            mesh = renderer.GetComponent<MeshFilter>().sharedMesh,
            material = renderer.sharedMaterial
        };

        private static void AddObject(VRCAvatarDescriptor avatar, Transform parent, Preset preset,
            XrtCaptionInstallationRecord record)
        {
            ValidateInstalled(avatar, record);
            RefreshPlacement(avatar);
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add XRT Caption");
            var paths = new List<string>();
            try
            {
                string root = Path.GetDirectoryName(AssetDatabase.GetAssetPath(record)).Replace('\\', '/');
                string folder = root + "/Text-" + Guid.NewGuid().ToString("N");
                AssetDatabase.CreateFolder(root, Path.GetFileName(folder));
                paths.Add(folder);
                var renderer = CreateObject(avatar, parent, preset, record.columns, folder, paths);
                var item = ObjectRecord(renderer, avatar);
                Undo.RecordObject(record, "Add XRT Caption");
                AddBindings(record, item.path);
                record.objects.Add(item);
                EditorUtility.SetDirty(record.installedController);
                AssetDatabase.SaveAssetIfDirty(record.installedController);
                EditorUtility.SetDirty(record);
                AssetDatabase.SaveAssetIfDirty(record);
                Changed(avatar);
                Undo.CollapseUndoOperations(undo);
                Selection.activeGameObject = renderer.gameObject;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                foreach (string path in paths.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
                throw;
            }
        }

        internal static void Remove(VRCAvatarDescriptor avatar, MeshRenderer selected = null)
        {
            ValidateAvatar(avatar);
            var record = FindRecord(avatar);
            if (record == null) return;
            RefreshPlacement(avatar);
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            const string action = "Remove XRT Caption";
            Undo.SetCurrentGroupName(action);
            Undo.RecordObject(record, action);
            var targets = record.objects.Where(item => {
                var renderer = FindRenderer(avatar, item);
                return renderer == null || selected == null || renderer == selected;
            }).ToArray();
            foreach (var item in targets)
            {
                RemoveBindings(record, item.path);
                var renderer = FindRenderer(avatar, item);
                if (renderer != null) CaptionAuthoring.RemoveObject(renderer, item.mesh, item.material);
                record.objects.Remove(item);
            }
            if (record.objects.Count == 0)
            {
                XrtAvatarInstaller.RemoveParameters(avatar.expressionParameters, record.expressionParameters, action);
                var controller = record.installedController;
                if (controller != null)
                {
                    Undo.RecordObject(controller, action);
                    controller.layers = controller.layers.Where(layer => !record.ownedLayers.Contains(layer.stateMachine)).ToArray();
                    controller.parameters = controller.parameters.Where(parameter => !record.animatorParameters.Contains(parameter.name)).ToArray();
                }
            }
            // Working copies and user edits remain assigned; assets stay for Undo.
            if (record.installedController != null)
            {
                EditorUtility.SetDirty(record.installedController);
                AssetDatabase.SaveAssetIfDirty(record.installedController);
            }
            EditorUtility.SetDirty(record);
            AssetDatabase.SaveAssetIfDirty(record);
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
            bool changed = false;
            foreach (var item in record.objects)
            {
                var renderer = FindRenderer(avatar, item);
                if (renderer == null) continue;
                string path = AnimationUtility.CalculateTransformPath(renderer.transform, avatar.transform);
                if (path != item.path)
                {
                    foreach (var clip in record.bindingClips.Where(clip => clip != null))
                    {
                        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        {
                            if (binding.path != item.path || !OwnsBinding(binding)) continue;
                            var curve = AnimationUtility.GetEditorCurve(clip, binding);
                            AnimationUtility.SetEditorCurve(clip, binding, null);
                            var moved = binding;
                            moved.path = path;
                            AnimationUtility.SetEditorCurve(clip, moved, curve);
                        }
                    }
                    item.path = path;
                    changed = true;
                }
                string id = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString();
                if (item.rendererGlobalId != id) { item.rendererGlobalId = id; changed = true; }
            }
            if (!changed) return;
            if (record.installedController != null)
            {
                EditorUtility.SetDirty(record.installedController);
                AssetDatabase.SaveAssetIfDirty(record.installedController);
            }
            EditorUtility.SetDirty(record);
            AssetDatabase.SaveAssetIfDirty(record);
        }

        private static bool OwnsBinding(EditorCurveBinding binding)
        {
            if (binding.type != typeof(MeshRenderer)) return false;
            if (binding.propertyName == "material._Columns") return true;
            const string prefix = "material._Byte";
            return binding.propertyName.StartsWith(prefix, StringComparison.Ordinal) &&
                int.TryParse(binding.propertyName.Substring(prefix.Length), out int index) &&
                index >= 0 && index < CaptionAssets.ByteCount && binding.propertyName == prefix + index;
        }

        private static void AddBindings(XrtCaptionInstallationRecord record, string path)
        {
            foreach (var clip in record.bindingClips.Where(clip => clip != null))
            {
                var binding = AnimationUtility.GetCurveBindings(clip).FirstOrDefault(candidate =>
                    OwnsBinding(candidate) && record.objects.Any(item => item.path == candidate.path));
                if (string.IsNullOrEmpty(binding.propertyName)) continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                binding.path = path;
                Undo.RecordObject(clip, "Add XRT Caption");
                AnimationUtility.SetEditorCurve(clip, binding, curve);
                EditorUtility.SetDirty(clip);
            }
        }

        private static void RemoveBindings(XrtCaptionInstallationRecord record, string path)
        {
            foreach (var clip in record.bindingClips.Where(clip => clip != null))
            {
                var bindings = AnimationUtility.GetCurveBindings(clip)
                    .Where(binding => binding.path == path && OwnsBinding(binding)).ToArray();
                if (bindings.Length == 0) continue;
                Undo.RecordObject(clip, "Remove XRT Caption");
                foreach (var binding in bindings) AnimationUtility.SetEditorCurve(clip, binding, null);
                EditorUtility.SetDirty(clip);
            }
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
                Migrate(record);
                bool assigned = (avatar.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                    .Any(layer => layer.animatorController == record.installedController);
                bool receiver = assigned && record.installedController != null && record.installedController.layers
                    .Any(layer => record.ownedLayers.Contains(layer.stateMachine));
                if (!receiver && !record.objects.Any(item => FindRenderer(avatar, item) != null)) continue;
                if (result != null) throw new InvalidOperationException("More than one caption setup is assigned to this avatar.");
                result = record;
            }
            return result;
        }

        private static bool ReceiverAssigned(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record)
        {
            if (record.recordVersion != 2 || record.installedController == null ||
                !avatar.customizeAnimationLayers || !avatar.customExpressions) return false;
            bool controller = (avatar.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                .Any(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX && !layer.isDefault &&
                    layer.animatorController == record.installedController);
            var parameters = avatar.expressionParameters?.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>();
            return controller && record.ownedLayers.Length == 2 &&
                record.ownedLayers.All(owned => record.installedController.layers.Any(layer => layer.stateMachine == owned)) &&
                record.expressionParameters.All(name => parameters.Any(parameter => parameter != null &&
                    parameter.name == name && parameter.valueType == VRCExpressionParameters.ValueType.Int)) &&
                record.animatorParameters.All(name => record.installedController.parameters.Any(parameter => parameter.name == name));
        }

        private static void ValidateInstalled(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record)
        {
            if (!ReceiverAssigned(avatar, record))
                throw new InvalidOperationException("Restore the caption FX controller and parameters before adding more text objects.");
        }

        private static string[] ExpressionNames(int bytes) => new[] {
            XrtCaptionProtocol.VersionParameter, XrtCaptionProtocol.ColumnsParameter,
            XrtCaptionProtocol.BytesParameter, XrtCaptionProtocol.Pointer
        }.Concat(Enumerable.Range(0, bytes).Select(index => XrtCaptionProtocol.BytePrefix + index)).ToArray();

        private static string[] AnimatorNames(int bytes) => ExpressionNames(bytes)
            .Concat(new[] { XrtCaptionProtocol.Prefix + "Weight" })
            .Concat(Enumerable.Range(0, CaptionAssets.ByteCount).Select(index => XrtCaptionProtocol.Prefix + "Cache" + index)).ToArray();

        private static void Migrate(XrtCaptionInstallationRecord record)
        {
            if (record.recordVersion != 1) return;
            record.objects = new List<XrtCaptionInstallationRecord.TextObject>();
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(record)).Replace('\\', '/');
            record.objects.Add(new XrtCaptionInstallationRecord.TextObject {
                rendererGlobalId = record.rendererGlobalId, path = record.objectName,
                material = record.material, mesh = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/Caption.asset")
            });
            var layers = record.installedController == null ? Array.Empty<AnimatorControllerLayer>() : record.installedController.layers;
            record.ownedLayers = layers.Where(layer => layer.stateMachine != null &&
                (layer.stateMachine.name == "XRT Caption · Receive" || layer.stateMachine.name == "XRT Caption · Display"))
                .Select(layer => layer.stateMachine).ToArray();
            record.bindingClips = record.ownedLayers.SelectMany(Clips).Distinct().Where(clip => clip != null &&
                AssetDatabase.GetAssetPath(clip) == AssetDatabase.GetAssetPath(record.installedController) &&
                AnimationUtility.GetCurveBindings(clip).Any(binding => binding.path == record.objectName && OwnsBinding(binding))).ToArray();
            record.expressionParameters = ExpressionNames(record.byteCount);
            record.animatorParameters = AnimatorNames(record.byteCount);
            record.recordVersion = 2;
            // Hydrate legacy ownership without saving merely because a window inspected it.
        }

        private static IEnumerable<AnimationClip> Clips(AnimatorStateMachine machine) =>
            machine.states.SelectMany(state => Clips(state.state.motion))
                .Concat(machine.stateMachines.SelectMany(child => Clips(child.stateMachine)));
        private static IEnumerable<AnimationClip> Clips(Motion motion)
        {
            if (motion is AnimationClip clip) return new[] { clip };
            if (motion is BlendTree tree) return tree.children.SelectMany(child => Clips(child.motion));
            return Array.Empty<AnimationClip>();
        }

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
        private static MeshRenderer[] FindRenderers(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record) =>
            record.objects.Select(item => FindRenderer(avatar, item)).Where(renderer => renderer != null).ToArray();

        private static MeshRenderer FindRenderer(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord.TextObject item)
        {
            if (GlobalObjectId.TryParse(item.rendererGlobalId, out var identifier))
            {
                var renderer = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(identifier) as MeshRenderer;
                if (renderer != null && renderer.transform.IsChildOf(avatar.transform)) return renderer;
            }
            // A newly created scene object may not have a persistent ID yet.
            var candidate = string.IsNullOrEmpty(item.path) ? null : avatar.transform.Find(item.path)?.GetComponent<MeshRenderer>();
            if (candidate != null && candidate.sharedMaterial == item.material &&
                candidate.GetComponent<MeshFilter>()?.sharedMesh == item.mesh) return candidate;
            if (item.mesh == null || item.material == null) return null;
            var matches = avatar.GetComponentsInChildren<MeshRenderer>(true).Where(renderer =>
                renderer.GetComponent<MeshFilter>()?.sharedMesh == item.mesh &&
                renderer.sharedMaterials.Length == 1 && renderer.sharedMaterial == item.material).Take(2).ToArray();
            return matches.Length == 1 ? matches[0] : null;
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
    }
}
