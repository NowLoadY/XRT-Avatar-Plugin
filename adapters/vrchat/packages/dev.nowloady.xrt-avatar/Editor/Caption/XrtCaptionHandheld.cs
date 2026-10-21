using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using Record = XrtAvatarPlugin.Editor.XrtCaptionInstallationRecord;

namespace XrtAvatarPlugin.Editor
{
    internal static class XrtCaptionHandheld
    {
        private const string ActionName = "Change handheld caption";

        // Unity restores inline serializable classes as empty objects, even when saved as null.
        internal static bool IsEnabled(Record.TextObject item) => !string.IsNullOrEmpty(item.handheld?.parameter);

        internal static void Draw(VRCAvatarDescriptor avatar, MeshRenderer renderer, Record record, Action<Action> run)
        {
            if (renderer == null || record == null) return;
            var item = record.objects.FirstOrDefault(value => XrtCaptionInstaller.FindRenderer(avatar, value) == renderer);
            if (item == null) return;
            bool enabled = IsEnabled(item);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                bool next = EditorGUILayout.ToggleLeft(new GUIContent("Hold in left hand",
                    "Right hand aligns the board when close. Move, rotate and resize the caption in Scene view. Turn off to remove the effect."), enabled);
                if (next != enabled) run(() => Set(avatar, renderer, next));
            }
        }

        internal static void Set(VRCAvatarDescriptor avatar, MeshRenderer renderer, bool enabled)
        {
            var status = XrtCaptionInstaller.Inspect(avatar);
            if (!status.CanRemove || renderer == null || !status.Renderers.Contains(renderer))
                throw new InvalidOperationException("Select an installed caption object on this avatar.");
            var animator = avatar.GetComponent<Animator>();
            if (enabled && (animator == null || !animator.isHuman ||
                animator.GetBoneTransform(HumanBodyBones.LeftHand) == null ||
                animator.GetBoneTransform(HumanBodyBones.RightHand) == null))
                throw new InvalidOperationException("Handheld captions need a humanoid avatar with both hand bones.");
            XrtAvatarAssets.Edit(avatar, ActionName, (controls, working) =>
            {
                var item = working.objects.First(value => XrtCaptionInstaller.FindRenderer(avatar, value) == renderer);
                Undo.RecordObject(working, ActionName);
                if (enabled && !IsEnabled(item)) Add(avatar, animator, renderer, working, item);
                else if (!enabled) Remove(avatar, working, item);
                XrtCaptionInstaller.UpdatePlacement(avatar, working);
                EditorSceneManager.MarkSceneDirty(avatar.gameObject.scene);
                Selection.activeGameObject = renderer.gameObject;
                SceneView.RepaintAll();
            });
        }

