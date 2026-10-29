using NoWayOut.UI;
using NoWayOut.UI.Common;
using NoWayOut.UI.Screens.MainMenu;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.App.Scopes
{
    public sealed class MainMenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIScreensConfig uiScreensConfig;
        [SerializeField] private MainMenuConfig mainMenuConfig;


        protected override void Configure(IContainerBuilder builder)
        {
            InstallUI(builder);
            InstallMainMenu(builder);
        }


        private void InstallUI(IContainerBuilder builder)
        {
            new UIInstaller(uiScreensConfig).Install(builder);
        }


        private void InstallMainMenu(IContainerBuilder builder)
        {
            new MainMenuInstaller(mainMenuConfig).Install(builder);
        }
    }
}
