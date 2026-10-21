using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace XrtAvatarPlugin.Editor
{
    public sealed class XrtCaptionInstallationRecord : ScriptableObject
    {
        [SerializeField, HideInInspector] internal int recordVersion = 4;
        [SerializeField, HideInInspector] internal string objectName;
        [SerializeField, HideInInspector] internal string rendererGlobalId;
        [SerializeField, HideInInspector] internal VRCExpressionParameters originalParameters;
        [SerializeField, HideInInspector] internal VRCExpressionParameters installedParameters;
        [SerializeField, HideInInspector] internal AnimatorController installedController;
        [SerializeField, HideInInspector] internal Material material;
        [SerializeField, HideInInspector] internal List<TextObject> objects = new List<TextObject>();
        [SerializeField, HideInInspector] internal AnimatorStateMachine[] ownedLayers;
        [SerializeField, HideInInspector] internal AnimationClip[] bindingClips;
        [SerializeField, HideInInspector] internal string[] expressionParameters;
        [SerializeField, HideInInspector] internal string[] animatorParameters;

        [Serializable]
        internal sealed class TextObject
        {
            public string rendererGlobalId;
            public string path;
            public Mesh mesh;
            public Material material;
            public Handheld handheld;
        }

        [Serializable]
        internal sealed class SceneNode
        {
            public string globalId;
            public string path;
        }

        [Serializable]
        internal sealed class Handheld
        {
            public SceneNode originalParent;
            public SceneNode carry;
            public SceneNode aim;
            public SceneNode rightGrip;
            public string parameter;
            public string contactTag;
            public AnimatorStateMachine layer;
            public AnimationClip carryClip;
            public AnimationClip alignedClip;
        }
        [SerializeField, HideInInspector] internal int byteCount;
        [SerializeField, HideInInspector] internal int columns;
    }
}
