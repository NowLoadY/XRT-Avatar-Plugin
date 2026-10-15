using System;
using UnityEditor;
using UnityEngine;

namespace XrtAvatarPlugin.Presentation
{
    /// <summary>Editor resource access for the shared, script-free caption surface.</summary>
    public static class CaptionAssets
    {
        public const int SlotCount = 128;
        public const int ByteCount = SlotCount * 2;
        public const string ShaderGuid = "aff880bce5af5760a11fa956f48b50c2";
        public const string AtlasGuid = "c2bf61d9a8c951f897c60df4a23dfe31";
        public const string MapGuid = "ea26506e8856587fb57e237e8a2c689a";

        public static string ByteProperty(int index)
        {
            if (index < 0 || index >= ByteCount) throw new ArgumentOutOfRangeException(nameof(index));
            return "_Byte" + index;
        }

        public static Shader LoadShader() => Load<Shader>(ShaderGuid);

        public static Material CreateMaterial()
        {
            var material = new Material(LoadShader()) { name = "XRT Caption" };
            material.SetTexture("_Atlas", Load<Texture2D>(AtlasGuid));
            material.SetTexture("_GlyphMap", Load<Texture2D>(MapGuid));
            return material;
        }

        /// <summary>One renderer and material; UV1 identifies each glyph for vertex layout.</summary>
        public static Mesh CreateMesh()
        {
            const int quads = SlotCount + 1;
            var vertices = new Vector3[quads * 4];
            var uv = new Vector2[vertices.Length];
            var cells = new Vector2[vertices.Length];
            var triangles = new int[quads * 6];
            var corners = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            for (int quad = 0; quad < quads; quad++)
            {
                int start = quad * 4;
                for (int corner = 0; corner < 4; corner++)
                {
                    vertices[start + corner] = corners[corner] - Vector2.one * 0.5f;
                    uv[start + corner] = corners[corner];
                    cells[start + corner] = new Vector2(quad - 1, 0);
                }
                int triangle = quad * 6;
                triangles[triangle] = start;
                triangles[triangle + 1] = start + 2;
                triangles[triangle + 2] = start + 1;
                triangles[triangle + 3] = start;
                triangles[triangle + 4] = start + 3;
                triangles[triangle + 5] = start + 2;
            }
            var mesh = new Mesh { name = "XRT Caption" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.uv2 = cells;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1.1f, 1.1f, 0.01f));
            return mesh;
        }

        private static T Load<T>(string guid) where T : UnityEngine.Object
        {
            var resource = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (resource == null) throw new InvalidOperationException("Reimport XRT-Avatar-Plugin to restore its caption resources.");
            return resource;
        }
    }

    /// <summary>Texture samples contain distances and addresses, never display colors.</summary>
    internal sealed class CaptionTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            bool atlas = guid == CaptionAssets.AtlasGuid;
            if (!atlas && guid != CaptionAssets.MapGuid) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = atlas ? 4096 : 256;
            importer.filterMode = atlas ? FilterMode.Bilinear : FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = importer.GetDefaultPlatformTextureSettings();
            settings.maxTextureSize = importer.maxTextureSize;
            settings.format = atlas ? TextureImporterFormat.R8 : TextureImporterFormat.RGBA32;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
