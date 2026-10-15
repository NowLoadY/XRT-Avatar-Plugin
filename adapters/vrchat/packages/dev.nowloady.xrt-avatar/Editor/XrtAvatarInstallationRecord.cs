using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace XrtAvatarPlugin.Editor
{
    // Kept outside the avatar hierarchy; this record is never uploaded with it.
    public sealed class XrtAvatarInstallationRecord : ScriptableObject
    {
        [SerializeField, HideInInspector] internal int recordVersion = 1;
        [SerializeField, HideInInspector] internal bool originalCustomExpressions;
        [SerializeField, HideInInspector] internal bool hadOriginalMenu;
        [SerializeField, HideInInspector] internal bool hadOriginalParameters;
        [SerializeField, HideInInspector] internal VRCExpressionsMenu originalMenu;
        [SerializeField, HideInInspector] internal VRCExpressionParameters originalParameters;
        [SerializeField, HideInInspector] internal VRCExpressionsMenu installedMenu;
        [SerializeField, HideInInspector] internal VRCExpressionParameters installedParameters;
        [SerializeField, HideInInspector] internal VRCExpressionsMenu xrtMenu;
    }
}
