using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using VContainer;

namespace NoWayOut.UI.Screens.ComingSoon
{
    public sealed class ComingSoonView : MonoBehaviour
    {
        private const string TABLE = "UI";
        private const string TEXT_PROPERTY = "text";

        [SerializeField] private UIDocument document;

        private ComingSoonPresenter presenter;
        private Label titleLabel;
        private Button backButton;

        public event Action BackRequested;


        [Inject]
        private void Construct(ComingSoonPresenter presenter)
        {
            this.presenter = presenter;
        }


        public void SetTitle(string entryKey)
        {
            titleLabel.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, entryKey));
        }


        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            titleLabel = root.Q<Label>("title-label");
            backButton = root.Q<Button>("back-button");

            root.Q<Label>("message-label").SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "coming_soon.message"));
            backButton.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "common.back"));

            backButton.clicked += HandleBackClicked;
            presenter.Attach(this);
        }


        private void OnDisable()
        {
            presenter.Detach();
            backButton.clicked -= HandleBackClicked;
        }


        private void HandleBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
