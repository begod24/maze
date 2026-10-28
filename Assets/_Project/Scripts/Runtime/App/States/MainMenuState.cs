using Cysharp.Threading.Tasks;
using NoWayOut.Core.States;
using NoWayOut.Infrastructure.SceneManagement;

namespace NoWayOut.App.States
{
    internal sealed class MainMenuState : IState
    {
        private readonly ISceneLoader sceneLoader;


        public MainMenuState(ISceneLoader sceneLoader)
        {
            this.sceneLoader = sceneLoader;
        }


        public async UniTask EnterAsync()
        {
            await sceneLoader.LoadAsync(SceneNames.MAIN_MENU);
        }


        public UniTask ExitAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
