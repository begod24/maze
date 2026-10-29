using System;
using Cysharp.Threading.Tasks;
using NoWayOut.Networking.Sessions;
using UnityEngine;

namespace NoWayOut.UI.Screens.JoinCode
{
    internal sealed class JoinCodePresenter
    {
        private const string CONNECTING_KEY = "join.connecting";
        private const string EMPTY_CODE_KEY = "join.empty_code";
        private const string FAILED_KEY = "join.failed";

        private readonly ISessionService sessionService;
        private JoinCodeView view;
        private bool isJoining;

        public event Action BackRequested;


        public JoinCodePresenter(ISessionService sessionService)
        {
            this.sessionService = sessionService;
        }


        public void Attach(JoinCodeView view)
        {
            this.view = view;
            view.JoinRequested += HandleJoinRequested;
            view.BackRequested += HandleBackRequested;
            view.HideStatus();
        }


        public void Detach()
        {
            if (view == null)
            {
                return;
            }

            view.JoinRequested -= HandleJoinRequested;
            view.BackRequested -= HandleBackRequested;
            view = null;
        }


        private void HandleJoinRequested(string code)
        {
            if (isJoining)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                view.ShowStatus(EMPTY_CODE_KEY, true);
                return;
            }

            JoinAsync(code).Forget();
        }


        private void HandleBackRequested()
        {
            view.HideStatus();
            view.Clear();
            BackRequested?.Invoke();
        }


        private async UniTaskVoid JoinAsync(string code)
        {
            isJoining = true;
            view.SetBusy(true);
            view.ShowStatus(CONNECTING_KEY, false);

            try
            {
                await sessionService.JoinAsync(code);
                ResetView();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowFailure();
            }
            finally
            {
                isJoining = false;
                SetViewBusy(false);
            }
        }


        private void ResetView()
        {
            if (view == null)
            {
                return;
            }

            view.HideStatus();
            view.Clear();
        }


        private void ShowFailure()
        {
            if (view == null)
            {
                return;
            }

            view.ShowStatus(FAILED_KEY, true);
        }


        private void SetViewBusy(bool isBusy)
        {
            if (view == null)
            {
                return;
            }

            view.SetBusy(isBusy);
        }
    }
}
