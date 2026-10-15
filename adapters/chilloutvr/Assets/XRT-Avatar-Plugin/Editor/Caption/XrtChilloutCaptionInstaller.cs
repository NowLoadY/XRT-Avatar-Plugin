using System;
using System.IO;
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

        internal sealed class Status
        {
            internal string Title;
            internal string Detail;
            internal bool CanAdd;
            internal bool CanRemove;
            internal XrtChilloutCaptionRecord Record;
            internal MeshRenderer Renderer;
        }

        internal static Status Inspect(CVRAvatar avatar)
        {
            try
            {
                XrtChilloutInstaller.ValidateAvatar(avatar);
                var record = FindRecord(avatar);
                if (record != null)
                {
                    ValidateInstalled(avatar, record);
                    var renderer = FindRenderers(avatar, record).FirstOrDefault();
                    return new Status
                    {
                        Title = "Added", CanRemove = true, Record = record, Renderer = renderer,
                        Detail = renderer == null ? "Caption object is missing. Remove, then add it again." :
                            "Move, rotate and scale the object in the Scene view."
                    };
                }
                var menu = XrtChilloutInstaller.Inspect(avatar);
                if (!menu.CanInstall && !menu.CanRemove) throw new InvalidOperationException(menu.Detail);
                ValidateSource(avatar);
                return new Status { Title = "Not added", CanAdd = true,
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
            if (FindRecord(avatar) != null) throw new InvalidOperationException("A caption object is already added to this avatar.");
            if (parent == null) parent = avatar.transform;
            if (parent != avatar.transform && !parent.IsChildOf(avatar.transform))
                throw new InvalidOperationException("Choose an attachment inside this avatar.");
            ValidateSource(avatar);
            var script = AssetDatabase.LoadAssetAtPath<CVRLuaScript>(AssetDatabase.GUIDToAssetPath(ReceiverGuid));
            if (script == null) throw new InvalidOperationException("Reimport the XRT package to restore its caption receiver.");
            CaptionAssets.LoadShader();

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add XRT Caption");
            string folder = null;
            GameObject board = null;
            try
            {
                XrtChilloutInstaller.Install(avatar);
                var sourceOverride = avatar.overrides;
                var sourceController = XrtChilloutInstaller.BaseController(sourceOverride);
                XrtChilloutInstaller.EnsureFolder(Root);
                folder = Root + "/Caption-" + Guid.NewGuid().ToString("N");
                XrtChilloutInstaller.EnsureFolder(folder);
                string controllerPath = folder + "/Avatar.controller";
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceController), controllerPath))
                    throw new IOException("Could not copy the avatar controller.");
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (controller == null) throw new IOException("Could not load the copied avatar controller.");
                var material = CaptionAssets.CreateMaterial();
                int columns = CaptionAuthoring.ConfigureMaterial(material, preset);
                // Sample text is confined to the window; uploaded materials begin empty.
                CaptionAuthoring.SetPreview(material, string.Empty, columns);
                Save(material, folder + "/Caption.mat");
                var mesh = CaptionAssets.CreateMesh();
                Save(mesh, folder + "/Caption.asset");
                AddInt(controller, XrtChilloutCaptionProtocol.VersionParameter, XrtChilloutCaptionProtocol.Version);
                AddInt(controller, XrtChilloutCaptionProtocol.ColumnsParameter, columns);
                AddInt(controller, XrtChilloutCaptionProtocol.BytesParameter, ByteCount);
                AddInt(controller, XrtChilloutCaptionProtocol.Pointer, XrtChilloutCaptionProtocol.ClearPointer);
                for (int i = 0; i < XrtChilloutCaptionProtocol.BusWordCount; i++)
                    AddInt(controller, XrtChilloutCaptionProtocol.WordPrefix + i, 0);
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssetIfDirty(controller);
                var overrides = new AnimatorOverrideController(controller) { name = "XRT Caption Overrides" };
                XrtChilloutInstaller.ApplyOverrideChain(sourceOverride, overrides, sourceController, controller);
                Save(overrides, folder + "/Overrides.overrideController");

                board = new GameObject("XRT Caption");
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

                var record = ScriptableObject.CreateInstance<XrtChilloutCaptionRecord>();
                record.avatarGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(avatar).ToString();
                record.rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString();
                record.originalOverride = sourceOverride;
                record.installedController = controller;
                record.installedOverride = overrides;
                record.material = material;
                record.mesh = mesh;
                record.columns = columns;
                record.controllerHash = XrtChilloutInstaller.Hash(controller);
                record.overrideHash = XrtChilloutInstaller.Hash(overrides);
                Save(record, folder + "/Installation.asset");
                Undo.RecordObject(avatar, "Add XRT Caption");
                avatar.overrides = overrides;
                XrtChilloutInstaller.MarkChanged(avatar);
                Undo.CollapseUndoOperations(undo);
                Selection.activeGameObject = board;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                if (board != null) Object.DestroyImmediate(board);
                if (folder != null && AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
                throw;
            }
        }

        internal static void Remove(CVRAvatar avatar)
        {
            XrtChilloutInstaller.ValidateAvatar(avatar);
            var record = FindRecord(avatar);
            if (record == null) return;
            ValidateInstalled(avatar, record);
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove XRT Caption");
            Undo.RecordObject(avatar, "Remove XRT Caption");
            avatar.overrides = record.originalOverride;
            foreach (var renderer in FindRenderers(avatar, record))
            {
                var board = renderer.gameObject;
                var receiver = board.GetComponents<CVRLuaClientBehaviour>().FirstOrDefault(r => IsOwnedReceiver(r, renderer));
                var filter = board.GetComponent<MeshFilter>();
                bool ownsFilter = filter != null && filter.sharedMesh == record.mesh;
                if (receiver != null && ownsFilter && board.transform.childCount == 0 && board.GetComponents<Component>().Length == 4)
                    Undo.DestroyObjectImmediate(board);
                else
                {
                    if (receiver != null) Undo.DestroyObjectImmediate(receiver);
                    if (ownsFilter) Undo.DestroyObjectImmediate(filter);
                    Undo.DestroyObjectImmediate(renderer);
                }
            }
            // Keep generated assets for Undo/Redo and other saved scene revisions.
            XrtChilloutInstaller.MarkChanged(avatar);
            Undo.CollapseUndoOperations(undo);
        }

        internal static bool IsInstalled(CVRAvatar avatar) => FindRecord(avatar) != null;
        internal static AnimatorOverrideController OverrideBeforeCaption(CVRAvatar avatar)
        {
            var record = FindRecord(avatar);
            return record == null ? avatar.overrides : record.originalOverride;
        }

        private static void ValidateSource(CVRAvatar avatar)
        {
            RuntimeAnimatorController source = avatar.overrides;
            if (source == null) source = avatar.GetComponent<Animator>()?.runtimeAnimatorController;
            if (source == null) source = avatar.avatarSettings?.baseController;
            var controller = XrtChilloutInstaller.BaseController(source);
            if (controller == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(controller))) throw new InvalidOperationException("Configure the avatar's animator in the CCK before adding captions.");
            var synced = CVRCommon.GetParametersFromController(controller, CVRCommon.NonCoreFilter, CVRCommon.NonLocalFilter)
                .GroupBy(p => p.name).Select(group => group.First()).ToArray();
            int bools = synced.Count(p => p.type == AnimatorControllerParameterType.Bool);
            int values = synced.Count(p => p.type != AnimatorControllerParameterType.Bool &&
                p.type != AnimatorControllerParameterType.Trigger);
            int usage = values * 32 + Mathf.CeilToInt(bools / 8f) * 8;
            if (usage + (XrtChilloutCaptionProtocol.BusWordCount + 1) * 32 > CVRCommon.AVATAR_BIT_LIMIT)
                throw new InvalidOperationException("Free 96 synced bits in the avatar's animator before adding captions.");
            if (XrtChilloutInstaller.HasUnsavedChanges(controller))
                throw new InvalidOperationException("Save the avatar's animator assets before adding captions.");
            if (controller.parameters.Any(p => p.name.StartsWith("XRT/Caption/", StringComparison.Ordinal) ||
                p.name.StartsWith("#XRT/Caption/", StringComparison.Ordinal)))
                throw new InvalidOperationException("This avatar already has caption parameters. Restore its original controller before adding captions.");
        }

        private static XrtChilloutCaptionRecord FindRecord(CVRAvatar avatar)
        {
            if (avatar == null || avatar.overrides == null) return null;
            string id = GlobalObjectId.GetGlobalObjectIdSlow(avatar).ToString();
            var records = AssetDatabase.FindAssets("t:XrtChilloutCaptionRecord")
                .Select(g => AssetDatabase.LoadAssetAtPath<XrtChilloutCaptionRecord>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r => r != null && r.avatarGlobalId == id && r.installedOverride == avatar.overrides).ToArray();
            if (records.Length > 1) throw new InvalidOperationException("More than one caption installation is registered for this avatar.");
            return records.FirstOrDefault();
        }

        private static MeshRenderer[] FindRenderers(CVRAvatar avatar, XrtChilloutCaptionRecord record)
        {
            if (GlobalObjectId.TryParse(record.rendererGlobalId, out var id) &&
                GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is MeshRenderer renderer &&
                renderer.transform.IsChildOf(avatar.transform))
                return new[] { renderer };
            // Unsaved scene objects may not resolve through GlobalObjectId yet.
            return avatar.GetComponentsInChildren<MeshRenderer>(true).Where(r =>
                r.GetComponent<MeshFilter>()?.sharedMesh == record.mesh &&
                r.GetComponents<CVRLuaClientBehaviour>().Any(receiver => IsOwnedReceiver(receiver, r))).ToArray();
        }

        private static bool IsOwnedReceiver(CVRLuaClientBehaviour receiver, MeshRenderer renderer) => receiver.asset != null &&
            AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(receiver.asset)) == ReceiverGuid &&
            receiver.boundObjects != null && receiver.boundObjects.Any(b => b.name == "captionRenderer" && b.boundThing == renderer);

        private static void ValidateInstalled(CVRAvatar avatar, XrtChilloutCaptionRecord record)
        {
            if (record.recordVersion != 1 || record.originalOverride == null || record.material == null || record.mesh == null ||
                !XrtChilloutInstaller.Unchanged(record.installedController, record.controllerHash) ||
                !XrtChilloutInstaller.Unchanged(record.installedOverride, record.overrideHash))
                throw new InvalidOperationException("This avatar's caption setup has been edited. Keep your changes and follow the recovery steps in the guide.");
        }

        private static void AddInt(AnimatorController controller, string name, int value) => controller.AddParameter(
            new AnimatorControllerParameter { name = name, type = AnimatorControllerParameterType.Int, defaultInt = value });
        private static void Save(Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
