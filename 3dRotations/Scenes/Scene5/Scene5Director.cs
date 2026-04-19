using CommonUtilities.CommonGlobalState;
using CommonUtilities.Persistence;
using Domain;

namespace _3dRotations.Scene.Scene5
{
    public class Scene5Director : ISceneDirector
    {
        private IGameEventBus? _eventBus;
        private I3dWorld? _world;
        private bool _dronesActivated;
        private bool _motherShipActivated;

        public bool IsVictory { get; private set; }
        public bool IsDefeat { get; private set; }

        public void Initialize(IGameEventBus eventBus, I3dWorld world)
        {
            _eventBus = eventBus;
            _world = world;
            _dronesActivated = false;
            _motherShipActivated = false;
            IsVictory = false;
            IsDefeat = false;
        }

        public void Update()
        {
            if (IsVictory || IsDefeat) return;

            CheckDroneActivation();
            CheckMotherShipActivation();
            CheckVictoryCondition();
        }

        private void CheckDroneActivation()
        {
            if (_dronesActivated) return;
            if (!GameState.GamePlayState.IsDecoyUnlocked) return;

            var aiObjs = GameState.SurfaceState.AiObjects;
            for (int i = 0; i < aiObjs.Count; i++)
            {
                if (aiObjs[i].ObjectName == "KamikazeDrone" && !aiObjs[i].IsActive)
                    aiObjs[i].IsActive = true;
            }
            _dronesActivated = true;
        }

        private void CheckMotherShipActivation()
        {
            if (_motherShipActivated) return;

            var gps = GameState.GamePlayState;
            if (gps.InitialSeeders == 0) return;

            var aiObjs = GameState.SurfaceState.AiObjects;
            int liveSeeders = 0;
            int liveDrones = 0;
            for (int i = 0; i < aiObjs.Count; i++)
            {
                if (aiObjs[i].ObjectName == "Seeder")
                    liveSeeders++;
                else if (aiObjs[i].ObjectName == "KamikazeDrone")
                    liveDrones++;
            }

            if (liveSeeders > 0 || liveDrones > 0) return;

            bool activated = false;
            int msCount = 0;
            for (int i = 0; i < aiObjs.Count; i++)
            {
                if (aiObjs[i].ObjectName == "MotherShipSmall" && !aiObjs[i].IsActive)
                {
                    aiObjs[i].IsActive = true;
                    activated = true;
                }
                if (aiObjs[i].ObjectName == "MotherShipSmall" && aiObjs[i].IsActive)
                    msCount++;
            }

            if (activated)
            {
                _motherShipActivated = true;
                gps.SeedersRemaining = liveSeeders;
                gps.DronesRemaining = liveDrones;
                gps.MotherShipsRemaining = msCount;
                gps.SaveCheckpoint();
                try { GameStatePersistence.SaveGameState(); } catch { }
                try { HighscoreService.SubmitFromGamePlay(gps); } catch { }
            }
        }

        private void CheckVictoryCondition()
        {
            var gps = GameState.GamePlayState;
            if (gps.InitialDrones == 0 && gps.InitialSeeders == 0) return;
            if (gps.DronesRemaining != 0 || gps.SeedersRemaining != 0 || gps.MotherShipsRemaining != 0) return;

            var aiObjs = GameState.SurfaceState.AiObjects;
            for (int i = 0; i < aiObjs.Count; i++)
            {
                var obj = aiObjs[i];
                if (obj.ImpactStatus?.HasExploded == true) continue;
                if (obj.ObjectName == "Seeder" || obj.ObjectName == "KamikazeDrone" || obj.ObjectName == "MotherShipSmall")
                    return;
            }

            IsVictory = true;
        }

        public void Dispose()
        {
            _eventBus = null;
            _world = null;
            _dronesActivated = false;
            _motherShipActivated = false;
            IsVictory = false;
            IsDefeat = false;
        }
    }
}
