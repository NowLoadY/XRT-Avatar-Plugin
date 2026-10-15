using System;
using System.Collections.Generic;
using System.Linq;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using ABI.CCK.Components.ScriptableObjects;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using XrtAvatarPlugin.Presentation;
using Object = UnityEngine.Object;

namespace XrtAvatarPlugin.ChilloutVR.Editor
{
    internal static class XrtChilloutCaptionInstaller
    {
        private const int ByteCount = 8;
        private const string ReceiverGuid = "64cbd0590f6b485ab097f211f5e536da";
        private const string Root = "Assets/XRT-Avatar-Plugin/ChilloutVR";
        private static readonly string[] Parameters = new[] {
            XrtChilloutCaptionProtocol.VersionParameter, XrtChilloutCaptionProtocol.ColumnsParameter,
            XrtChilloutCaptionProtocol.BytesParameter, XrtChilloutCaptionProtocol.Pointer
        }.Concat(Enumerable.Range(0, XrtChilloutCaptionProtocol.BusWordCount)
            .Select(i => XrtChilloutCaptionProtocol.WordPrefix + i)).ToArray();

        internal sealed class Status
        {
            internal string Title;
            internal string Detail;
            internal bool CanAdd;
            internal bool CanRemove;
            internal int Columns;
            internal MeshRenderer[] Renderers = Array.Empty<MeshRenderer>();
        }

        internal static Status Inspect(CVRAvatar avatar)
        {
            try
            {
                XrtChilloutInstaller.ValidateAvatar(avatar);
                var record = FindRecord(avatar);
                var renderers = FindRenderers(avatar, record);
                if (HasBus(avatar))
                {
                    if (record == null) throw new InvalidOperationException("The caption installation record is missing. Restore its generated XRT folder.");
                    try { ValidateBus(XrtChilloutInstaller.CurrentController(avatar)); }
                    catch (InvalidOperationException failure)
                    {
                        return new Status { Title = "Check caption source", Detail = failure.Message,
                            CanRemove = true, Renderers = renderers };
                    }
                    return new Status {
                        Title = renderers.Length == 0 ? "No text object" : renderers.Length + " text objects",
                        CanAdd = true, CanRemove = true, Renderers = renderers,
                        Columns = XrtChilloutInstaller.CurrentController(avatar).parameters.First(p => p.name == XrtChilloutCaptionProtocol.ColumnsParameter).defaultInt,
                        Detail = "Add a text child to a prop, or select a text object to remove it."
                    };
                }
                ValidateSource(avatar);
                return new Status { Title = renderers.Length == 0 ? "Not added" : "Caption source missing", CanAdd = true,
                    CanRemove = renderers.Length > 0, Renderers = renderers,
                    Columns = renderers.Length > 0 ? record.columns : 0,
                    Detail = "128 characters · visible to nearby players" };
            }
            catch (Exception failure)
            {
                return new Status { Title = "Check avatar", Detail = failure.Message };
            }
        }

