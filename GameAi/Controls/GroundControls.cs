using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using CommonUtilities.GamePlayHelpers;
using Domain;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GameAiAndControls.Controls
{
    public class GroundControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ITriangleMeshWithColor? GuideCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public I3dObject ParentObject { get; set; }

        public float zPosition { get; set; } = 0;
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private bool enableLogging = false;
        private readonly HashSet<int> _processedBombCraters = new();

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;
            if (theObject.ImpactStatus.HasCrashed == true)
            {
                if (enableLogging) Logger.Log($"GroundControls: {theObject.ImpactStatus.ObjectName} has crashed! Handle the crash.");
            }

            DetectBombCraters();

            if (theObject != null && theObject.ParentSurface != null)
            {
                //Replace the surfaces from the new viewport - other objects might have moved surface position
                var newViewPort = theObject!.ParentSurface!.GetSurfaceViewPort();                
                theObject.ObjectParts = newViewPort.ObjectParts; 
                theObject.CrashBoxes = newViewPort.CrashBoxes;
            }            
            return theObject!;
        }

        private void DetectBombCraters()
        {
            var aiObjects = GameState.SurfaceState?.AiObjects;
            var global2DMap = GameState.SurfaceState?.Global2DMap;
            if (aiObjects == null || global2DMap == null) return;

            int mapHeight = global2DMap.GetLength(0);
            int mapWidth = global2DMap.GetLength(1);
            var rnd = new Random();

            for (int i = 0; i < aiObjects.Count; i++)
            {
                var obj = aiObjects[i];
                if (obj.ObjectName != "BomberBomb") continue;
                if (obj.ImpactStatus?.HasCrashed != true && obj.ImpactStatus?.HasExploded != true) continue;
                if (obj.ImpactStatus?.ObjectName != "Surface") continue;
                if (_processedBombCraters.Contains(obj.ObjectId)) continue;

                _processedBombCraters.Add(obj.ObjectId);

                var wp = obj.WorldPosition;
                if (wp == null) continue;

                int centerX = MapCoordinateHelpers.WorldXToTileIndex(wp.x, global2DMap);
                int centerZ = MapCoordinateHelpers.WorldZToTileIndex(wp.z, global2DMap);

                // Apply crater to 3x3 area around impact
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int tileZ = MapCoordinateHelpers.WrapIndex(centerZ + dz, mapHeight);
                        int tileX = MapCoordinateHelpers.WrapIndex(centerX + dx, mapWidth);

                        ref var tile = ref global2DMap[tileZ, tileX];
                        if (!tile.isCratered && IsCraterableTerrain(tile.mapDepth))
                        {
                            tile.isCratered = true;
                            tile.mapDepth = Math.Max(GetMinimumDryCraterDepth(), tile.mapDepth - rnd.Next(10, 21));
                        }
                    }
                }

                if (enableLogging && Logger.EnableFileLogging)
                    Logger.Log($"GroundControls: Bomb crater at tile x={centerX}; z={centerZ}");
            }
        }

        private static bool IsCraterableTerrain(int mapDepth)
        {
            var terrain = GamePlayHelpers.GetTerrainType(mapDepth, MapSetup.maxHeight);
            return terrain == GamePlayHelpers.TerrainType.Grassland ||
                   terrain == GamePlayHelpers.TerrainType.Highlands ||
                   terrain == GamePlayHelpers.TerrainType.Mountains;
        }

        private static int GetMinimumDryCraterDepth()
        {
            return (int)Math.Ceiling(MapSetup.maxHeight * 0.15f);
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            throw new NotImplementedException();
        }
        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            throw new NotImplementedException();
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            throw new NotImplementedException();
        }
    }
}
