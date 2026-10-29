using System;
using Cysharp.Threading.Tasks;
using NoWayOut.Core.Events;
using NoWayOut.Networking.Events;
using NoWayOut.Networking.Sessions;
using NoWayOut.UI.Common;
using NoWayOut.UI.Events;
using NoWayOut.UI.Screens.ComingSoon;
using NoWayOut.UI.Screens.JoinCode;
using NoWayOut.UI.Screens.Lobby;
using UnityEngine;

namespace NoWayOut.UI.Screens.MainMenu
{
    internal sealed class MainMenuPresenter
    {
        private const string CREATING_KEY = "main_menu.creating";
        private const string CREATE_FAILED_KEY = "main_menu.create_failed";

        private readonly IScreenService screenService;
        private readonly ISessionService sessionService;
        private readonly IEventBus eventBus;
        private readonly MainMenuConfig config;
        private readonly JoinCodePresenter joinCodePresenter;
        private readonly ComingSoonPresenter comingSoonPresenter;
        private MainMenuView view;
        private IDisposable sessionStartedSubscription;
        private IDisposable sessionEndedSubscription;
        private bool isBusy;


        public MainMenuPresenter(
            IScreenService screenService,
            ISessionService sessionService,
            IEventBus eventBus,
            MainMenuConfig config,
            JoinCodePresenter joinCodePresenter,
            ComingSoonPresenter comingSoonPresenter)
        {
            this.screenService = screenService;
            this.sessionService = sessionService;
            this.eventBus = eventBus;
            this.config = config;
            this.joinCodePresenter = joinCodePresenter;
            this.comingSoonPresenter = comingSoonPresenter;
        }


        public void Attach(MainMenuView view)
        {
            this.view = view;
            view.ItemHovered += HandleItemHovered;
            view.ItemUnhovered += HandleItemUnhovered;
            view.ItemSelected += HandleItemSelected;
            joinCodePresenter.BackRequested += HandleBackRequested;
            comingSoonPresenter.BackRequested += HandleBackRequested;
            sessionStartedSubscription = eventBus.Subscribe<SessionStartedEvent>(HandleSessionStarted);
            sessionEndedSubscription = eventBus.Subscribe<SessionEndedEvent>(HandleSessionEnded);

            isBusy = false;
            view.HideStatus();
            view.SetInteractable(true);
        }


        public void Detach()
        {
            if (view == null)
            {
                return;
            }

            view.ItemHovered -= HandleItemHovered;
            view.ItemUnhovered -= HandleItemUnhovered;
            view.ItemSelected -= HandleItemSelected;
            joinCodePresenter.BackRequested -= HandleBackRequested;
            comingSoonPresenter.BackRequested -= HandleBackRequested;
            sessionStartedSubscription?.Dispose();
            sessionEndedSubscription?.Dispose();
            sessionStartedSubscription = null;
            sessionEndedSubscription = null;
            view = null;
        }


        private void HandleItemHovered(MainMenuItem item)
        {
            if (isBusy)
            {
                return;
            }

            view.SetHovered(item, true);
        }


        private void HandleItemUnhovered(MainMenuItem item)
        {
            if (isBusy)
            {
                return;
            }

            view.SetHovered(item, false);
        }


        private void HandleItemSelected(MainMenuItem item)
        {
            if (isBusy)
            {
                return;
            }

            SelectAsync(item).Forget();
        }


        private async UniTaskVoid SelectAsync(MainMenuItem item)
        {
            isBusy = true;
            view.SetInteractable(false);
            await view.FocusAsync(item);

            if (view == null)
            {
                return;
            }

            switch (item)
            {
                case MainMenuItem.Create:
                    await CreateRoomAsync();
                    break;
                case MainMenuItem.Join:
                    screenService.Show<JoinCodeView>();
                    break;
                case MainMenuItem.Settings:
                case MainMenuItem.Credits:
                    comingSoonPresenter.SetSection(item);
                    screenService.Show<ComingSoonView>();
                    break;
                default:
                    eventBus.Publish(new QuitRequestedEvent());
                    break;
            }
        }


        private async UniTask CreateRoomAsync()
        {
            view.ShowStatus(CREATING_KEY);

            try
            {
                await sessionService.HostAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (view == null)
                {
                    return;
                }

                view.ShowStatus(CREATE_FAILED_KEY);
                await UniTask.Delay(TimeSpan.FromSeconds(config.errorDisplaySeconds));
                await ReturnToRoomAsync();
            }
        }


        private void HandleSessionStarted(SessionStartedEvent payload)
        {
            view.HideStatus();
            screenService.Hide<JoinCodeView>();
            screenService.Show<LobbyView>();
        }


        private void HandleSessionEnded(SessionEndedEvent payload)
        {
            screenService.Hide<LobbyView>();
            ReturnToRoomAsync().Forget();
        }


        private void HandleBackRequested()
        {
            screenService.Hide<JoinCodeView>();
            screenService.Hide<ComingSoonView>();
            ReturnToRoomAsync().Forget();
        }


        private async UniTask ReturnToRoomAsync()
        {
            if (view == null)
            {
                return;
            }

            view.HideStatus();
            await view.ReturnAsync();

            if (view == null)
            {
                return;
            }

            isBusy = false;
            view.SetInteractable(true);
        }
    }
}