        private static void Add(VRCAvatarDescriptor avatar, Animator animator, MeshRenderer renderer,
            Record record, Record.TextObject item)
        {
            string token = Guid.NewGuid().ToString("N");
            var left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var right = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var surface = renderer.transform;
            var held = new Record.Handheld {
                originalParent = Remember(avatar, surface.parent),
                parameter = "XRT/Handheld/" + token,
                contactTag = "XRTHandheld" + token
            };
            var carry = Create("XRT Left Hand " + token.Substring(0, 8), left);
            carry.position = PalmCenter(animator, true);
            carry.rotation = surface.rotation;
            var stable = Undo.AddComponent<VRCRotationConstraint>(carry.gameObject);
            stable.Sources.Add(new VRCConstraintSource(avatar.transform, 1));
            stable.RotationAtRest = carry.localEulerAngles;
            stable.RotationOffset = (Quaternion.Inverse(avatar.transform.rotation) * carry.rotation).eulerAngles;
            stable.GlobalWeight = 1;
            stable.IsActive = stable.Locked = true;
            stable.ApplyConfigurationChanges();

            var rightGrip = Create("XRT Right Grip " + token.Substring(0, 8), right);
            rightGrip.position = PalmCenter(animator, false);
            var sender = Undo.AddComponent<VRCContactSender>(rightGrip.gameObject);
            sender.rootTransform = rightGrip;
            sender.shapeType = ContactBase.ShapeType.Sphere;
            sender.radius = .006f;
            sender.localOnly = false;
            sender.collisionTags = new List<string> { held.contactTag };
            sender.ApplyConfigurationChanges();

            var aimTransform = Create("Two Hand Alignment", carry);
            var aim = Undo.AddComponent<VRCAimConstraint>(aimTransform.gameObject);
            aim.Sources.Add(new VRCConstraintSource(rightGrip, 1));
            aim.WorldUp = VRCConstraintBase.WorldUpType.ObjectRotationUp;
            aim.WorldUpTransform = carry;
            aim.RotationAtRest = aim.RotationOffset = Vector3.zero;
            aim.GlobalWeight = 0;
            aim.IsActive = aim.Locked = true;

            var receiver = Undo.AddComponent<VRCContactReceiver>(carry.gameObject);
            receiver.rootTransform = carry;
            receiver.shapeType = ContactBase.ShapeType.Sphere;
            receiver.receiverType = ContactReceiver.ReceiverType.Proximity;
            receiver.parameter = held.parameter;
            receiver.allowSelf = true;
            receiver.allowOthers = false;
            receiver.localOnly = false;
            receiver.collisionTags = new List<string> { held.contactTag };

            // Keep the user's surface transform, dimensions and material settings.
            Undo.SetTransformParent(surface, aimTransform, ActionName);
            held.carry = Remember(avatar, carry);
            held.aim = Remember(avatar, aimTransform);
            held.rightGrip = Remember(avatar, rightGrip);
            item.handheld = held;
            BuildAlignment(record.installedController, held, aimTransform, avatar.transform);
        }

        internal static void Refresh(VRCAvatarDescriptor avatar, Record.TextObject item, MeshRenderer renderer, bool authoring)
        {
            var held = item.handheld;
            if (!IsEnabled(item)) return;
            if (authoring && (held.carryClip == null || held.alignedClip == null)) return;
            ResolveRig(avatar, held, out var carry, out var aimTransform, out var right);
            var animator = avatar.GetComponent<Animator>();
            if (renderer == null || carry == null || aimTransform == null || right == null || animator == null ||
                carry.parent != animator.GetBoneTransform(HumanBodyBones.LeftHand) ||
                right.parent != animator.GetBoneTransform(HumanBodyBones.RightHand) ||
                !renderer.transform.IsChildOf(aimTransform))
            {
                if (authoring) return; // Keep removal available after a manual hierarchy edit.
                throw new InvalidOperationException("Restore the handheld caption hierarchy, or turn off Hold in left hand before uploading.");
            }
            var aim = aimTransform.GetComponent<VRCAimConstraint>();
            var receiver = carry.GetComponent<VRCContactReceiver>();
            if (aim == null || receiver == null || right.GetComponent<VRCContactSender>() == null ||
                carry.GetComponent<VRCRotationConstraint>() == null)
            {
                if (authoring) return;
                throw new InvalidOperationException("Restore the handheld caption components, or turn off Hold in left hand.");
            }

            var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            var bounds = mesh == null ? new Bounds(Vector3.zero, Vector3.one) : mesh.bounds;
            float oppositeEdge = renderer.transform.InverseTransformPoint(carry.position).x > bounds.center.x ? -1 : 1;
            Vector3 edge = bounds.center + Vector3.right * (bounds.extents.x * oppositeEdge);
            Vector3 reach = aimTransform.InverseTransformPoint(renderer.transform.TransformPoint(edge));
            Vector3 axis = reach.sqrMagnitude < .000001f ? Vector3.left : reach.normalized;
            Vector3 up = Vector3.ProjectOnPlane(aimTransform.InverseTransformDirection(renderer.transform.forward), axis);
            if (up.sqrMagnitude < .000001f)
                up = Vector3.ProjectOnPlane(aimTransform.InverseTransformDirection(renderer.transform.up), axis);
            up.Normalize();
            float radius = Mathf.Max(.01f, reach.magnitude * 1.25f);
            if (aim.AimAxis != axis || aim.UpAxis != up || aim.WorldUpVector != up || !Mathf.Approximately(receiver.radius, radius))
            {
                if (authoring) Undo.RecordObjects(new UnityEngine.Object[] { aim, receiver }, ActionName);
                aim.AimAxis = axis;
                aim.UpAxis = aim.WorldUpVector = up;
                receiver.radius = radius;
                aim.ApplyConfigurationChanges();
                receiver.ApplyConfigurationChanges();
                if (authoring)
                {
                    EditorUtility.SetDirty(aim);
                    EditorUtility.SetDirty(receiver);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(aim);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(receiver);
                }
            }
            string path = AnimationUtility.CalculateTransformPath(aimTransform, avatar.transform);
            UpdateClip(held.carryClip, path, 0, authoring);
            UpdateClip(held.alignedClip, path, 1, authoring);
            if (authoring)
            {
                held.carry = Remember(avatar, carry);
                held.aim = Remember(avatar, aimTransform);
                held.rightGrip = Remember(avatar, right);
                var original = Resolve(avatar, held.originalParent);
                if (original != null) held.originalParent = Remember(avatar, original);
            }
        }

