using System.Collections.Generic;
using UnityEngine;

namespace NoWayOut.UI.Common
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/UI Screens Config", fileName = "UIScreensConfig")]
    public sealed class UIScreensConfig : ScriptableObject
    {
        public List<GameObject> screenPrefabs = new List<GameObject>();
    }
}
