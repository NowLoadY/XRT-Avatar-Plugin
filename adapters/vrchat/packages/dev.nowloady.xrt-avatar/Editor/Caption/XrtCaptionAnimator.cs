using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using XrtAvatarPlugin.Presentation;

namespace XrtAvatarPlugin.Editor
{
    // Transport and local retention stay in the VRChat adapter; the material is shared.
    internal static class XrtCaptionAnimator
    {
        internal const string Prefix = XrtCaptionProtocol.Prefix;
        internal const string Pointer = XrtCaptionProtocol.Pointer;
        internal const string BytePrefix = XrtCaptionProtocol.BytePrefix;
        internal const string Visible = Prefix + "Visible";
        private const string CachePrefix = Prefix + "Cache";
        private const string Weight = Prefix + "Weight";
        private const string Buffered = Prefix + "Buffered";
        private const string PendingPrefix = Prefix + "Pending";

        internal static IEnumerable<string> BufferParameters => new[] { Buffered }.Concat(
            Enumerable.Range(0, CaptionAssets.ByteCount).Select(i => PendingPrefix + i));

        internal static void Build(AnimatorController controller, string rendererPath, int byteCount, int columns,
            out AnimatorStateMachine[] ownedLayers, out AnimationClip[] bindingClips)
        {
            var clips = new List<AnimationClip>();
            AddParameter(controller, XrtCaptionProtocol.VersionParameter, AnimatorControllerParameterType.Int, XrtCaptionProtocol.Version);
            AddParameter(controller, XrtCaptionProtocol.ColumnsParameter, AnimatorControllerParameterType.Int, columns);
            AddParameter(controller, XrtCaptionProtocol.BytesParameter, AnimatorControllerParameterType.Int, byteCount);
            AddParameter(controller, Pointer, AnimatorControllerParameterType.Int, XrtCaptionProtocol.ClearPointer);
            AddParameter(controller, Weight, AnimatorControllerParameterType.Float, 1);
            for (int i = 0; i < byteCount; i++)
                AddParameter(controller, BytePrefix + i, AnimatorControllerParameterType.Int);
            for (int i = 0; i < CaptionAssets.ByteCount; i++)
                AddParameter(controller, CachePrefix + i, AnimatorControllerParameterType.Float);

            var receive = Layer(controller, "XRT Caption · Receive");
            var clock = new AnimationClip { name = "Caption receive interval", frameRate = 30 };
            AnimationUtility.SetEditorCurve(clock,
                EditorCurveBinding.FloatCurve(rendererPath, typeof(MeshRenderer), "material._Columns"),
                AnimationCurve.Constant(0, 1f / 30, columns));
            AssetDatabase.AddObjectToAsset(clock, controller);
            clips.Add(clock);
            var clear = receive.AddState("Clear");
            clear.writeDefaultValues = false;
            clear.motion = clock;
            var reset = clear.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            reset.localOnly = false;
            reset.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
            for (int i = 0; i < CaptionAssets.ByteCount; i++)
                reset.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = CachePrefix + i,
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    value = 0
                });
            receive.defaultState = clear;
            ReceiveTransition(receive, clear, XrtCaptionProtocol.ClearPointer, false);
            for (int chunk = 0; chunk < CaptionAssets.ByteCount / byteCount; chunk++)
            {
                var state = receive.AddState("Write " + chunk);
                state.writeDefaultValues = false;
                state.motion = clock;
                var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                driver.localOnly = false;
                driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
                for (int offset = 0; offset < byteCount; offset++)
                    driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Copy,
                        source = BytePrefix + offset,
                        name = CachePrefix + (chunk * byteCount + offset),
                        convertRange = false
                    });
                ReceiveTransition(receive, state, chunk);
            }
            AddBuffering(controller, receive, byteCount);

            var display = Layer(controller, "XRT Caption · Display");
            var show = display.AddState("Display");
            // VRChat recommends Write Defaults on for Direct Blend Tree states.
            show.writeDefaultValues = true;
            var direct = new BlendTree { name = "Caption bytes", blendType = BlendTreeType.Direct };
            AssetDatabase.AddObjectToAsset(direct, controller);
            // Independent properties must add at weight one, without dividing by child count.
            var serializedTree = new SerializedObject(direct);
            var normalized = serializedTree.FindProperty("m_NormalizedBlendValues");
            if (normalized == null)
                throw new InvalidOperationException("This Unity version cannot create the caption blend tree.");
            normalized.boolValue = false;
            serializedTree.ApplyModifiedPropertiesWithoutUndo();
            var children = new ChildMotion[CaptionAssets.ByteCount];
            for (int i = 0; i < children.Length; i++)
            {
                var value = new BlendTree
                {
                    name = "Byte " + i,
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = CachePrefix + i,
                    useAutomaticThresholds = false
                };
                AssetDatabase.AddObjectToAsset(value, controller);
                var low = ByteClip(controller, rendererPath, i, 0);
                var high = ByteClip(controller, rendererPath, i, 255);
                clips.Add(low);
                clips.Add(high);
                value.AddChild(low, 0);
                value.AddChild(high, 255);
                children[i] = new ChildMotion { motion = value, directBlendParameter = Weight, timeScale = 1 };
            }
            direct.children = children;
            show.motion = direct;
            display.defaultState = show;
            var visibility = AddVisibility(controller, new[] { rendererPath }, out AnimationClip[] visibilityClips);
            clips.AddRange(visibilityClips);
            ownedLayers = new[] { receive, display, visibility };
            bindingClips = clips.ToArray();
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
        }

        internal static AnimatorStateMachine AddVisibility(AnimatorController controller, IEnumerable<string> paths,
            out AnimationClip[] clips)
        {
            AddParameter(controller, Visible, AnimatorControllerParameterType.Bool);
            var machine = Layer(controller, "XRT Caption · Visibility");
            var shown = machine.AddState("Shown");
            var hidden = machine.AddState("Hidden");
            clips = new AnimationClip[2];
            for (int i = 0; i < clips.Length; i++)
            {
                var clip = new AnimationClip { name = i == 0 ? "Show captions" : "Hide captions", frameRate = 60 };
                foreach (string path in paths)
                    AnimationUtility.SetEditorCurve(clip,
                        EditorCurveBinding.FloatCurve(path, typeof(MeshRenderer), "m_Enabled"),
                        AnimationCurve.Constant(0, 1f / 60, i == 0 ? 1 : 0));
                AssetDatabase.AddObjectToAsset(clip, controller);
                var state = i == 0 ? shown : hidden;
                state.writeDefaultValues = false;
                state.motion = clip;
                clips[i] = clip;
            }
            machine.defaultState = hidden;
            var hide = shown.AddTransition(hidden);
            hide.AddCondition(AnimatorConditionMode.IfNot, 0, Visible);
            hide.duration = 0;
            hide.hasExitTime = false;
            var show = hidden.AddTransition(shown);
            show.AddCondition(AnimatorConditionMode.If, 0, Visible);
            show.duration = 0;
            show.hasExitTime = false;
            return machine;
        }

        internal static void AddBuffering(AnimatorController controller, AnimatorStateMachine receive, int byteCount)
        {
            AddParameter(controller, Buffered, AnimatorControllerParameterType.Bool);
            for (int i = 0; i < CaptionAssets.ByteCount; i++)
                AddParameter(controller, PendingPrefix + i, AnimatorControllerParameterType.Float);
            foreach (var transition in receive.anyStateTransitions)
                if (transition.destinationState != receive.defaultState)
                    transition.AddCondition(AnimatorConditionMode.IfNot, 0, Buffered);
            foreach (var transition in receive.defaultState.transitions)
                if (transition.destinationState == receive.defaultState)
                    receive.defaultState.RemoveTransition(transition);
            var reset = receive.defaultState.behaviours.OfType<VRCAvatarParameterDriver>().Single();
            reset.parameters.Add(new VRC_AvatarParameterDriver.Parameter { name = Buffered, value = 0 });
            for (int i = 0; i < CaptionAssets.ByteCount; i++)
                reset.parameters.Add(new VRC_AvatarParameterDriver.Parameter { name = PendingPrefix + i, value = 0 });
            for (int chunk = 0; chunk < CaptionAssets.ByteCount / byteCount; chunk++)
            {
                var state = ReceiveState(receive, "Stage " + chunk);
                var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                driver.localOnly = false;
                driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
                for (int offset = 0; offset < byteCount; offset++)
                    driver.parameters.Add(Copy(BytePrefix + offset, PendingPrefix + (chunk * byteCount + offset)));
                ReceiveTransition(receive, state, chunk).AddCondition(AnimatorConditionMode.If, 0, Buffered);
            }
            var begin = ReceiveState(receive, "Begin buffered captions");
            var mode = begin.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            mode.localOnly = false;
            mode.parameters = new List<VRC_AvatarParameterDriver.Parameter> {
                new VRC_AvatarParameterDriver.Parameter { name = Buffered, value = 1 }
            };
            ReceiveTransition(receive, begin, XrtCaptionProtocol.BeginPointer, false);
            var commit = ReceiveState(receive, "Show complete caption");
            var publish = commit.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            publish.localOnly = false;
            publish.parameters = Enumerable.Range(0, CaptionAssets.ByteCount)
                .Select(i => Copy(PendingPrefix + i, CachePrefix + i)).ToList();
            ReceiveTransition(receive, commit, XrtCaptionProtocol.CommitPointer, false);
        }

        private static AnimatorState ReceiveState(AnimatorStateMachine machine, string name)
        {
            var state = machine.AddState(name);
            state.writeDefaultValues = false;
            state.motion = machine.defaultState.motion;
            return state;
        }

        private static VRC_AvatarParameterDriver.Parameter Copy(string source, string target) =>
            new VRC_AvatarParameterDriver.Parameter { type = VRC_AvatarParameterDriver.ChangeType.Copy,
                source = source, name = target, convertRange = false };

        private static AnimatorStateTransition ReceiveTransition(AnimatorStateMachine machine, AnimatorState state, int pointer, bool repeatWhileHeld = true)
        {
            var transition = machine.AddAnyStateTransition(state);
            transition.AddCondition(AnimatorConditionMode.Equals, pointer, Pointer);
            transition.duration = 0;
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.None;
            if (!repeatWhileHeld) return transition;
            // Read again after a real frame interval; remote data can arrive after its address.
            var repeat = state.AddTransition(state);
            repeat.AddCondition(AnimatorConditionMode.Equals, pointer, Pointer);
            repeat.duration = 0;
            repeat.hasExitTime = true;
            repeat.exitTime = 1;
            repeat.canTransitionToSelf = true;
            return transition;
        }

        private static AnimatorStateMachine Layer(AnimatorController controller, string name)
        {
            var machine = new AnimatorStateMachine { name = name };
            AssetDatabase.AddObjectToAsset(machine, controller);
            controller.AddLayer(new AnimatorControllerLayer
            {
                name = name, stateMachine = machine, defaultWeight = 1,
                blendingMode = AnimatorLayerBlendingMode.Override
            });
            return machine;
        }

        private static AnimationClip ByteClip(AnimatorController controller, string path, int index, float value)
        {
            var clip = new AnimationClip { name = "Byte " + index + " = " + value, frameRate = 60 };
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(MeshRenderer), "material." + CaptionAssets.ByteProperty(index)),
                AnimationCurve.Constant(0, 1f / 60, value));
            AssetDatabase.AddObjectToAsset(clip, controller);
            return clip;
        }

        private static void AddParameter(AnimatorController controller, string name,
            AnimatorControllerParameterType type, int initial = 0)
        {
            foreach (var parameter in controller.parameters)
                if (parameter.name == name)
                    throw new InvalidOperationException("This FX controller already contains caption parameters.");
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = name, type = type, defaultInt = initial, defaultFloat = initial, defaultBool = initial != 0
            });
        }
    }
}