        internal static void Remove(VRCAvatarDescriptor avatar, Record record, Record.TextObject item)
        {
            var held = item.handheld;
            if (!IsEnabled(item)) return;
            ResolveRig(avatar, held, out var carry, out var aim, out var right);
            var renderer = XrtCaptionInstaller.FindRenderer(avatar, item);
            if (renderer != null && ((carry != null && renderer.transform.IsChildOf(carry)) ||
                (aim != null && renderer.transform.IsChildOf(aim))))
            {
                var parent = Resolve(avatar, held.originalParent) ?? avatar.transform;
                if (parent == renderer.transform || parent.IsChildOf(renderer.transform) ||
                    (carry != null && parent.IsChildOf(carry))) parent = avatar.transform;
                Undo.SetTransformParent(renderer.transform, parent, ActionName);
            }
            RemoveNode(aim, typeof(VRCAimConstraint));
            RemoveNode(carry, typeof(VRCRotationConstraint), typeof(VRCContactReceiver));
            RemoveNode(right, typeof(VRCContactSender));
            var controller = record.installedController;
            Undo.RecordObject(controller, ActionName);
            controller.layers = controller.layers.Where(layer => layer.stateMachine != held.layer).ToArray();
            controller.parameters = controller.parameters.Where(parameter => parameter.name != held.parameter).ToArray();
            EditorUtility.SetDirty(controller);
            item.handheld = null;
        }

        private static void RemoveNode(Transform node, params Type[] owned)
        {
            if (node == null) return;
            // User-added components and children stay intact, even on generated nodes.
            foreach (var type in owned)
            {
                var component = node.GetComponent(type);
                if (component != null) Undo.DestroyObjectImmediate(component);
            }
            if (node.childCount == 0 && node.GetComponents<Component>().All(component => component is Transform))
                Undo.DestroyObjectImmediate(node.gameObject);
        }

        private static void BuildAlignment(AnimatorController controller, Record.Handheld held, Transform aim, Transform avatar)
        {
            Undo.RecordObject(controller, ActionName);
            controller.AddParameter(held.parameter, AnimatorControllerParameterType.Float);
            var machine = new AnimatorStateMachine { name = "XRT Handheld " + held.parameter.Substring("XRT/Handheld/".Length, 8) };
            AssetDatabase.AddObjectToAsset(machine, controller);
            held.layer = machine;
            var state = machine.AddState("Follow hands");
            state.writeDefaultValues = false;
            var blend = new BlendTree { name = "Hand proximity", blendType = BlendTreeType.Simple1D,
                blendParameter = held.parameter, useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, controller);
            held.carryClip = WeightClip(controller, aim, avatar, 0);
            held.alignedClip = WeightClip(controller, aim, avatar, 1);
            // Fade at both distance boundaries; do not aim when hands coincide.
            blend.AddChild(held.carryClip, 0);
            blend.AddChild(held.alignedClip, .15f);
            blend.AddChild(held.alignedClip, .75f);
            blend.AddChild(held.carryClip, .92f);
            blend.AddChild(held.carryClip, 1);
            state.motion = blend;
            machine.defaultState = state;
            controller.AddLayer(new AnimatorControllerLayer { name = machine.name, stateMachine = machine,
                defaultWeight = 1, blendingMode = AnimatorLayerBlendingMode.Override });
            EditorUtility.SetDirty(controller);
        }

