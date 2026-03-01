using System.Windows.Input;

namespace Domain
{
    public interface ISceneHandler
    {
        void SetupActiveScene(I3dWorld world);
        void ResetActiveScene(I3dWorld world);
        void NextScene(I3dWorld world);
        IScene GetActiveScene();
        void HandleKeyPress(KeyEventArgs k, I3dWorld world);
    }

    public interface IScene
    {
        SceneTypes SceneType { get; }
        GameModes GameMode { get; }
        void SetupScene(I3dWorld world);
        string SceneMusic { get; }
        void SetupSceneOverlay();
        void SetupGameOverlay();
    }
}
