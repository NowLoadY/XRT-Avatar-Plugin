using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace XrtAvatarPlugin.ChilloutVR.Editor
{
    // Scene references decide whether this editor-only installation is active.
    public sealed class XrtChilloutCaptionRecord : ScriptableObject
    {
        [SerializeField, HideInInspector] internal int recordVersion = 1;
        [SerializeField, HideInInspector] internal string rendererGlobalId;
        [SerializeField, HideInInspector] internal AnimatorOverrideController originalOverride;
        [SerializeField, HideInInspector] internal AnimatorController installedController;
        [SerializeField, HideInInspector] internal Material material;
        [SerializeField, HideInInspector] internal Mesh mesh;
        [SerializeField, HideInInspector] internal int columns;
        [SerializeField, HideInInspector] internal List<Display> displays = new List<Display>();

        [Serializable]
        internal sealed class Display
        {
            public string rendererGlobalId;
            public Material material;
            public Mesh mesh;
        }
    }
}
