using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace XrtAvatarPlugin.Editor
{
    // Optional integration: projects without Gesture Manager still import the package.
    internal sealed class GesturePreview : IDisposable
    {
        private const string ManagerName = "BlackStartX.GestureManager.GestureManager";
        private const string ModuleName = "BlackStartX.GestureManager.Editor.Modules.Vrc3.ModuleVrc3";
        private readonly Component manager;
        private readonly object module;
        private readonly FieldInfo moduleField;
        private readonly IDictionary parameters;
        private readonly MethodInfo read, set;
        private readonly bool ownsManager;
        private readonly List<(MeshRenderer renderer, Material original, Material temporary)> materials =
            new List<(MeshRenderer, Material, Material)>();
        internal UnityEngine.Object Menu => manager;
        internal bool IsValid => manager != null && ReferenceEquals(moduleField.GetValue(manager), module);
        internal static bool Available => FindType(ManagerName) != null && FindType(ModuleName) != null;
        private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name, false)).FirstOrDefault(type => type != null);

        internal GesturePreview(VRCAvatarDescriptor avatar)
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode to test.");
            Type managerType = FindType(ManagerName), moduleType = FindType(ModuleName);
            if (managerType == null || moduleType == null)
                throw new InvalidOperationException("Install Gesture Manager in ALCOM to test the expression menu.");
            moduleField = managerType.GetField("Module");
            foreach (var candidate in Resources.FindObjectsOfTypeAll(managerType).OfType<Component>())
            {
                if (!candidate.gameObject.scene.IsValid() || !candidate.gameObject.activeInHierarchy) continue;
                object current = moduleField.GetValue(candidate);
                if (current != null && current.GetType() == moduleType &&
                    (UnityEngine.Object)moduleType.GetField("AvatarDescriptor").GetValue(current) == avatar)
                { manager = candidate; module = current; break; }
            }
            try
            {
                foreach (var renderer in avatar.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Material original = renderer.sharedMaterial;
                    if (original == null || original.shader == null || original.shader.name != "XRT/Avatar Caption") continue;
                    var temporary = new Material(original) { hideFlags = HideFlags.HideAndDontSave };
                    materials.Add((renderer, original, temporary));
                    renderer.sharedMaterial = temporary;
                }
                if (manager == null)
                {
                    var temporary = new GameObject("XRT Local Test · Gesture Manager") { hideFlags = HideFlags.DontSave };
                    ownsManager = true;
                    manager = temporary.AddComponent(managerType);
                    FieldInfo settingsField = managerType.GetField("settings");
                    object settings = settingsField.GetValue(manager) ?? Activator.CreateInstance(settingsField.FieldType);
                    settings.GetType().GetField("favourite").SetValue(settings, avatar);
                    settingsField.SetValue(manager, settings);
                    module = Activator.CreateInstance(moduleType, new object[] { avatar, null });
                    managerType.GetMethod("SetModule").Invoke(manager, new[] { module });
                }
                parameters = (IDictionary)moduleType.GetField("Params").GetValue(module);
                if (!parameters.Contains(XrtAvatarProtocol.Enabled) || !parameters.Contains(XrtAvatarProtocol.VersionParameter))
                    throw new InvalidOperationException("Add XRT captions or avatar controls before testing.");
                Type parameterType = parameters[XrtAvatarProtocol.Enabled].GetType();
                read = parameterType.GetMethod("FloatValue");
                set = parameterType.GetMethod("Set", new[] { moduleType, typeof(float), typeof(object) });
                Write(XrtAvatarProtocol.Enabled, new LocalOscQuery.Value('F', 0));
            }
            catch { Dispose(); throw; }
        }

        internal Dictionary<string, LocalOscQuery.Value> Snapshot()
        {
            var result = new Dictionary<string, LocalOscQuery.Value>();
            foreach (DictionaryEntry pair in parameters)
            {
                string name = (string)pair.Key;
                if (!name.StartsWith("XRT/", StringComparison.Ordinal) && name != "MuteSelf") continue;
                object parameter = pair.Value;
                var type = (AnimatorControllerParameterType)parameter.GetType().GetField("Type").GetValue(parameter);
                float number = (float)read.Invoke(parameter, null);
                char tag = type == AnimatorControllerParameterType.Bool ? (number != 0 ? 'T' : 'F') :
                    type == AnimatorControllerParameterType.Int ? 'i' : 'f';
                result[name] = new LocalOscQuery.Value(tag, number);
            }
            return result;
        }

        internal void Write(string name, LocalOscQuery.Value value)
        {
            if (!parameters.Contains(name)) return;
            object parameter = parameters[name];
            var type = (AnimatorControllerParameterType)parameter.GetType().GetField("Type").GetValue(parameter);
            if (type == AnimatorControllerParameterType.Bool && !value.IsBool ||
                type == AnimatorControllerParameterType.Int && value.Type != 'i' ||
                type == AnimatorControllerParameterType.Float && value.Type != 'f') return;
            set.Invoke(parameter, new object[] { module, value.Number, null });
        }

        public void Dispose()
        {
            if (ownsManager && manager != null) UnityEngine.Object.DestroyImmediate(manager.gameObject);
            foreach (var surface in materials)
            {
                if (surface.renderer != null && surface.renderer.sharedMaterial == surface.temporary)
                    surface.renderer.sharedMaterial = surface.original;
                if (surface.temporary != null) UnityEngine.Object.DestroyImmediate(surface.temporary);
            }
            materials.Clear();
        }
    }
}
