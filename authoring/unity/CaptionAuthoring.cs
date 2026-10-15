using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace XrtAvatarPlugin.Presentation
{
    /// <summary>Common caption preset selection and preview text for Unity authoring.</summary>
    public static class CaptionAuthoring
    {
        public static int ConfigureMaterial(Material material, string preset, int columns = 0)
        {
            if (columns == 0) columns = string.Equals(preset, "portrait", StringComparison.OrdinalIgnoreCase) ? 8 : 32;
            material.SetFloat("_Columns", columns);
            material.SetColor("_BackgroundColor", string.Equals(preset, "board", StringComparison.OrdinalIgnoreCase)
                ? Color.clear : new Color(.98f, .98f, .98f, .92f));
            return columns;
        }

        public static Vector3 Dimensions(int columns) => columns == 8
            ? new Vector3(.24f, .60f, 1) : new Vector3(.64f, .10f, 1);

        public static void SetPreview(Material material, string text, int columns)
        {
            if (columns < 8 || columns > 32) throw new ArgumentOutOfRangeException(nameof(columns));
            var glyphs = PreviewGlyphs(text, columns);
            for (int i = 0; i < CaptionAssets.SlotCount; i++)
            {
                int code = i < glyphs.Count ? glyphs[i] : 0;
                material.SetFloat(CaptionAssets.ByteProperty(i * 2), code >> 8);
                material.SetFloat(CaptionAssets.ByteProperty(i * 2 + 1), code & 255);
            }
        }

        private static List<char> PreviewGlyphs(string text, int columns)
        {
            var input = new StringBuilder();
            string value = text ?? string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsHighSurrogate(character) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                { input.Append('\uFFFD'); i++; }
                else if (char.IsSurrogate(character)) input.Append('\uFFFD');
                else input.Append(char.IsControl(character) && character != '\n' ? ' ' : character);
            }
            var glyphs = new List<char>();
            string[] lines = input.ToString().Normalize(NormalizationForm.FormC).Trim().Split('\n');
            bool previousEmpty = false;
            for (int lineIndex = 0; lineIndex < lines.Length && glyphs.Count <= CaptionAssets.SlotCount; lineIndex++)
            {
                if (lineIndex > 0)
                {
                    int remaining = columns - glyphs.Count % columns;
                    if (remaining != columns || previousEmpty) Pad(glyphs, remaining);
                }
                string line = lines[lineIndex].Trim();
                previousEmpty = line.Length == 0;
                if (previousEmpty) continue;
                string[] words = Regex.Split(line, @"\s+");
                for (int wordIndex = 0; wordIndex < words.Length && glyphs.Count <= CaptionAssets.SlotCount; wordIndex++)
                {
                    string word = words[wordIndex];
                    int column = glyphs.Count % columns;
                    if (wordIndex > 0 && column != 0)
                    {
                        if (word.Length <= columns && column + word.Length + 1 > columns) Pad(glyphs, columns - column);
                        else glyphs.Add(' ');
                    }
                    foreach (char character in word)
                    {
                        if (glyphs.Count > CaptionAssets.SlotCount) break;
                        glyphs.Add(character);
                    }
                }
            }
            if (glyphs.Count > CaptionAssets.SlotCount)
            {
                glyphs.RemoveRange(CaptionAssets.SlotCount, glyphs.Count - CaptionAssets.SlotCount);
                glyphs[CaptionAssets.SlotCount - 1] = '\u2026';
            }
            return glyphs;
        }
        private static void Pad(List<char> glyphs, int count)
        {
            for (int i = 0; i < count && glyphs.Count <= CaptionAssets.SlotCount; i++) glyphs.Add('\0');
        }

        public static MeshRenderer DrawObjectPicker(MeshRenderer[] renderers, ref int selectedIndex)
        {
            if (renderers == null || renderers.Length == 0) { selectedIndex = -1; return null; }
            selectedIndex = Mathf.Clamp(selectedIndex, 0, renderers.Length - 1);
            var names = new string[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                names[i] = renderers[i] == null ? "Missing text object" : renderers[i].name;
            selectedIndex = EditorGUILayout.Popup("Text object", selectedIndex, names);
            return renderers[selectedIndex];
        }

        // Remove the generated text only; a user's replacement renderer or prop stays intact.
        public static void RemoveObject(MeshRenderer renderer, Mesh ownedMesh, Material ownedMaterial,
            params Component[] ownedComponents)
        {
            if (renderer == null) return;
            var target = renderer.gameObject;
            var filter = target.GetComponent<MeshFilter>();
            var materials = renderer.sharedMaterials;
            bool ownsSurface = ownedMesh != null && ownedMaterial != null &&
                filter != null && filter.sharedMesh == ownedMesh &&
                materials.Length == 1 && materials[0] == ownedMaterial;
            ownedComponents = ownedComponents ?? Array.Empty<Component>();
            bool wholeObject = ownsSurface && target.transform.childCount == 0;
            foreach (var component in target.GetComponents<Component>())
                if (component == null || (component != target.transform && component != filter &&
                    component != renderer && Array.IndexOf(ownedComponents, component) < 0)) wholeObject = false;
            if (wholeObject) { Undo.DestroyObjectImmediate(target); return; }
            foreach (var component in ownedComponents)
                if (component != null && component.gameObject == target && component != target.transform &&
                    component != renderer && component != filter) Undo.DestroyObjectImmediate(component);
            if (ownsSurface)
            {
                Undo.DestroyObjectImmediate(renderer);
                Undo.DestroyObjectImmediate(filter);
            }
        }

        public static int DrawPresets(int selectedPreset, int columns = 0)
        {
            if (columns == 8 && selectedPreset == 0) selectedPreset = 1;
            else if (columns == 32 && selectedPreset == 1) selectedPreset = 0;
            else if (columns == 16) selectedPreset = 2;
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < 3; i++)
                {
                    int value = i;
                    Rect area = GUILayoutUtility.GetRect(80, 66, GUILayout.ExpandWidth(true));
                    bool selected = selectedPreset == value;
                    using (new EditorGUI.DisabledScope(columns != 0 &&
                        ((value == 0 && columns != 32) || (value == 1 && columns != 8))))
                        if (GUI.Toggle(area, selected, GUIContent.none, "Button") && !selected) selectedPreset = value;
                    var shape = value == 1
                        ? new Rect(area.center.x - 8, area.y + 9, 16, 25)
                        : new Rect(area.center.x - 20, area.y + 15, 40, 15);
                    Handles.BeginGUI();
                    Handles.color = EditorGUIUtility.isProSkin ? new Color(.8f, .8f, .8f) : new Color(.35f, .4f, .42f);
                    Handles.DrawAAPolyLine(1.5f, new Vector3(shape.x, shape.y), new Vector3(shape.xMax, shape.y),
                        new Vector3(shape.xMax, shape.yMax), new Vector3(shape.x, shape.yMax), new Vector3(shape.x, shape.y));
                    for (int row = 0; row < (value == 1 ? 4 : 2); row++)
                    {
                        float y = shape.y + 5 + row * 4;
                        Handles.DrawAAPolyLine(1, new Vector3(shape.x + 4, y), new Vector3(shape.xMax - 4, y));
                    }
                    if (value == 2)
                        Handles.DrawAAPolyLine(1.5f, new Vector3(shape.x + 7, shape.yMax + 3),
                            new Vector3(shape.x + 12, shape.yMax - 2), new Vector3(shape.x + 18, shape.yMax + 3));
                    Handles.EndGUI();
                    GUI.Label(new Rect(area.x, area.yMax - 23, area.width, 18), new[] { "Horizontal", "Portrait", "Board" }[value],
                        new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
                }
            }
            return selectedPreset;
        }

    }

    /// <summary>Renders the installed material with its real mesh and bundled font.</summary>
    public sealed class CaptionPreview : IDisposable
    {
        private PreviewRenderUtility preview;
        private Material material;
        private Material sourceMaterial;
        private Mesh mesh;

        public void Draw(Rect rect, string preset, string text, Material installedMaterial = null, int columns = 0)
        {
            if (Event.current.type != EventType.Repaint) return;
            try
            {
                if (preview == null) preview = new PreviewRenderUtility();
                if (mesh == null) mesh = CaptionAssets.CreateMesh();
                if (material == null || sourceMaterial != installedMaterial)
                {
                    if (material != null) UnityEngine.Object.DestroyImmediate(material);
                    material = CaptionAssets.CreateMaterial();
                    sourceMaterial = installedMaterial;
                }
                columns = CaptionAuthoring.ConfigureMaterial(material, preset, columns);
                if (installedMaterial != null)
                {
                    material.CopyPropertiesFromMaterial(installedMaterial);
                    columns = Mathf.Clamp(Mathf.RoundToInt(material.GetFloat("_Columns")), 8, 32);
                }
                CaptionAuthoring.SetPreview(material, text, columns);
                preview.BeginPreview(rect, GUIStyle.none);
                var camera = preview.camera;
                camera.orthographic = true;
                Vector3 size = CaptionAuthoring.Dimensions(columns);
                camera.aspect = rect.width / rect.height;
                camera.orthographicSize = Mathf.Max(size.y * .5f, size.x * .5f / camera.aspect) * 1.15f;
                camera.transform.position = new Vector3(0, 0, -2);
                camera.transform.rotation = Quaternion.identity;
                camera.clearFlags = CameraClearFlags.Color;
                camera.backgroundColor = new Color(.91f, .94f, .94f);
                preview.DrawMesh(mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, size), material, 0);
                camera.Render();
                GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
            }
            catch (Exception failure)
            {
                // A failed preview must not leave a render target active for the editor.
                preview?.Cleanup();
                preview = null;
                GUI.Label(rect, failure.Message, EditorStyles.wordWrappedMiniLabel);
            }
        }

        public void Dispose()
        {
            preview?.Cleanup();
            preview = null;
            if (material != null) UnityEngine.Object.DestroyImmediate(material);
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            material = null;
            sourceMaterial = null;
            mesh = null;
        }
    }
}
