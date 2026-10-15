using UnityEditor.Animations;
using UnityEngine;

namespace XrtAvatarPlugin.ChilloutVR.Editor
{
    // Scene references decide whether this editor-only installation is active.
    public sealed class XrtChilloutCaptionRecord : ScriptableObject
    {
        [SerializeField, HideInInspector] internal int recordVersion = 1;
        [SerializeField, HideInInspector] internal string avatarGlobalId;
        [SerializeField, HideInInspector] internal string rendererGlobalId;
        [SerializeField, HideInInspector] internal AnimatorOverrideController originalOverride;
        [SerializeField, HideInInspector] internal AnimatorOverrideController installedOverride;
        [SerializeField, HideInInspector] internal AnimatorController installedController;
        [SerializeField, HideInInspector] internal Material material;
        [SerializeField, HideInInspector] internal Mesh mesh;
        [SerializeField, HideInInspector] internal int columns;
        [SerializeField, HideInInspector] internal string controllerHash;
        [SerializeField, HideInInspector] internal string overrideHash;
    }
}
