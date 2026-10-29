using NoWayOut.Gameplay.World;
using NoWayOut.Gameplay.World.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoWayOut.Editor.Tools
{
    internal static class FloorPreviewTool
    {
        private const string MENU_PATH = "Tools/No Way Out/Preview Generated Floor";
        private const string CATALOG_PATH = "Assets/_Project/Data/World/WorldModuleCatalog.asset";
        private const string CONFIG_PATH = "Assets/_Project/Data/Configs/FloorConfig.asset";
        private const string ROOT_PREFIX = "FloorPreview_seed_";
        private const int RANDOM_SEED_MIN = 1;


        [MenuItem(MENU_PATH)]
        private static void Preview()
        {
            FloorConfig config = AssetDatabase.LoadAssetAtPath<FloorConfig>(CONFIG_PATH);
            WorldModuleCatalog catalog = AssetDatabase.LoadAssetAtPath<WorldModuleCatalog>(CATALOG_PATH);
            int seed = PickSeed(config);
            FloorLayout layout = new FloorGenerator(config, catalog).Generate(seed);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject root = new GameObject(ROOT_PREFIX + seed + "_" + layout.StartVariant);
            SceneManager.MoveGameObjectToScene(root, scene);

            foreach (PlacedModule module in layout.Modules)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(module.Definition.prefab, scene);
                instance.transform.SetParent(root.transform, false);
                instance.transform.SetPositionAndRotation(module.GetWorldPosition(config.cellSize), module.GetWorldRotation());
            }

            Selection.activeGameObject = root;
        }


        private static int PickSeed(FloorConfig config)
        {
            if (config.seed != 0)
            {
                return config.seed;
            }

            return Random.Range(RANDOM_SEED_MIN, int.MaxValue);
        }
    }
}
