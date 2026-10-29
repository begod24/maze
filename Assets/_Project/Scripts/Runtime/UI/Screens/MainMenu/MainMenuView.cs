using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using VContainer;

namespace NoWayOut.UI.Screens.MainMenu
{
    public sealed class MainMenuView : MonoBehaviour
    {
        private const string TABLE = "UI";
        private const string TEXT_PROPERTY = "text";
        private const string MENU_NAME = "main-menu";
        private const string TITLE_NAME = "title-label";
        private const string STATUS_NAME = "status-label";
        private const string FOCUSED_CLASS = "main-menu--focused";
        private const string HOVERED_CLASS = "tv-button--hovered";
        private const string STATUS_HIDDEN_CLASS = "main-menu__status--hidden";
        private const int BOUNDS_CORNER_COUNT = 8;

        [SerializeField] private UIDocument document;
        [SerializeField] private Camera menuCamera;
        [SerializeField] private MainMenuTv[] tvs;

        private readonly Vector3[] corners = new Vector3[BOUNDS_CORNER_COUNT];
        private MainMenuPresenter presenter;
        private MainMenuConfig config;
        private VisualElement menu;
        private Label statusLabel;
        private Button[] buttons;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Sequence cameraSequence;
        private bool isInteractable;

        public event Action<MainMenuItem> ItemHovered;
        public event Action<MainMenuItem> ItemUnhovered;
        public event Action<MainMenuItem> ItemSelected;


        [Inject]
        private void Construct(MainMenuPresenter presenter, MainMenuConfig config)
        {
            this.presenter = presenter;
            this.config = config;
        }


        public void SetInteractable(bool value)
        {
            isInteractable = value;
            foreach (Button button in buttons)
            {
                button.SetEnabled(value);
            }
        }


        public void SetHovered(MainMenuItem item, bool isHovered)
        {
            int index = IndexOf(item);
            buttons[index].EnableInClassList(HOVERED_CLASS, isHovered);

            float target = isHovered ? config.hoverDistortion : config.idleDistortion;
            tvs[index].AnimateDistortion(target, config.hoverDuration, config.hoverEase);
        }


        public async UniTask FocusAsync(MainMenuItem item)
        {
            MainMenuTv tv = tvs[IndexOf(item)];
            menu.AddToClassList(FOCUSED_CLASS);
            tv.AnimateDistortion(config.selectDistortion, config.focusDuration, config.focusEase);

            Transform cameraTransform = menuCamera.transform;
            cameraSequence.Stop();
            cameraSequence = Sequence.Create()
                .Group(Tween.Position(cameraTransform, tv.FocusPoint.position, config.focusDuration, config.focusEase))
                .Group(Tween.Rotation(cameraTransform, tv.FocusPoint.rotation, config.focusDuration, config.focusEase));
            await cameraSequence;
        }


        public async UniTask ReturnAsync()
        {
            foreach (MainMenuTv tv in tvs)
            {
                tv.AnimateDistortion(config.idleDistortion, config.returnDuration, config.returnEase);
            }

            Transform cameraTransform = menuCamera.transform;
            cameraSequence.Stop();
            cameraSequence = Sequence.Create()
                .Group(Tween.Position(cameraTransform, restPosition, config.returnDuration, config.returnEase))
                .Group(Tween.Rotation(cameraTransform, restRotation, config.returnDuration, config.returnEase));
            await cameraSequence;

            menu.RemoveFromClassList(FOCUSED_CLASS);
        }


