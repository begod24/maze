using UnityEditor;
using UnityEditor.SceneManagement;

namespace NoWayOut.Editor
{
    [InitializeOnLoad]
    internal static class PlayFromBootScene
    {
        private const string MENU_PATH = "No Way Out/Play From Boot Scene";
        private const string PREFS_KEY = "NoWayOut.PlayFromBootScene";
        private const string BOOT_SCENE_PATH = "Assets/_Project/Scenes/Boot.unity";

        private static bool IsEnabled => EditorPrefs.GetBool(PREFS_KEY, true);


        static PlayFromBootScene()
        {
            EditorApplication.delayCall += Apply;
        }


        [MenuItem(MENU_PATH)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(PREFS_KEY, !IsEnabled);
            Apply();
        }


        [MenuItem(MENU_PATH, true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked(MENU_PATH, IsEnabled);
            return true;
        }


        private static void Apply()
        {
            if (IsEnabled)
            {
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BOOT_SCENE_PATH);
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }
    }
}
