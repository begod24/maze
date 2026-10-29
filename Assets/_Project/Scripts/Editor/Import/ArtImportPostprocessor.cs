using System.IO;
using UnityEditor;
using UnityEngine;

namespace NoWayOut.Editor.Import
{
    internal sealed class ArtImportPostprocessor : AssetPostprocessor
    {
        private const string ART_ROOT = "Assets/_Project/Art/";
        private const string ENVIRONMENT_ROOT = ART_ROOT + "Environment/";
        private const string CHARACTERS_ROOT = ART_ROOT + "Characters/";
        private const string ENTITIES_ROOT = ART_ROOT + "Entities/";
        private const string MATERIAL_PREFIX = "M_";
        private const string NORMAL_SUFFIX = "_Normal";
        private const string MASK_SUFFIX = "_Mask";
        private const string ARM_SUFFIX = "_ARM";
        private const string COLLIDER_PREFIX = "COL_";
        private const int DEFAULT_MAX_TEXTURE_SIZE = 2048;


        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ART_ROOT))
            {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.bakeAxisConversion = true;
            importer.globalScale = 1f;

            if (assetPath.StartsWith(ENVIRONMENT_ROOT))
            {
                importer.generateSecondaryUV = true;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
            }
            else if (assetPath.StartsWith(CHARACTERS_ROOT))
            {
                importer.animationType = ModelImporterAnimationType.Human;
            }
            else if (assetPath.StartsWith(ENTITIES_ROOT))
            {
                importer.animationType = ModelImporterAnimationType.Generic;
            }
            else
            {
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
            }
        }


        private void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith(ART_ROOT))
            {
                return;
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.name.StartsWith(COLLIDER_PREFIX))
                {
                    ConvertToBoxCollider(filter);
                }
            }
        }


        private Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            if (!assetPath.StartsWith(ART_ROOT) || !material.name.StartsWith(MATERIAL_PREFIX))
            {
                return null;
            }

            foreach (string guid in AssetDatabase.FindAssets(material.name + " t:Material", new[] { ART_ROOT.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == material.name)
                {
                    return AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }

            return null;
        }


        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ART_ROOT))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);

            if (name.EndsWith(NORMAL_SUFFIX))
            {
                importer.textureType = TextureImporterType.NormalMap;
            }
            else if (name.EndsWith(MASK_SUFFIX) || name.EndsWith(ARM_SUFFIX))
            {
                importer.sRGBTexture = false;
            }

            if (importer.importSettingsMissing)
            {
                importer.maxTextureSize = DEFAULT_MAX_TEXTURE_SIZE;
            }
        }


        private static void ConvertToBoxCollider(MeshFilter filter)
        {
            GameObject target = filter.gameObject;
            Bounds bounds = filter.sharedMesh.bounds;

            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

            Object.DestroyImmediate(target.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(filter);
        }
    }
}