        internal static void Install(CVRAvatar avatar, Transform parent, string preset)
        {
            XrtChilloutInstaller.ValidateAvatar(avatar);
            if (parent == null) parent = avatar.transform;
            if (parent != avatar.transform && !parent.IsChildOf(avatar.transform))
                throw new InvalidOperationException("Choose an attachment inside this avatar.");
            var record = FindRecord(avatar);
            bool existingBus = HasBus(avatar);
            if (existingBus)
            {
                if (record == null) throw new InvalidOperationException("Restore the generated XRT installation folder before adding text objects.");
                ValidateBus(XrtChilloutInstaller.CurrentController(avatar));
            }
            else ValidateSource(avatar);
            var script = AssetDatabase.LoadAssetAtPath<CVRLuaScript>(AssetDatabase.GUIDToAssetPath(ReceiverGuid));
            if (script == null) throw new InvalidOperationException("Reimport the XRT package to restore its caption receiver.");
            CaptionAssets.LoadShader();

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add XRT Caption");
            string folder = null;
            try
            {
                XrtChilloutInstaller.Install(avatar);
                if (!existingBus) ValidateSource(avatar);
                var controller = XrtChilloutInstaller.CurrentController(avatar);
                XrtChilloutInstaller.EnsureFolder(Root);
                folder = Root + "/Caption-" + Guid.NewGuid().ToString("N");
                XrtChilloutInstaller.EnsureFolder(folder);
                var material = CaptionAssets.CreateMaterial();
                int sourceColumns = existingBus
                    ? controller.parameters.First(p => p.name == XrtChilloutCaptionProtocol.ColumnsParameter).defaultInt
                    : record != null && FindRenderers(avatar, record).Any() ? record.columns : 0;
                int columns = CaptionAuthoring.ConfigureMaterial(material, preset, sourceColumns);
                CaptionAuthoring.SetPreview(material, string.Empty, columns);
                Save(material, folder + "/Caption.mat");
                var mesh = CaptionAssets.CreateMesh();
                Save(mesh, folder + "/Caption.asset");
                if (!existingBus)
                {
                    Undo.RecordObject(controller, "Add XRT caption parameters");
                    AddInt(controller, XrtChilloutCaptionProtocol.VersionParameter, XrtChilloutCaptionProtocol.Version);
                    AddInt(controller, XrtChilloutCaptionProtocol.ColumnsParameter, columns);
                    AddInt(controller, XrtChilloutCaptionProtocol.BytesParameter, ByteCount);
                    AddInt(controller, XrtChilloutCaptionProtocol.Pointer, XrtChilloutCaptionProtocol.ClearPointer);
                    for (int i = 0; i < XrtChilloutCaptionProtocol.BusWordCount; i++)
                        AddInt(controller, XrtChilloutCaptionProtocol.WordPrefix + i, 0);
                    EditorUtility.SetDirty(controller);
                    AssetDatabase.SaveAssetIfDirty(controller);
                }

                var board = new GameObject("XRT Text");
                Undo.RegisterCreatedObjectUndo(board, "Add XRT Caption");
                Undo.SetTransformParent(board.transform, parent, "Attach XRT Caption");
                GameObjectUtility.EnsureUniqueNameForSibling(board);
                board.transform.localPosition = parent == avatar.transform
                    ? new Vector3(0, Mathf.Max(.5f, avatar.viewPosition.y * .65f), .3f) : Vector3.zero;
                board.transform.localRotation = Quaternion.identity;
                board.transform.localScale = CaptionAuthoring.Dimensions(columns);
                board.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = board.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                var receiver = board.AddComponent<CVRLuaClientBehaviour>();
                receiver.asset = script;
                receiver.localOnly = false;
                receiver.boundObjects = new[] { new CVRBaseLuaBehaviour.BoundObject
                    { name = "captionRenderer", boundThing = renderer } };

                if (record == null)
                {
                    record = ScriptableObject.CreateInstance<XrtChilloutCaptionRecord>();
                    record.avatarGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(avatar).ToString();
                    record.originalOverride = avatar.overrides;
                    record.installedController = controller;
                    record.columns = columns;
                    Save(record, folder + "/Installation.asset");
                }
                Undo.RecordObject(record, "Add XRT Caption");
                Migrate(record);
                record.columns = columns;
                record.displays.Add(new XrtChilloutCaptionRecord.Display {
                    rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString(),
                    material = material, mesh = mesh
                });
                EditorUtility.SetDirty(record);
                AssetDatabase.SaveAssetIfDirty(record);
                XrtChilloutInstaller.MarkChanged(avatar);
                Undo.CollapseUndoOperations(undo);
                Selection.activeGameObject = board;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                if (folder != null && AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
                throw;
            }
        }

        internal static void Remove(CVRAvatar avatar, MeshRenderer selected)
        {
            XrtChilloutInstaller.ValidateAvatar(avatar);
            var record = FindRecord(avatar);
            if (record == null) return;
            var renderers = FindRenderers(avatar, record);
            if (renderers.Length > 0 && (selected == null || !renderers.Contains(selected)))
                throw new InvalidOperationException("Choose the text object to remove.");
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove XRT Caption");
            Undo.RecordObject(record, "Remove XRT Caption");
            Migrate(record);
            if (selected != null)
            {
                var display = record.displays.First(item => Resolve(avatar, item).Contains(selected));
                var receiver = selected.GetComponents<CVRLuaClientBehaviour>().FirstOrDefault(r => IsOwnedReceiver(r, selected));
                CaptionAuthoring.RemoveObject(selected, display.mesh, display.material, receiver);
                // Retain asset ownership for Undo and saved scene revisions.
            }
            if (!FindRenderers(avatar, record).Any())
                XrtChilloutInstaller.RemoveParameters(XrtChilloutInstaller.CurrentController(avatar), Parameters);
            EditorUtility.SetDirty(record);
            AssetDatabase.SaveAssetIfDirty(record);
            XrtChilloutInstaller.MarkChanged(avatar);
            Undo.CollapseUndoOperations(undo);
        }

        internal static bool IsInstalled(CVRAvatar avatar) => avatar != null &&
            (HasBus(avatar) || FindRenderers(avatar, FindRecord(avatar)).Length != 0);

        // Version-1 captions copied the control controller. Keep that provenance so
        // their menu record remains usable after removing the last caption.
        internal static bool UsesControl(CVRAvatar avatar, AnimatorOverrideController control) => control != null &&
            Records(avatar).Any(record => record.originalOverride == control &&
                record.installedController == XrtChilloutInstaller.CurrentController(avatar));

        private static bool HasBus(CVRAvatar avatar) =>
            XrtChilloutInstaller.CurrentController(avatar)?.parameters.Any(p => Parameters.Contains(p.name)) ?? false;

        private static void ValidateBus(AnimatorController controller)
        {
            if (Parameters.Any(name => !controller.parameters.Any(p => p.name == name && p.type == AnimatorControllerParameterType.Int)))
                throw new InvalidOperationException("Restore the missing XRT caption parameters before adding a text object.");
            int columns = controller.parameters.First(p => p.name == XrtChilloutCaptionProtocol.ColumnsParameter).defaultInt;
            if (columns != 8 && columns != 16 && columns != 32)
                throw new InvalidOperationException("Set the caption column count to 8, 16 or 32.");
        }

        private static void ValidateSource(CVRAvatar avatar)
        {
            var controller = XrtChilloutInstaller.CurrentController(avatar);
            if (controller == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(controller)))
                throw new InvalidOperationException("Configure the avatar's animator in the CCK before adding captions.");
            var synced = CVRCommon.GetParametersFromController(controller, CVRCommon.NonCoreFilter, CVRCommon.NonLocalFilter)
                .GroupBy(p => p.name).Select(group => group.First()).ToArray();
            int bools = synced.Count(p => p.type == AnimatorControllerParameterType.Bool);
            int values = synced.Count(p => p.type != AnimatorControllerParameterType.Bool && p.type != AnimatorControllerParameterType.Trigger);
            int usage = values * 32 + Mathf.CeilToInt(bools / 8f) * 8;
            if (usage + (XrtChilloutCaptionProtocol.BusWordCount + 1) * 32 > CVRCommon.AVATAR_BIT_LIMIT)
                throw new InvalidOperationException("Free 96 synced bits in the avatar's animator before adding captions.");
        }

