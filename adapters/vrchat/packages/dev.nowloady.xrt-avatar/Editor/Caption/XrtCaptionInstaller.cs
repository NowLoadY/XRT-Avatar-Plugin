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
                    bool receiverAssigned = ReceiverAssigned(avatar, record);
                    return new Status
                    {
                        Title = "Added", CanAdd = receiverAssigned, CanRemove = true, Record = record,
                        ByteCount = record.byteCount, Renderers = renderers,
                        Detail = !receiverAssigned
                            ? "Restore the caption FX controller and parameters before adding more text objects."
                            : renderers.Length == 0 ? "Add a text object or remove this caption setup."
                            : "Move, rotate and scale each object in the Scene view."
                    };
                }
                var menu = XrtAvatarInstaller.Inspect(avatar);
                if (!menu.CanInstall && !menu.CanRemove)
                    throw new InvalidOperationException(menu.Detail);
                ValidateController(avatar);
                int bytes = AvailableBytes(avatar);
                return new Status
                {
                    Title = "Not added", CanAdd = true, ByteCount = bytes,
                    Detail = "128 characters · " + ((bytes + 1) * 8 + 1) + " synced bits"
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
                Upgrade(avatar);
                existing = FindRecord(avatar);
                AddObject(avatar, parent, preset, existing);
                return;
            }
            var sourceController = ValidateController(avatar);
            AvailableBytes(avatar);
            if (CaptionAssets.LoadShader() == null)
                throw new InvalidOperationException("Reimport the XRT package to restore its caption assets.");

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add XRT Caption");
            string folder = null;
            var originals = Snapshot(avatar);
            GameObject board = null;
            try
            {
                XrtAvatarInstaller.Install(avatar);
                XrtAvatarAssets.Edit(avatar, "Add XRT Caption", (controls, unused) =>
                {
                    var sourceParameters = avatar.expressionParameters;
                    int bytes = AvailableBytes(avatar);
                    int columns = preset == Preset.Portrait ? XrtCaptionProtocol.PortraitColumns : XrtCaptionProtocol.HorizontalColumns;
                    folder = XrtAvatarAssets.CreateFolder("Caption-");

                    var renderer = CreateObject(avatar, parent, preset, columns, folder);
                    board = renderer.gameObject;
                    string rendererPath = AnimationUtility.CalculateTransformPath(board.transform, avatar.transform);
                    var controller = CloneController(sourceController, folder + "/FX.controller");
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
                    entries.Add(VisibilityParameter());
                    parameters.parameters = entries.ToArray();
                    Save(parameters, folder + "/Parameters.asset");

                    var record = ScriptableObject.CreateInstance<XrtCaptionInstallationRecord>();
                    record.name = "XRT Caption Installation";
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
                    Save(record, folder + "/Installation.asset");
                    Undo.RecordObject(avatar, "Add XRT Caption");
                    avatar.expressionParameters = parameters;
                    avatar.customExpressions = true;
                    avatar.baseAnimationLayers = baseLayers;
                    avatar.specialAnimationLayers = specialLayers;
                    avatar.customizeAnimationLayers = true;
                    AddVisibilityMenu(avatar, controls?.xrtMenu);
                    Changed(avatar);
                });
                Undo.CollapseUndoOperations(undo);
                Selection.activeGameObject = board;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                RestoreSnapshot(avatar, originals);
                if (board != null) Object.DestroyImmediate(board);
                if (folder != null) AssetDatabase.DeleteAsset(folder);
                throw;
            }
        }

        private static MeshRenderer CreateObject(VRCAvatarDescriptor avatar, Transform parent,
            Preset preset, int columns, string folder)
        {
            var material = CaptionAssets.CreateMaterial();
            CaptionAuthoring.ConfigureMaterial(material, preset.ToString(), columns);
            CaptionAuthoring.SetPreview(material, CaptionAuthoring.DefaultPreviewText, columns);
            Save(material, folder + "/Caption.mat");
            var mesh = CaptionAssets.CreateMesh();
            Save(mesh, folder + "/Caption.asset");
            var text = new GameObject("XRT Caption");
            Undo.RegisterCreatedObjectUndo(text, "Add XRT Caption");
            Undo.SetTransformParent(text.transform, parent, "Attach XRT Caption");
            GameObjectUtility.EnsureUniqueNameForSibling(text);
            text.transform.localPosition = parent == avatar.transform
                ? new Vector3(0, Mathf.Max(0.5f, avatar.ViewPosition.y * 0.65f), 0.3f) : Vector3.zero;
            text.transform.localRotation = Quaternion.identity;
            text.transform.localScale = CaptionAuthoring.Dimensions(preset.ToString());
            Undo.AddComponent<MeshFilter>(text).sharedMesh = mesh;
            var renderer = Undo.AddComponent<MeshRenderer>(text);
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
            XrtAvatarAssets.Edit(avatar, "Add XRT Caption", (controls, working) =>
            {
                string root = Path.GetDirectoryName(AssetDatabase.GetAssetPath(working)).Replace('\\', '/');
                string folder = root + "/Text-" + Guid.NewGuid().ToString("N");
                AssetDatabase.CreateFolder(root, Path.GetFileName(folder));
                var renderer = CreateObject(avatar, parent, preset, working.columns, folder);
                var item = ObjectRecord(renderer, avatar);
                Undo.RecordObject(working, "Add XRT Caption");
                AddBindings(working, item.path);
                working.objects.Add(item);
                SaveRecord(working);
                Changed(avatar);
                Selection.activeGameObject = renderer.gameObject;
            });
        }

        internal static void Remove(VRCAvatarDescriptor avatar, MeshRenderer selected = null)
        {
            ValidateAvatar(avatar);
            var record = FindRecord(avatar);
            if (record == null) return;
            const string action = "Remove XRT Caption";
            XrtAvatarAssets.Edit(avatar, action, (controls, working) =>
            {
                Undo.RecordObject(working, action);
                var targets = working.objects.Where(item => {
                    var renderer = FindRenderer(avatar, item);
                    return renderer == null || selected == null || renderer == selected;
                }).ToArray();
                foreach (var item in targets)
                {
                    RemoveBindings(working, item.path);
                    var renderer = FindRenderer(avatar, item);
                    if (renderer != null) CaptionAuthoring.RemoveObject(renderer, item.mesh, item.material);
                    working.objects.Remove(item);
                }
                if (working.objects.Count == 0)
                {
                    if (controls?.xrtMenu != null)
                    {
                        Undo.RecordObject(controls.xrtMenu, action);
                        controls.xrtMenu.controls?.RemoveAll(control => control?.parameter?.name == XrtCaptionAnimator.Visible);
                        EditorUtility.SetDirty(controls.xrtMenu);
                        AssetDatabase.SaveAssetIfDirty(controls.xrtMenu);
                    }
                    XrtAvatarInstaller.RemoveParameters(avatar.expressionParameters, working.expressionParameters, action);
                    if (controls != null)
                    {
                        controls.installedParameters = avatar.expressionParameters;
                        EditorUtility.SetDirty(controls);
                        AssetDatabase.SaveAssetIfDirty(controls);
                    }
                    var controller = working.installedController;
                    if (controller != null)
                    {
                        Undo.RecordObject(controller, action);
                        controller.layers = controller.layers.Where(layer => !working.ownedLayers.Contains(layer.stateMachine)).ToArray();
                        controller.parameters = controller.parameters.Where(parameter => !working.animatorParameters.Contains(parameter.name)).ToArray();
                    }
                }
                SaveRecord(working);
                Changed(avatar);
            });
        }

        internal static VRCExpressionParameters ParametersBeforeCaption(VRCAvatarDescriptor avatar)
        {
            var record = FindRecord(avatar);
            return record != null && avatar.expressionParameters == record.installedParameters
                ? record.originalParameters : avatar.expressionParameters;
        }

        internal static bool IsInstalled(VRCAvatarDescriptor avatar) => FindRecord(avatar) != null;

        internal static bool NeedsUpdate(VRCAvatarDescriptor avatar)
        {
            var record = FindRecord(avatar);
            return record != null && record.objects.Count > 0 && record.recordVersion < 4;
        }

        internal static void Upgrade(VRCAvatarDescriptor avatar)
        {
            if (!NeedsUpdate(avatar)) return;
            ValidateAvatar(avatar);
            ValidateInstalled(avatar, FindRecord(avatar));
            XrtAvatarAssets.Edit(avatar, "Update XRT Captions", (controls, working) =>
            {
                EnsureVisibility(avatar, working, controls?.xrtMenu);
                EnsureBuffering(working);
                Changed(avatar);
            });
        }

        private static void EnsureBuffering(XrtCaptionInstallationRecord record, bool authoring = true)
        {
            if (record.recordVersion >= 4) return;
            XrtCaptionAnimator.AddBuffering(record.installedController, record.ownedLayers[0], record.byteCount);
            record.animatorParameters = record.animatorParameters.Concat(XrtCaptionAnimator.BufferParameters).ToArray();
            record.recordVersion = 4;
            EditorUtility.SetDirty(record.installedController);
            AssetDatabase.SaveAssetIfDirty(record.installedController);
            if (authoring) SaveRecord(record);
        }

        private static void EnsureVisibility(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record,
            VRCExpressionsMenu menu, bool authoring = true)
        {
            if (record.recordVersion >= 3) return;
            var parameters = avatar.expressionParameters;
            var entries = parameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>();
            var budget = XrtParameterBudget.Inspect(avatar, entries,
                record.expressionParameters.Concat(record.animatorParameters));
            if (budget.Bits >= 256 || budget.Count >= 8192)
                throw new InvalidOperationException("Free one synced bit in Expressions Parameters to add the Caption panel switch.");
            if (entries.Any(parameter => parameter?.name == XrtCaptionAnimator.Visible))
                throw new InvalidOperationException("A parameter named " + XrtCaptionAnimator.Visible + " already exists.");
            if (authoring) Undo.RecordObject(parameters, "Update XRT Captions");
            parameters.parameters = entries.Concat(new[] { VisibilityParameter() }).ToArray();
            var visibility = XrtCaptionAnimator.AddVisibility(record.installedController,
                record.objects.Select(item => item.path), out AnimationClip[] clips);
            record.ownedLayers = record.ownedLayers.Concat(new[] { visibility }).ToArray();
            record.bindingClips = record.bindingClips.Concat(clips).ToArray();
            record.expressionParameters = record.expressionParameters.Concat(new[] { XrtCaptionAnimator.Visible }).ToArray();
            record.animatorParameters = record.animatorParameters.Concat(new[] { XrtCaptionAnimator.Visible }).ToArray();
            record.recordVersion = 3;
            AddVisibilityMenu(avatar, menu, authoring);
            EditorUtility.SetDirty(parameters);
            AssetDatabase.SaveAssetIfDirty(parameters);
            foreach (var clip in clips) AssetDatabase.SaveAssetIfDirty(clip);
            EditorUtility.SetDirty(record.installedController);
            AssetDatabase.SaveAssetIfDirty(record.installedController);
            if (authoring) SaveRecord(record);
        }

        private static VRCExpressionParameters.Parameter VisibilityParameter() =>
            new VRCExpressionParameters.Parameter {
                name = XrtCaptionAnimator.Visible, valueType = VRCExpressionParameters.ValueType.Bool,
                defaultValue = 0, saved = true, networkSynced = true
            };

        private static void AddVisibilityMenu(VRCAvatarDescriptor avatar, VRCExpressionsMenu menu, bool authoring = true)
        {
            if (menu == null) throw new InvalidOperationException("Restore this avatar's XRT translation menu first.");
            bool present = menu.controls?.Any(control => control?.parameter?.name == XrtCaptionAnimator.Visible) == true;
            if (!present && menu.controls != null && menu.controls.Count >= 8)
                throw new InvalidOperationException("Free one slot in the XRT submenu to add the Caption panel switch.");
            if (authoring) Undo.RecordObject(menu, "Add XRT Captions switch");
            if (menu.controls == null) menu.controls = new List<VRCExpressionsMenu.Control>();
            if (!present) menu.controls.Add(new VRCExpressionsMenu.Control {
                name = "Caption panel", type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = XrtCaptionAnimator.Visible }, value = 1
            });
            menu.Parameters = avatar.expressionParameters;
            EditorUtility.SetDirty(menu);
            AssetDatabase.SaveAssetIfDirty(menu);
            if (avatar.expressionsMenu != null)
            {
                if (authoring) Undo.RecordObject(avatar.expressionsMenu, "Add XRT Captions switch");
                avatar.expressionsMenu.Parameters = avatar.expressionParameters;
                EditorUtility.SetDirty(avatar.expressionsMenu);
                AssetDatabase.SaveAssetIfDirty(avatar.expressionsMenu);
            }
        }

        // Apply all path moves together: swapping two object names must not overwrite
        // either curve before the other one has been read.
        internal static void UpdatePlacement(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record,
            bool authoring = true)
        {
            var renderers = record.objects.Select(item => FindRenderer(avatar, item)).ToArray();
            var paths = record.objects.Select((item, index) => renderers[index] == null ? item.path :
                AnimationUtility.CalculateTransformPath(renderers[index].transform, avatar.transform)).ToArray();
            var moves = record.objects.Select((item, index) => new { item.path, next = paths[index] })
                .Where(move => move.path != move.next).ToArray();
            foreach (var clip in record.bindingClips.Where(clip => clip != null))
            {
                var curves = AnimationUtility.GetCurveBindings(clip).Where(binding => OwnsBinding(binding) &&
                    moves.Any(move => move.path == binding.path)).Select(binding => new {
                        binding, curve = AnimationUtility.GetEditorCurve(clip, binding),
                        path = moves.First(move => move.path == binding.path).next
                    }).ToArray();
                foreach (var curve in curves) AnimationUtility.SetEditorCurve(clip, curve.binding, null);
                foreach (var curve in curves)
                {
                    var binding = curve.binding;
                    binding.path = curve.path;
                    AnimationUtility.SetEditorCurve(clip, binding, curve.curve);
                }
                if (curves.Length > 0) EditorUtility.SetDirty(clip);
            }
            for (int i = 0; i < record.objects.Count; i++)
            {
                record.objects[i].path = paths[i];
                if (authoring && renderers[i] != null)
                    record.objects[i].rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(renderers[i]).ToString();
            }
            if (authoring) SaveRecord(record);
        }

        private static void SaveRecord(XrtCaptionInstallationRecord record)
        {
            foreach (var clip in record.bindingClips.Where(clip => clip != null)) AssetDatabase.SaveAssetIfDirty(clip);
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
            if (binding.propertyName == "m_Enabled") return true;
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

        internal static void RefreshBuildPlacement(VRCAvatarDescriptor target,
            XrtCaptionInstallationRecord record, string folder)
        {
            using (var copies = new AssetCopies())
            {
                // Build metadata is temporary and must not become another discoverable installation.
                var working = Object.Instantiate(record);
                try
                {
                    working.installedController = copies.Add(record.installedController);
                    working.bindingClips = record.bindingClips.Select(clip => copies.Add(clip)).ToArray();
                    working.installedParameters = copies.Add(target.expressionParameters);
                    var controls = XrtAvatarInstaller.FindAssignedRecord(target);
                    var menus = new HashSet<VRCExpressionsMenu>();
                    XrtAvatarAssets.AddMenu(copies, target.expressionsMenu, menus);
                    XrtAvatarAssets.AddMenu(copies, controls?.xrtMenu, menus);
                    copies.Remap();
                    // The temporary record is deliberately outside the persistent copy graph.
                    working.ownedLayers = record.ownedLayers.Select(layer => copies.Get(layer)).ToArray();
                    UpdatePlacement(target, working, false);
                    copies.Save(folder);
                    copies.RemapScene(target.gameObject);
                    EnsureVisibility(target, working, controls == null ? null : copies.Get(controls.xrtMenu), false);
                    EnsureBuffering(working, false);
                    copies.Commit();
                }
                finally { Object.DestroyImmediate(working); }
            }
        }

        private static int AvailableBytes(VRCAvatarDescriptor avatar)
        {
            var parameters = avatar.customExpressions
                ? avatar.expressionParameters ?? avatar.expressionsMenu?.Parameters : null;
            var entries = parameters != null
                ? parameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>()
                : VRC.SDK3.Editor.VRCExpressionParametersEditor.GetDefaultParameters();
            if (parameters != null && EditorUtility.IsDirty(parameters))
                throw new InvalidOperationException("Save the Expressions Parameters asset first.");
            var budget = XrtParameterBudget.Inspect(avatar, entries);
            foreach (int bytes in XrtCaptionProtocol.BusByteCounts)
                if (budget.Bits + (bytes + 1) * 8 + 1 <= 256 && budget.Count + bytes + 5 <= 8192) return bytes;
            if (budget.Count + XrtCaptionProtocol.BusByteCounts.Min() + 5 > 8192)
                throw new InvalidOperationException("This avatar has no room for caption parameters. Remove unused Expressions Parameters first.");
            throw new InvalidOperationException("Caption objects need at least 25 free synced bits in Expressions Parameters.");
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

        private static AnimatorController ValidateController(VRCAvatarDescriptor avatar)
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
            if (!EditorUtility.IsPersistent(controller) ||
                AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)).Any(EditorUtility.IsDirty))
                throw new InvalidOperationException("Save the FX controller before adding caption objects.");
            if (!(controller is AnimatorController animator))
                throw new InvalidOperationException("Use an Animator Controller for the avatar FX layer before adding caption objects.");
            return animator;
        }

        private static AnimatorController CloneController(AnimatorController source, string path)
        {
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path))
                throw new InvalidOperationException("Could not copy the avatar's FX controller.");
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }

        internal static XrtCaptionInstallationRecord FindRecord(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return null;
            XrtCaptionInstallationRecord result = null;
            int best = 0;
            bool ambiguous = false;
            foreach (string guid in AssetDatabase.FindAssets("t:XrtCaptionInstallationRecord", new[] { "Assets" }))
            {
                var record = AssetDatabase.LoadAssetAtPath<XrtCaptionInstallationRecord>(AssetDatabase.GUIDToAssetPath(guid));
                if (record == null) continue;
                Migrate(record);
                bool assigned = record.installedController != null &&
                    (avatar.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                    .Any(layer => layer.animatorController == record.installedController);
                bool receiver = assigned && record.installedController.layers
                    .Any(layer => record.ownedLayers.Contains(layer.stateMachine));
                // Historical records may retain this renderer's old scene ID. Prefer
                // the assigned receiver, and only recover by actual owned surfaces.
                int score = receiver ? 3 : record.objects.Count > 0 && record.installedParameters != null &&
                    avatar.expressionParameters == record.installedParameters ? 2 :
                    record.objects.Any(item => FindOwnedRenderer(avatar, item) != null) ? 1 : 0;
                if (score == 0 || score < best) continue;
                if (score == best) { ambiguous = true; continue; }
                result = record;
                best = score;
                ambiguous = false;
            }
            if (ambiguous) throw new InvalidOperationException("More than one caption setup is assigned to this avatar.");
            return result;
        }

        private static bool ReceiverAssigned(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record)
        {
            if ((record.recordVersion < 2 || record.recordVersion > 4) || record.installedController == null ||
                !avatar.customizeAnimationLayers || !avatar.customExpressions) return false;
            bool controller = (avatar.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                .Any(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX && !layer.isDefault &&
                    layer.animatorController == record.installedController);
            var parameters = avatar.expressionParameters?.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>();
            return controller && record.ownedLayers.Length == (record.recordVersion >= 3 ? 3 : 2) &&
                record.ownedLayers.All(owned => record.installedController.layers.Any(layer => layer.stateMachine == owned)) &&
                record.expressionParameters.All(name => parameters.Any(parameter => parameter != null &&
                    parameter.name == name && parameter.valueType == (name == XrtCaptionAnimator.Visible
                        ? VRCExpressionParameters.ValueType.Bool : VRCExpressionParameters.ValueType.Int))) &&
                record.animatorParameters.All(name => record.installedController.parameters.Any(parameter => parameter.name == name));
        }

        private static void ValidateInstalled(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord record)
        {
            if (!ReceiverAssigned(avatar, record))
                throw new InvalidOperationException("Restore the caption FX controller and parameters before adding more text objects.");
        }

        private static string[] ExpressionNames(int bytes, bool visibility = true) => new[] {
            XrtCaptionProtocol.VersionParameter, XrtCaptionProtocol.ColumnsParameter,
            XrtCaptionProtocol.BytesParameter, XrtCaptionProtocol.Pointer
        }.Concat(Enumerable.Range(0, bytes).Select(index => XrtCaptionProtocol.BytePrefix + index))
            .Concat(visibility ? new[] { XrtCaptionAnimator.Visible } : Array.Empty<string>()).ToArray();

        private static string[] AnimatorNames(int bytes, bool visibility = true, bool buffering = true) => ExpressionNames(bytes, visibility)
            .Concat(new[] { XrtCaptionProtocol.Prefix + "Weight" })
            .Concat(Enumerable.Range(0, CaptionAssets.ByteCount).Select(index => XrtCaptionProtocol.Prefix + "Cache" + index))
            .Concat(buffering ? XrtCaptionAnimator.BufferParameters : Array.Empty<string>()).ToArray();

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
            record.expressionParameters = ExpressionNames(record.byteCount, false);
            record.animatorParameters = AnimatorNames(record.byteCount, false, false);
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
            var owned = FindOwnedRenderer(avatar, item);
            if (owned != null) return owned;
            // The record was selected by its assigned assets. Its local path also
            // identifies a duplicated renderer whose material or mesh was customized.
            var candidate = string.IsNullOrEmpty(item.path) ? null : avatar.transform.Find(item.path)?.GetComponent<MeshRenderer>();
            return candidate;
        }

        private static MeshRenderer FindOwnedRenderer(VRCAvatarDescriptor avatar, XrtCaptionInstallationRecord.TextObject item)
        {
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
        private static void Save(Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
