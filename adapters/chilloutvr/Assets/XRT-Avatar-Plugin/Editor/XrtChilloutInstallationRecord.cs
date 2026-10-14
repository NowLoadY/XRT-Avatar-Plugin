using UnityEngine;
using UnityEditor.Animations;

namespace XrtAvatarPlugin.ChilloutVR.Editor
{
    // Editor asset stored separately from the uploaded avatar hierarchy.
    public sealed class XrtChilloutInstallationRecord : ScriptableObject
    {
        [SerializeField, HideInInspector] internal int recordVersion = 1;
        [SerializeField, HideInInspector] internal string avatarGlobalId;
        [SerializeField, HideInInspector] internal bool installed;
        [SerializeField, HideInInspector] internal bool originalUsesAdvancedSettings;
        [SerializeField, HideInInspector] internal bool originalHadSettings;
        [SerializeField, HideInInspector] internal bool hadOriginalOverride;
        [SerializeField, HideInInspector] internal AnimatorOverrideController originalOverride;
        [SerializeField, HideInInspector] internal AnimatorOverrideController installedOverride;
        [SerializeField, HideInInspector] internal AnimatorController installedController;
        [SerializeField, HideInInspector] internal string controllerHash;
        [SerializeField, HideInInspector] internal string overrideHash;
    }
}