        private static IEnumerable<XrtChilloutCaptionRecord> Records(CVRAvatar avatar)
        {
            if (avatar == null) return Enumerable.Empty<XrtChilloutCaptionRecord>();
            string id = GlobalObjectId.GetGlobalObjectIdSlow(avatar).ToString();
            return AssetDatabase.FindAssets("t:XrtChilloutCaptionRecord")
                .Select(g => AssetDatabase.LoadAssetAtPath<XrtChilloutCaptionRecord>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(record => record != null && record.avatarGlobalId == id);
        }

        private static XrtChilloutCaptionRecord FindRecord(CVRAvatar avatar)
        {
            if (avatar == null) return null;
            var controller = XrtChilloutInstaller.CurrentController(avatar);
            var records = Records(avatar).Where(record => record.installedController == controller ||
                FindRenderers(avatar, record).Any()).ToArray();
            if (records.Length > 1) throw new InvalidOperationException("More than one caption installation is registered for this avatar.");
            return records.FirstOrDefault();
        }

        private static IEnumerable<XrtChilloutCaptionRecord.Display> Displays(XrtChilloutCaptionRecord record)
        {
            if (record == null) return Enumerable.Empty<XrtChilloutCaptionRecord.Display>();
            if (record.displays != null && record.displays.Count > 0) return record.displays;
            if (record.mesh == null) return Enumerable.Empty<XrtChilloutCaptionRecord.Display>();
            return new[] { new XrtChilloutCaptionRecord.Display {
                rendererGlobalId = record.rendererGlobalId, material = record.material, mesh = record.mesh
            } };
        }

        private static void Migrate(XrtChilloutCaptionRecord record)
        {
            record.displays = Displays(record).ToList();
            record.rendererGlobalId = null;
            record.material = null;
            record.mesh = null;
        }

        private static MeshRenderer[] FindRenderers(CVRAvatar avatar, XrtChilloutCaptionRecord record) =>
            avatar == null ? Array.Empty<MeshRenderer>() : Displays(record).SelectMany(display => Resolve(avatar, display)).Distinct().ToArray();

        private static MeshRenderer[] Resolve(CVRAvatar avatar, XrtChilloutCaptionRecord.Display display)
        {
            MeshRenderer identified = null;
            if (GlobalObjectId.TryParse(display.rendererGlobalId, out var id))
                identified = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as MeshRenderer;
            return avatar.GetComponentsInChildren<MeshRenderer>(true).Where(renderer =>
                (renderer == identified || renderer.GetComponent<MeshFilter>()?.sharedMesh == display.mesh) &&
                renderer.GetComponents<CVRLuaClientBehaviour>().Any(receiver => IsOwnedReceiver(receiver, renderer))).ToArray();
        }

        private static bool IsOwnedReceiver(CVRLuaClientBehaviour receiver, MeshRenderer renderer) => receiver != null && receiver.asset != null &&
            AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(receiver.asset)) == ReceiverGuid &&
            receiver.boundObjects != null && receiver.boundObjects.Any(binding => binding.name == "captionRenderer" && binding.boundThing == renderer);

        private static void AddInt(AnimatorController controller, string name, int value) => controller.AddParameter(
            new AnimatorControllerParameter { name = name, type = AnimatorControllerParameterType.Int, defaultInt = value });
        private static void Save(Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
