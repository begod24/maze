using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;
using VContainer;

namespace NoWayOut.UI.Screens.Lobby
{
    public sealed class LobbyView : MonoBehaviour
    {
        private const string TABLE = "UI";
        private const string TEXT_PROPERTY = "text";
        private const string HIDDEN_CLASS = "lobby__item--hidden";
        private const string COUNT_VARIABLE = "count";
        private const string MAX_VARIABLE = "max";

        [SerializeField] private UIDocument document;

        private readonly IntVariable playerCount = new IntVariable();
        private readonly IntVariable maxPlayers = new IntVariable();
        private LobbyPresenter presenter;
        private Label codeLabel;
        private Label waitingLabel;
        private Button copyButton;
        private Button startButton;
        private Button leaveButton;

        public event Action CopyRequested;
        public event Action StartRequested;
        public event Action LeaveRequested;


        [Inject]
        private void Construct(LobbyPresenter presenter)
        {
            this.presenter = presenter;
        }


        public void SetCode(string code)
        {
            codeLabel.text = code;
        }


        public void SetPlayers(int count, int max)
        {
            playerCount.Value = count;
            maxPlayers.Value = max;
        }


        public void SetIsHost(bool isHost)
        {
            startButton.EnableInClassList(HIDDEN_CLASS, !isHost);
            waitingLabel.EnableInClassList(HIDDEN_CLASS, isHost);
        }


        public void SetBusy(bool isBusy)
        {
            startButton.SetEnabled(!isBusy);
            leaveButton.SetEnabled(!isBusy);
        }


        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            codeLabel = root.Q<Label>("code-label");
            waitingLabel = root.Q<Label>("waiting-label");
            copyButton = root.Q<Button>("copy-button");
            startButton = root.Q<Button>("start-button");
            leaveButton = root.Q<Button>("leave-button");

            LocalizedString playersText = new LocalizedString(TABLE, "lobby.players");
            playersText.Add(COUNT_VARIABLE, playerCount);
            playersText.Add(MAX_VARIABLE, maxPlayers);

            root.Q<Label>("title-label").SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "lobby.title"));
            root.Q<Label>("code-caption").SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "lobby.code"));
            root.Q<Label>("players-label").SetBinding(TEXT_PROPERTY, playersText);
            waitingLabel.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "lobby.waiting_host"));
            copyButton.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "lobby.copy"));
            startButton.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "lobby.start"));
            leaveButton.SetBinding(TEXT_PROPERTY, new LocalizedString(TABLE, "lobby.leave"));

            copyButton.clicked += HandleCopyClicked;
            startButton.clicked += HandleStartClicked;
            leaveButton.clicked += HandleLeaveClicked;
            presenter.Attach(this);
        }


        private void OnDisable()
        {
            presenter.Detach();
            copyButton.clicked -= HandleCopyClicked;
            startButton.clicked -= HandleStartClicked;
            leaveButton.clicked -= HandleLeaveClicked;
        }


        private void HandleCopyClicked()
        {
            CopyRequested?.Invoke();
        }


        private void HandleStartClicked()
        {
            StartRequested?.Invoke();
        }


        private void HandleLeaveClicked()
        {
            LeaveRequested?.Invoke();
        }
    }
}
