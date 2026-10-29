using System;
using Cysharp.Threading.Tasks;
using NoWayOut.Core.Events;
using NoWayOut.Networking.Events;
using NoWayOut.Networking.Sessions;
using NoWayOut.UI.Events;
using UnityEngine;

namespace NoWayOut.UI.Screens.Lobby
{
    internal sealed class LobbyPresenter
    {
        private readonly ISessionService sessionService;
        private readonly IEventBus eventBus;
        private LobbyView view;
        private IDisposable sessionStartedSubscription;
        private IDisposable sessionChangedSubscription;


        public LobbyPresenter(ISessionService sessionService, IEventBus eventBus)
        {
            this.sessionService = sessionService;
            this.eventBus = eventBus;
        }


        public void Attach(LobbyView view)
        {
            this.view = view;
            view.CopyRequested += HandleCopyRequested;
            view.StartRequested += HandleStartRequested;
            view.LeaveRequested += HandleLeaveRequested;
            sessionStartedSubscription = eventBus.Subscribe<SessionStartedEvent>(HandleSessionStarted);
            sessionChangedSubscription = eventBus.Subscribe<SessionChangedEvent>(HandleSessionChanged);
            Refresh();
        }


        public void Detach()
        {
            if (view == null)
            {
                return;
            }

            view.CopyRequested -= HandleCopyRequested;
            view.StartRequested -= HandleStartRequested;
            view.LeaveRequested -= HandleLeaveRequested;
            sessionStartedSubscription?.Dispose();
            sessionChangedSubscription?.Dispose();
            sessionStartedSubscription = null;
            sessionChangedSubscription = null;
            view = null;
        }


        private void Refresh()
        {
            view.SetCode(sessionService.JoinCode);
            view.SetPlayers(sessionService.PlayerCount, sessionService.MaxPlayers);
            view.SetIsHost(sessionService.IsHost);
            view.SetBusy(false);
        }


        private void HandleSessionStarted(SessionStartedEvent payload)
        {
            Refresh();
        }


        private void HandleSessionChanged(SessionChangedEvent payload)
        {
            Refresh();
        }


        private void HandleCopyRequested()
        {
            GUIUtility.systemCopyBuffer = sessionService.JoinCode;
        }


        private void HandleStartRequested()
        {
            view.SetBusy(true);
            eventBus.Publish(new GameStartRequestedEvent());
        }


        private void HandleLeaveRequested()
        {
            view.SetBusy(true);
            sessionService.LeaveAsync().Forget();
        }
    }
}
