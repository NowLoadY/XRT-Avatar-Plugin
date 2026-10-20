using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace XrtAvatarPlugin.Editor
{
    internal sealed class XrtLocalTestWindow : EditorWindow
    {
        [SerializeField] private VRCAvatarDescriptor avatar;
        [SerializeField] private bool pending, enteredPlay;
        [SerializeField] private string avatarPath;
        private GesturePreview preview;
        private LocalOscQuery connection;
        private string error;
        private double nextRepaint;
        private static readonly string[] States = { "Waiting for XRTranslate", "Ready", "Starting", "Translating", "Stopping", "Error" };

        [MenuItem("Tools/XRT-Avatar-Plugin/Local Test")]
        internal static void Open()
        {
            bool existing = HasOpenInstances<XrtLocalTestWindow>();
            var window = GetWindow<XrtLocalTestWindow>("XRT · Local Test");
            window.minSize = new Vector2(360, 190);
            if (!existing) window.position = new Rect(window.position.x, window.position.y, 380, 210);
            if (Selection.activeGameObject != null)
                window.avatar = Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            OnSelectionChange();
        }
        private void OnSelectionChange()
        {
            if (connection != null || pending || Selection.activeGameObject == null) return;
            var selected = Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
            if (selected != null) avatar = selected;
            Repaint();
        }
        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeReload;
            Stop(false);
        }
        private void BeforeReload() => Stop(false);
        private void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) pending = enteredPlay = false;
            if (state == PlayModeStateChange.EnteredPlayMode && pending)
                EditorApplication.delayCall += StartConnection;
            if (state == PlayModeStateChange.ExitingPlayMode)
            { pending = false; Stop(false); }
        }
        private void Tick()
        {
            if (pending && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
                StartConnection();
            if (connection == null) return;
            try
            {
                if (!EditorApplication.isPlaying || !preview.IsValid) { Stop(false); return; }
                connection.Tick();
                if (EditorApplication.timeSinceStartup >= nextRepaint)
                {
                    nextRepaint = EditorApplication.timeSinceStartup + .25;
                    Repaint();
                }
            }
            catch (Exception failure) { error = failure.GetBaseException().Message; Stop(false); }
        }
        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(connection != null || pending))
                avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);
            GUILayout.Space(8);
            if (connection != null)
            {
                string details = "OSCQuery " + connection.Endpoint + "\n" + connection.Writes +
                    " writes · " + connection.Snapshots + " snapshots";
                GUILayout.Label(new GUIContent(States[Mathf.Clamp((int)connection.Read(XrtAvatarProtocol.Status), 0, States.Length - 1)], details),
                    EditorStyles.boldLabel);
                GUILayout.Label("Expressions → XRT → Translation", EditorStyles.wordWrappedLabel);
            }
            else
                GUILayout.Label(pending ? "Entering Play mode…" : "Enable Game Plugins → VRChat in XRTranslate, then start the test.",
                    EditorStyles.wordWrappedLabel);
            if (!GesturePreview.Available)
                EditorGUILayout.HelpBox("Install Gesture Manager in ALCOM to test the expression menu.", MessageType.Info);
            GUILayout.Space(8);
            using (new EditorGUI.DisabledScope(!GesturePreview.Available || avatar == null || pending))
                if (GUILayout.Button(connection != null ? "Stop test" : "Start test"))
                {
                    if (connection != null) Stop(true);
                    else if (EditorApplication.isPlaying) StartConnection();
                    else
                    {
                        error = null;
                        avatarPath = AnimationUtility.CalculateTransformPath(avatar.transform, null);
                        pending = enteredPlay = true;
                        EditorApplication.EnterPlaymode();
                    }
                }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        }
        private void StartConnection()
        {
            if (!pending && !EditorApplication.isPlaying) return;
            if (!pending && connection != null) return;
            pending = false;
            try
            {
                if (EditorApplication.isPlaying && (avatar == null || !avatar.gameObject.scene.IsValid()))
                {
                    var matches = UnityEngine.Object.FindObjectsOfType<VRCAvatarDescriptor>(true)
                        .Where(candidate => candidate.gameObject.scene.IsValid() &&
                            AnimationUtility.CalculateTransformPath(candidate.transform, null) == avatarPath).ToArray();
                    if (matches.Length == 1) avatar = matches[0];
                }
                if (!EditorApplication.isPlaying || avatar == null) throw new InvalidOperationException("Select a scene avatar to test.");
                preview = new GesturePreview(avatar);
                connection = new LocalOscQuery(preview.Snapshot, preview.Write);
                Selection.activeObject = preview.Menu;
                error = null;
            }
            catch (Exception failure) { error = failure.GetBaseException().Message; Stop(false); }
        }
        private void Stop(bool exitPlay)
        {
            connection?.Dispose();
            connection = null;
            preview?.Dispose();
            preview = null;
            if (exitPlay && enteredPlay && EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            if (!pending) enteredPlay = false;
        }
    }
}
