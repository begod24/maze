using NoWayOut.UI.Common;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.UI
{
    public sealed class UIInstaller : IInstaller
    {
        private readonly UIScreensConfig screensConfig;


        public UIInstaller(UIScreensConfig screensConfig)
        {
            this.screensConfig = screensConfig;
        }


        public void Install(IContainerBuilder builder)
        {
            RegisterConfig(builder);
            RegisterScreenService(builder);
        }


        private void RegisterConfig(IContainerBuilder builder)
        {
            builder.RegisterInstance(screensConfig);
        }


        private void RegisterScreenService(IContainerBuilder builder)
        {
            builder.Register<IScreenService, ScreenService>(Lifetime.Scoped);
        }
    }
}
