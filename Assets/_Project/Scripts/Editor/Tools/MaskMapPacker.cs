using System.IO;
using UnityEditor;
using UnityEngine;

namespace NoWayOut.Editor.Tools
{
    internal static class MaskMapPacker
    {
        private const string MENU_PATH = "Tools/No Way Out/Pack HDRP Mask Map (from ARM)";
        private const string ARM_SUFFIX = "_ARM";
        private const string MASK_SUFFIX = "_Mask";
        private const string MASK_EXTENSION = ".png";
        private const byte DETAIL_MASK_VALUE = 255;


        [MenuItem(MENU_PATH)]
        private static void PackSelected()
        {
            foreach (Texture2D texture in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                Pack(AssetDatabase.GetAssetPath(texture));
            }
        }


        [MenuItem(MENU_PATH, true)]
        private static bool ValidatePackSelected()
        {
            foreach (Texture2D texture in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                if (IsArmTexture(AssetDatabase.GetAssetPath(texture)))
                {
                    return true;
                }
            }

            return false;
        }


        public static string Pack(string armPath)
        {
            if (!IsArmTexture(armPath))
            {
                return null;
            }

            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            source.LoadImage(File.ReadAllBytes(armPath));
            Color32[] pixels = source.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 arm = pixels[i];
                pixels[i] = new Color32(arm.b, arm.r, DETAIL_MASK_VALUE, (byte)(byte.MaxValue - arm.g));
            }

            Texture2D mask = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            mask.SetPixels32(pixels);
            mask.Apply();

            string name = Path.GetFileNameWithoutExtension(armPath);
            string maskName = name.Substring(0, name.Length - ARM_SUFFIX.Length) + MASK_SUFFIX + MASK_EXTENSION;
            string maskPath = Path.GetDirectoryName(armPath).Replace('\\', '/') + "/" + maskName;
            File.WriteAllBytes(maskPath, mask.EncodeToPNG());

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(mask);
            AssetDatabase.ImportAsset(maskPath);
            return maskPath;
        }


        private static bool IsArmTexture(string path)
        {
            return Path.GetFileNameWithoutExtension(path).EndsWith(ARM_SUFFIX);
        }
    }
}
