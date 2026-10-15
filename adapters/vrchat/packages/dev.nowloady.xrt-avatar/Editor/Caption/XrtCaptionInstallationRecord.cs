using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace XrtAvatarPlugin.Editor
{
    public sealed class XrtCaptionInstallationRecord : ScriptableObject
    {
        [SerializeField, HideInInspector] internal int recordVersion = 1;
        [SerializeField, HideInInspector] internal string avatarGlobalId;
        [SerializeField, HideInInspector] internal string objectName;
        [SerializeField, HideInInspector] internal string rendererGlobalId;
        [SerializeField, HideInInspector] internal bool originalCustomExpressions;
        [SerializeField, HideInInspector] internal bool originalCustomizeAnimationLayers;
        [SerializeField, HideInInspector] internal VRCExpressionParameters originalParameters;
        [SerializeField, HideInInspector] internal VRCAvatarDescriptor.CustomAnimLayer[] originalBaseLayers;
        [SerializeField, HideInInspector] internal VRCAvatarDescriptor.CustomAnimLayer[] installedBaseLayers;
        [SerializeField, HideInInspector] internal VRCAvatarDescriptor.CustomAnimLayer[] originalSpecialLayers;
        [SerializeField, HideInInspector] internal VRCAvatarDescriptor.CustomAnimLayer[] installedSpecialLayers;
        [SerializeField, HideInInspector] internal VRCExpressionParameters installedParameters;
        [SerializeField, HideInInspector] internal AnimatorController installedController;
        [SerializeField, HideInInspector] internal Material material;
        [SerializeField, HideInInspector] internal int byteCount;
        [SerializeField, HideInInspector] internal int columns;
        [SerializeField, HideInInspector] internal string parametersHash;
        [SerializeField, HideInInspector] internal string controllerHash;
    }
}
