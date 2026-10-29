using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NoWayOut.Editor.Tools
{
    internal static class SkyReflectionBaker
    {
        private const string MENU_PATH = "Tools/No Way Out/Bake Sky Reflection (Active Scene)";
        private const string GRADIENT_SKY_SHADER = "NoWayOut/Gradient Sky";
        private const string OUTPUT_FOLDER = "Assets/_Project/Art/Textures/Environment/";
        private const string OUTPUT_PREFIX = "T_SkyReflection_";
        private const string OUTPUT_EXTENSION = ".asset";
        private const string DIALOG_TITLE = "Bake Sky Reflection";
        private const string DIALOG_MESSAGE = "The active scene's skybox must use " + GRADIENT_SKY_SHADER + ".";
        private const string DIALOG_OK = "OK";
        private const int SIZE = 64;
        private const int FACE_COUNT = 6;

        private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
        private static readonly int MiddleColorId = Shader.PropertyToID("_MiddleColor");
        private static readonly int BottomColorId = Shader.PropertyToID("_BottomColor");
        private static readonly int GradientDiffusionId = Shader.PropertyToID("_GradientDiffusion");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");


        [MenuItem(MENU_PATH)]
        private static void Bake()
        {
            Material sky = RenderSettings.skybox;
            if (sky == null || sky.shader.name != GRADIENT_SKY_SHADER)
            {
                EditorUtility.DisplayDialog(DIALOG_TITLE, DIALOG_MESSAGE, DIALOG_OK);
                return;
            }

            Cubemap cubemap = new Cubemap(SIZE, TextureFormat.RGBAHalf, true);
            for (int mip = 0; mip < cubemap.mipmapCount; mip++)
            {
                for (int face = 0; face < FACE_COUNT; face++)
                {
                    FillFace(cubemap, sky, (CubemapFace)face, mip);
                }
            }

            cubemap.Apply(false);
            Scene scene = SceneManager.GetActiveScene();
            Cubemap asset = Save(cubemap, OUTPUT_FOLDER + OUTPUT_PREFIX + scene.name + OUTPUT_EXTENSION);

            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = asset;
            EditorSceneManager.MarkSceneDirty(scene);
        }


        private static void FillFace(Cubemap cubemap, Material sky, CubemapFace face, int mip)
        {
            int size = Mathf.Max(1, SIZE >> mip);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    pixels[y * size + x] = EvaluateSky(sky, GetDirection(face, u, v));
                }
            }

            cubemap.SetPixels(pixels, face, mip);
        }


        private static Vector3 GetDirection(CubemapFace face, float u, float v)
        {
            switch (face)
            {
                case CubemapFace.PositiveX:
                    return new Vector3(1f, -v, -u).normalized;
                case CubemapFace.NegativeX:
                    return new Vector3(-1f, -v, u).normalized;
                case CubemapFace.PositiveY:
                    return new Vector3(u, 1f, v).normalized;
                case CubemapFace.NegativeY:
                    return new Vector3(u, -1f, -v).normalized;
                case CubemapFace.PositiveZ:
                    return new Vector3(u, -v, 1f).normalized;
                default:
                    return new Vector3(-u, -v, -1f).normalized;
            }
        }


        private static Color EvaluateSky(Material sky, Vector3 direction)
        {
            float height = direction.y * sky.GetFloat(GradientDiffusionId);
            Color color = Color.Lerp(sky.GetColor(MiddleColorId).linear, sky.GetColor(TopColorId).linear, Mathf.Clamp01(height));
            color = Color.Lerp(color, sky.GetColor(BottomColorId).linear, Mathf.Clamp01(-height));
            color *= sky.GetFloat(IntensityId);
            color.a = 1f;
            return color;
        }


        private static Cubemap Save(Cubemap cubemap, string path)
        {
            Cubemap existing = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(cubemap, path);
                return cubemap;
            }

            EditorUtility.CopySerialized(cubemap, existing);
            Object.DestroyImmediate(cubemap);
            AssetDatabase.SaveAssets();
            return existing;
        }
    }
}