        public void ShowStatus(string entryKey)
        {
            statusLabel.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, entryKey));
            statusLabel.RemoveFromClassList(STATUS_HIDDEN_CLASS);
        }


        public void HideStatus()
        {
            statusLabel.AddToClassList(STATUS_HIDDEN_CLASS);
        }


        private void Awake()
        {
            restPosition = menuCamera.transform.position;
            restRotation = menuCamera.transform.rotation;
        }


        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            menu = root.Q(MENU_NAME);
            statusLabel = root.Q<Label>(STATUS_NAME);
            root.Q<Label>(TITLE_NAME).SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "game.title"));

            buttons = new Button[tvs.Length];
            for (int i = 0; i < tvs.Length; i++)
            {
                buttons[i] = BindButton(root, tvs[i].Item);
            }

            presenter.Attach(this);
        }


        private void OnDisable()
        {
            presenter.Detach();
            cameraSequence.Stop();

            foreach (Button button in buttons)
            {
                button.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
                button.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
                button.UnregisterCallback<FocusInEvent>(HandleFocusIn);
                button.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
                button.clickable.clickedWithEventInfo -= HandleClicked;
            }
        }


        private void LateUpdate()
        {
            if (!isInteractable)
            {
                return;
            }

            for (int i = 0; i < tvs.Length; i++)
            {
                PlaceButtonOverTv(buttons[i], tvs[i]);
            }
        }


        private Button BindButton(VisualElement root, MainMenuItem item)
        {
            Button button = root.Q<Button>(GetButtonName(item));
            button.userData = item;
            button.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, GetLabelKey(item)));
            button.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            button.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            button.RegisterCallback<FocusInEvent>(HandleFocusIn);
            button.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            button.clickable.clickedWithEventInfo += HandleClicked;
            return button;
        }


        private void PlaceButtonOverTv(Button button, MainMenuTv tv)
        {
            Bounds bounds = tv.Bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            corners[0] = new Vector3(min.x, min.y, min.z);
            corners[1] = new Vector3(max.x, min.y, min.z);
            corners[2] = new Vector3(min.x, max.y, min.z);
            corners[3] = new Vector3(max.x, max.y, min.z);
            corners[4] = new Vector3(min.x, min.y, max.z);
            corners[5] = new Vector3(max.x, min.y, max.z);
            corners[6] = new Vector3(min.x, max.y, max.z);
            corners[7] = new Vector3(max.x, max.y, max.z);

            Vector2 panelMin = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 panelMax = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners)
            {
                Vector3 screenPoint = menuCamera.WorldToScreenPoint(corner);
                Vector2 flipped = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
                Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(button.panel, flipped);
                panelMin = Vector2.Min(panelMin, panelPoint);
                panelMax = Vector2.Max(panelMax, panelPoint);
            }

            button.style.left = panelMin.x;
            button.style.top = panelMin.y;
            button.style.width = panelMax.x - panelMin.x;
            button.style.height = panelMax.y - panelMin.y + config.labelHeight;
        }


        private int IndexOf(MainMenuItem item)
        {
            for (int i = 0; i < tvs.Length; i++)
            {
                if (tvs[i].Item == item)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(item), item, "No TV for this menu item");
        }


        private void HandlePointerEnter(PointerEnterEvent evt)
        {
            RaiseHovered(evt.currentTarget);
        }


        private void HandlePointerLeave(PointerLeaveEvent evt)
        {
            RaiseUnhovered(evt.currentTarget);
        }


        private void HandleFocusIn(FocusInEvent evt)
        {
            RaiseHovered(evt.currentTarget);
        }


        private void HandleFocusOut(FocusOutEvent evt)
        {
            RaiseUnhovered(evt.currentTarget);
        }


        private void HandleClicked(EventBase evt)
        {
            ItemSelected?.Invoke(GetItem(evt.currentTarget));
        }


        private void RaiseHovered(IEventHandler target)
        {
            ItemHovered?.Invoke(GetItem(target));
        }


        private void RaiseUnhovered(IEventHandler target)
        {
            ItemUnhovered?.Invoke(GetItem(target));
        }


        private static MainMenuItem GetItem(IEventHandler target)
        {
            VisualElement element = (VisualElement)target;
            return (MainMenuItem)element.userData;
        }


        private static string GetButtonName(MainMenuItem item)
        {
            switch (item)
            {
                case MainMenuItem.Create:
                    return "create-button";
                case MainMenuItem.Join:
                    return "join-button";
                case MainMenuItem.Settings:
                    return "settings-button";
                case MainMenuItem.Credits:
                    return "credits-button";
                default:
                    return "quit-button";
            }
        }


        private static string GetLabelKey(MainMenuItem item)
        {
            switch (item)
            {
                case MainMenuItem.Create:
                    return "main_menu.create";
                case MainMenuItem.Join:
                    return "main_menu.join";
                case MainMenuItem.Settings:
                    return "main_menu.settings";
                case MainMenuItem.Credits:
                    return "main_menu.credits";
                default:
                    return "main_menu.quit";
            }
        }
    }
}
