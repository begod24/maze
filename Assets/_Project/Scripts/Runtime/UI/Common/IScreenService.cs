using UnityEngine;

namespace NoWayOut.UI.Common
{
    public interface IScreenService
    {
        void Show<TView>() where TView : MonoBehaviour;

        void Hide<TView>() where TView : MonoBehaviour;

        bool IsVisible<TView>() where TView : MonoBehaviour;
    }
}
