using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace XrtAvatarPlugin.Presentation
{
    /// <summary>Copies explicitly selected asset files without changing their sources.</summary>
    public sealed class AssetCopies : IDisposable
    {
        private sealed class AssetFile
        {
            internal string Path;
            internal Object Main;
            internal Object[] Objects;
        }

        private readonly Dictionary<Object, Object> copies = new Dictionary<Object, Object>();
        private readonly List<AssetFile> files = new List<AssetFile>();
        private readonly List<string> savedPaths = new List<string>();
        private bool saved;
        private bool committed;

        public T Add<T>(T source) where T : Object
        {
            if (source == null) return null;
            if (copies.TryGetValue(source, out var existing)) return (T)existing;
            if (saved) throw new InvalidOperationException("Add all assets before saving their copies.");
            string path = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(path) || !EditorUtility.IsPersistent(source))
                throw new InvalidOperationException("Save the source asset before copying it: " + source.name);
            var main = AssetDatabase.LoadMainAssetAtPath(path);
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);
            if (main == null || !objects.Contains(source) || !objects.Contains(main) || objects.Any(value => value == null))
                throw new InvalidOperationException("Could not read every object in asset: " + path);
            foreach (var value in objects)
            {
                var copy = Create(value);
                copies.Add(value, copy);
                // CopySerialized retains current unsaved settings and starts all references
                // at their source values; Remap connects the completed copy graph afterward.
                EditorUtility.CopySerialized(value, copy);
                copy.name = value.name;
                copy.hideFlags = value.hideFlags & ~(HideFlags.DontSaveInEditor |
                    HideFlags.DontSaveInBuild | HideFlags.DontUnloadUnusedAsset);
            }
            files.Add(new AssetFile { Path = path, Main = main, Objects = objects });
            return Get(source);
        }

        public T Get<T>(T source) where T : Object
        {
            if (source == null) return null;
            if (!copies.TryGetValue(source, out var copy))
                throw new InvalidOperationException("The asset was not included in this copy: " + source.name);
            return (T)copy;
        }

        public void Remap()
        {
            foreach (var copy in copies.Values) RemapObject(copy, null);
        }

        public void Save(string folder)
        {
            if (saved) throw new InvalidOperationException("These copies have already been saved.");
            if (!AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("Create the destination asset folder first: " + folder);
            saved = true;
            foreach (var file in files)
            {
                string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + Path.GetFileName(file.Path));
                savedPaths.Add(path);
                var main = Get(file.Main);
                AssetDatabase.CreateAsset(main, path);
                foreach (var source in file.Objects)
                {
                    if (source == file.Main) continue;
                    var copy = Get(source);
                    if (!EditorUtility.IsPersistent(copy)) AssetDatabase.AddObjectToAsset(copy, main);
                    else if (AssetDatabase.GetAssetPath(copy) != path)
                        throw new IOException("Copied subasset belongs to another file: " + copy.name);
                }
            }
            // Every copied reference must be persistent before any file is serialized.
            foreach (var copy in copies.Values)
            {
                if (!EditorUtility.IsPersistent(copy))
                    throw new IOException("Could not save copied asset: " + copy.name);
                EditorUtility.SetDirty(copy);
            }
            foreach (var file in files) AssetDatabase.SaveAssetIfDirty(Get(file.Main));
        }

        public void RemapScene(GameObject root, string undoAction = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            bool changed = false;
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null && RemapObject(component, undoAction))
                {
                    changed = true;
                    if (undoAction != null && PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            // Build-time copies use a null action and never dirty an authoring scene.
            if (changed && undoAction != null && root.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(root.scene);
        }

        private bool RemapObject(Object target, string undoAction)
        {
            using (var serialized = new SerializedObject(target))
            {
                var property = serialized.GetIterator();
                bool changed = false;
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var reference = property.objectReferenceValue;
                    if (reference == null || !copies.TryGetValue(reference, out var copy)) continue;
                    if (!changed && undoAction != null) Undo.RecordObject(target, undoAction);
                    property.objectReferenceValue = copy;
                    changed = true;
                }
                if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
                return changed;
            }
        }

        private static Object Create(Object source)
        {
            // Unity animator objects need explicit construction; Instantiate does not
            // provide a complete independent state-machine graph.
            if (source is AnimatorController) return new AnimatorController();
            if (source is AnimatorOverrideController) return new AnimatorOverrideController();
            if (source is AnimatorStateMachine) return new AnimatorStateMachine();
            if (source is AnimatorState) return new AnimatorState();
            if (source is AnimatorStateTransition) return new AnimatorStateTransition();
            if (source is AnimatorTransition) return new AnimatorTransition();
            if (source is BlendTree) return new BlendTree();
            if (source is AnimationClip) return new AnimationClip();
            if (source is AvatarMask) return new AvatarMask();
            if (source is ScriptableObject) return ScriptableObject.CreateInstance(source.GetType());
            if (source is Material || source is Mesh) return Object.Instantiate(source);
            throw new InvalidOperationException("This asset type cannot be copied: " + source.GetType().Name);
        }

        /// <summary>Keep the completed copies after this operation is disposed.</summary>
        public void Commit() => committed = true;

        public void Dispose()
        {
            if (!committed)
            {
                for (int i = savedPaths.Count - 1; i >= 0; i--)
                    if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(savedPaths[i])) &&
                        !AssetDatabase.DeleteAsset(savedPaths[i]))
                        Debug.LogError("Could not remove copied asset: " + savedPaths[i]);
                foreach (var copy in copies.Values)
                    if (copy != null && !EditorUtility.IsPersistent(copy)) Object.DestroyImmediate(copy);
            }
            copies.Clear();
            files.Clear();
            savedPaths.Clear();
        }
    }
}
