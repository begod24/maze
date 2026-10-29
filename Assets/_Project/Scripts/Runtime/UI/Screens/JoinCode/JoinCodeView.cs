using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using VContainer;

namespace NoWayOut.UI.Screens.JoinCode
{
    public sealed class JoinCodeView : MonoBehaviour
    {
        private const string TABLE = "UI";
        private const string TEXT_PROPERTY = "text";
        private const string STATUS_HIDDEN_CLASS = "status--hidden";
        private const string STATUS_ERROR_CLASS = "text--error";

        [SerializeField] private UIDocument document;

        private JoinCodePresenter presenter;
        private TextField codeField;
        private Label statusLabel;
        private Button joinButton;
        private Button backButton;

        public event Action<string> JoinRequested;
        public event Action BackRequested;


        [Inject]
        private void Construct(JoinCodePresenter presenter)
        {
            this.presenter = presenter;
        }


        public void SetBusy(bool isBusy)
        {
            codeField.SetEnabled(!isBusy);
            joinButton.SetEnabled(!isBusy);
            backButton.SetEnabled(!isBusy);
        }


        public void ShowStatus(string entryKey, bool isError)
        {
            statusLabel.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, entryKey));
            statusLabel.EnableInClassList(STATUS_ERROR_CLASS, isError);
            statusLabel.RemoveFromClassList(STATUS_HIDDEN_CLASS);
        }


        public void HideStatus()
        {
            statusLabel.AddToClassList(STATUS_HIDDEN_CLASS);
        }


        public void Clear()
        {
            codeField.value = string.Empty;
        }


        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            codeField = root.Q<TextField>("code-field");
            statusLabel = root.Q<Label>("status-label");
            joinButton = root.Q<Button>("join-button");
            backButton = root.Q<Button>("back-button");

            root.Q<Label>("title-label").SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "join.title"));
            joinButton.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "join.submit"));
            backButton.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "common.back"));

            joinButton.clicked += HandleJoinClicked;
            backButton.clicked += HandleBackClicked;
            codeField.RegisterCallback<KeyDownEvent>(HandleCodeKeyDown);
            presenter.Attach(this);
        }


        private void OnDisable()
        {
            presenter.Detach();
            joinButton.clicked -= HandleJoinClicked;
            backButton.clicked -= HandleBackClicked;
            codeField.UnregisterCallback<KeyDownEvent>(HandleCodeKeyDown);
        }


        private void HandleJoinClicked()
        {
            JoinRequested?.Invoke(codeField.value);
        }


        private void HandleBackClicked()
        {
            BackRequested?.Invoke();
        }


        private void HandleCodeKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                HandleJoinClicked();
            }
        }
    }
}