        private static AnimationClip WeightClip(AnimatorController controller, Transform aim, Transform avatar, float weight)
        {
            var clip = new AnimationClip { name = weight == 0 ? "Carry caption" : "Align caption", frameRate = 60 };
            UpdateClip(clip, AnimationUtility.CalculateTransformPath(aim, avatar), weight, false);
            AssetDatabase.AddObjectToAsset(clip, controller);
            return clip;
        }

        private static void UpdateClip(AnimationClip clip, string path, float weight, bool authoring)
        {
            if (clip == null) throw new InvalidOperationException("Restore the handheld caption animation, or turn off Hold in left hand.");
            var bindings = AnimationUtility.GetCurveBindings(clip).Where(binding =>
                binding.type == typeof(VRCAimConstraint) && binding.propertyName == "GlobalWeight").ToArray();
            if (bindings.Length == 1 && bindings[0].path == path) return;
            if (authoring) Undo.RecordObject(clip, ActionName);
            foreach (var binding in bindings) AnimationUtility.SetEditorCurve(clip, binding, null);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(VRCAimConstraint), "GlobalWeight"),
                AnimationCurve.Constant(0, 1, weight));
            EditorUtility.SetDirty(clip);
        }

        private static Record.SceneNode Remember(VRCAvatarDescriptor avatar, Transform node) => new Record.SceneNode {
            globalId = GlobalObjectId.GetGlobalObjectIdSlow(node).ToString(),
            path = AnimationUtility.CalculateTransformPath(node, avatar.transform)
        };

        private static void ResolveRig(VRCAvatarDescriptor avatar, Record.Handheld held,
            out Transform carry, out Transform aim, out Transform right)
        {
            carry = Resolve(avatar, held.carry, node => node.GetComponent<VRCContactReceiver>()?.parameter == held.parameter);
            var baseTransform = carry;
            aim = Resolve(avatar, held.aim, node => baseTransform != null &&
                node.GetComponent<VRCAimConstraint>()?.WorldUpTransform == baseTransform);
            right = Resolve(avatar, held.rightGrip, node =>
                node.GetComponent<VRCContactSender>()?.collisionTags?.Contains(held.contactTag) == true);
        }

        private static Transform Resolve(VRCAvatarDescriptor avatar, Record.SceneNode node, Func<Transform, bool> match = null)
        {
            if (node == null) return null;
            if (GlobalObjectId.TryParse(node.globalId, out var id))
            {
                var transform = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Transform;
                if (transform != null && (transform == avatar.transform || transform.IsChildOf(avatar.transform))) return transform;
            }
            var candidate = node.path == null ? null : node.path == "" ? avatar.transform : avatar.transform.Find(node.path);
            if (candidate != null && (match == null || match(candidate))) return candidate;
            if (match == null) return null;
            // Build clones have new scene identities. The private contact parameter/tag
            // also identifies nodes after a rename without trusting an occupied old path.
            var matches = avatar.GetComponentsInChildren<Transform>(true).Where(match).Take(2).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static Transform Create(string name, Transform parent)
        {
            var item = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(item, ActionName);
            Undo.SetTransformParent(item.transform, parent, ActionName);
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            item.transform.localScale = Vector3.one;
            return item.transform;
        }

        private static Vector3 PalmCenter(Animator animator, bool left)
        {
            var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            var knuckle = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            return knuckle == null ? hand.position : Vector3.Lerp(hand.position, knuckle.position, .55f);
        }
    }
}
