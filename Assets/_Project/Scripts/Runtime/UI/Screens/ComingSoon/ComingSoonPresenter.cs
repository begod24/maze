using System;
using NoWayOut.UI.Screens.MainMenu;

namespace NoWayOut.UI.Screens.ComingSoon
{
    internal sealed class ComingSoonPresenter
    {
        private const string SETTINGS_TITLE_KEY = "main_menu.settings";
        private const string CREDITS_TITLE_KEY = "main_menu.credits";

        private ComingSoonView view;
        private string titleKey = SETTINGS_TITLE_KEY;

        public event Action BackRequested;


        public void Attach(ComingSoonView view)
        {
            this.view = view;
            view.BackRequested += HandleBackRequested;
            view.SetTitle(titleKey);
        }


        public void Detach()
        {
            if (view == null)
            {
                return;
            }

            view.BackRequested -= HandleBackRequested;
            view = null;
        }


        public void SetSection(MainMenuItem item)
        {
            if (item == MainMenuItem.Credits)
            {
                titleKey = CREDITS_TITLE_KEY;
            }
            else
            {
                titleKey = SETTINGS_TITLE_KEY;
            }

            if (view != null)
            {
                view.SetTitle(titleKey);
            }
        }


        private void HandleBackRequested()
        {
            BackRequested?.Invoke();
        }
    }
}
