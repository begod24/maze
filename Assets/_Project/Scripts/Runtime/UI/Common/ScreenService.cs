using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace NoWayOut.UI.Common
{
    internal sealed class ScreenService : IScreenService, IDisposable
    {
        private const string SCREEN_CLASS = "screen";
        private const string HIDDEN_CLASS = "screen--hidden";

        private readonly IObjectResolver resolver;
        private readonly UIScreensConfig config;
        private readonly Dictionary<Type, VisualElement> screens = new Dictionary<Type, VisualElement>();
        private readonly List<GameObject> instances = new List<GameObject>();


        public ScreenService(IObjectResolver resolver, UIScreensConfig config)
        {
            this.resolver = resolver;
            this.config = config;
        }


        public void Show<TView>() where TView : MonoBehaviour
        {
            VisualElement screen = GetOrCreateScreen<TView>();
            screen.style.display = DisplayStyle.Flex;
            screen.SetEnabled(true);
            screen.schedule.Execute(() => screen.RemoveFromClassList(HIDDEN_CLASS));
        }


        public void Hide<TView>() where TView : MonoBehaviour
        {
            if (!screens.TryGetValue(typeof(TView), out VisualElement screen))
            {
                return;
            }

            screen.SetEnabled(false);
            screen.AddToClassList(HIDDEN_CLASS);
            screen.RegisterCallbackOnce<TransitionEndEvent>(HandleHideTransitionEnd);
        }


        public bool IsVisible<TView>() where TView : MonoBehaviour
        {
            return screens.TryGetValue(typeof(TView), out VisualElement screen) && !screen.ClassListContains(HIDDEN_CLASS);
        }


        public void Dispose()
        {
            foreach (GameObject instance in instances)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }

            instances.Clear();
            screens.Clear();
        }


        private VisualElement GetOrCreateScreen<TView>() where TView : MonoBehaviour
        {
            if (screens.TryGetValue(typeof(TView), out VisualElement existing))
            {
                return existing;
            }

            GameObject instance = resolver.Instantiate(FindPrefab<TView>());
            instances.Add(instance);

            VisualElement screen = instance.GetComponent<UIDocument>().rootVisualElement.Q(className: SCREEN_CLASS);
            screens.Add(typeof(TView), screen);
            return screen;
        }


        private GameObject FindPrefab<TView>() where TView : MonoBehaviour
        {
            foreach (GameObject prefab in config.screenPrefabs)
            {
                if (prefab.GetComponent<TView>() != null)
                {
                    return prefab;
                }
            }

            throw new InvalidOperationException("No screen prefab with " + typeof(TView).Name + " in UIScreensConfig");
        }


        private static void HandleHideTransitionEnd(TransitionEndEvent evt)
        {
            VisualElement screen = (VisualElement)evt.currentTarget;
            if (screen.ClassListContains(HIDDEN_CLASS))
            {
                screen.style.display = DisplayStyle.None;
            }
        }
    }
}
