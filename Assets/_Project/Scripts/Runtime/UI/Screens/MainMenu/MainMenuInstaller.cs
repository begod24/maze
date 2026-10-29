using NoWayOut.UI.Screens.ComingSoon;
using NoWayOut.UI.Screens.JoinCode;
using NoWayOut.UI.Screens.Lobby;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.UI.Screens.MainMenu
{
    public sealed class MainMenuInstaller : IInstaller
    {
        private readonly MainMenuConfig config;


        public MainMenuInstaller(MainMenuConfig config)
        {
            this.config = config;
        }


        public void Install(IContainerBuilder builder)
        {
            RegisterConfig(builder);
            RegisterPresenters(builder);
        }


        private void RegisterConfig(IContainerBuilder builder)
        {
            builder.RegisterInstance(config);
        }


        private void RegisterPresenters(IContainerBuilder builder)
        {
            builder.Register<MainMenuPresenter>(Lifetime.Scoped);
            builder.Register<JoinCodePresenter>(Lifetime.Scoped);
            builder.Register<LobbyPresenter>(Lifetime.Scoped);
            builder.Register<ComingSoonPresenter>(Lifetime.Scoped);
        }
    }
}
