using UnityEngine;
using UnityEditor.Animations;

namespace XrtAvatarPlugin.ChilloutVR.Editor
{
    // Editor asset stored separately from the uploaded avatar hierarchy.
    public sealed class XrtChilloutInstallationRecord : ScriptableObject
    {
        [SerializeField, HideInInspector] internal int recordVersion = 1;
        [SerializeField, HideInInspector] internal bool originalUsesAdvancedSettings;
        [SerializeField, HideInInspector] internal bool originalHadSettings;
        [SerializeField, HideInInspector] internal AnimatorOverrideController installedOverride;
        [SerializeField, HideInInspector] internal AnimatorController installedController;
    }
}
