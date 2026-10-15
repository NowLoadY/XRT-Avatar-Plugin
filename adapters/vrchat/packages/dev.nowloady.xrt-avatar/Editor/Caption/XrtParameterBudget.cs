using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace XrtAvatarPlugin.Editor
{
    // NDMF's public parameter providers include build-time contributions from avatar tools.
    // Reflection keeps that integration optional for projects using only the VRChat SDK.
    internal sealed class XrtParameterBudget
    {
        internal int Bits;
        internal int Count;

        internal static XrtParameterBudget Inspect(VRCAvatarDescriptor avatar,
            VRCExpressionParameters.Parameter[] fallback)
        {
            var parameters = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var parameter in fallback)
            {
                if (parameter == null || string.IsNullOrEmpty(parameter.name)) continue;
                RequireAvailableName(parameter.name);
                Merge(parameters, parameter.name, parameter.networkSynced
                    ? parameter.valueType == VRCExpressionParameters.ValueType.Bool ? 1 : 8 : 0);
            }
            var nativeNames = new HashSet<string>(parameters.Keys, StringComparer.Ordinal);
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(value => value.GetName().Name == "nadena.dev.ndmf");
            if (assembly != null)
            {
                var supplied = ReadNdmf(assembly, avatar.gameObject);
                // The provider already includes and remaps the assigned descriptor asset.
                // SDK defaults and a menu-only Parameters reference need their own fallback.
                if (avatar.customExpressions && avatar.expressionParameters != null)
                    parameters.Clear();
                foreach (var parameter in supplied)
                {
                    if (string.IsNullOrEmpty(parameter.Name)) continue;
                    RequireAvailableName(parameter.Name);
                    if (IsControlName(parameter.Name) && !nativeNames.Contains(parameter.Name))
                        throw new InvalidOperationException("Another avatar component already uses XRT controls. Rename or remove its XRT parameters first.");
                    if (!parameter.AnimatorOnly)
                        Merge(parameters, parameter.Name, parameter.Bits);
                }
            }
            // Caption installation also adds these unsynced menu controls when absent.
            Merge(parameters, XrtAvatarProtocol.Enabled, 0);
            Merge(parameters, XrtAvatarProtocol.Status, 0);
            Merge(parameters, XrtAvatarProtocol.VersionParameter, 0);
            return new XrtParameterBudget { Bits = parameters.Values.Sum(), Count = parameters.Count };
        }

        private static List<(string Name, int Bits, bool AnimatorOnly)> ReadNdmf(
            System.Reflection.Assembly assembly, GameObject avatar)
        {
            try
            {
                var infoType = assembly.GetType("nadena.dev.ndmf.ParameterInfo", true);
                var parameterType = assembly.GetType("nadena.dev.ndmf.ProvidedParameter", true);
                var query = infoType.GetMethod("GetParametersForObject", new[] {
                    typeof(GameObject), infoType.GetNestedType("ConflictHandler")
                });
                var info = infoType.GetField("ForUI").GetValue(null);
                var effectiveName = parameterType.GetProperty("EffectiveName");
                var bitUsage = parameterType.GetProperty("BitUsage");
                var animatorOnly = parameterType.GetProperty("IsAnimatorOnly");
                if (query == null || info == null || effectiveName == null || bitUsage == null || animatorOnly == null)
                    throw new MissingMemberException();
                var supplied = (IEnumerable)query.Invoke(info, new object[] { avatar, null });
                if (supplied == null) throw new InvalidOperationException();
                var result = new List<(string, int, bool)>();
                foreach (var parameter in supplied)
                    result.Add(((string)effectiveName.GetValue(parameter), (int)bitUsage.GetValue(parameter),
                        (bool)animatorOnly.GetValue(parameter)));
                return result;
            }
            catch (Exception failure)
            {
                throw new InvalidOperationException("Could not read the avatar's parameter budget. Update NDMF, then try again.", failure);
            }
        }

        private static void Merge(Dictionary<string, int> parameters, string name, int bits)
        {
            parameters.TryGetValue(name, out int previous);
            parameters[name] = Math.Max(previous, bits);
        }

        private static void RequireAvailableName(string name)
        {
            if (name.StartsWith(XrtCaptionAnimator.Prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("This avatar already contains caption parameters. Remove or rename the existing caption setup first.");
        }

        private static bool IsControlName(string name) => name == XrtAvatarProtocol.Enabled ||
            name == XrtAvatarProtocol.Status || name == XrtAvatarProtocol.VersionParameter;
    }
}
