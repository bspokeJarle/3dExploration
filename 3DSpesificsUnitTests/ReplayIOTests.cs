using GameplayHelpers.ReplayIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Domain;
using System;
using System.Collections.Generic;
using System.IO;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests
{
    [TestClass]
    public class ReplayIOTests
    {
        [TestMethod]
        public void SaveAndLoad_Replay_RoundTripsFrames()
        {
            string path = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.osrp");

            var recorder = new ReplayRecorder();
            recorder.BeginRecording(surfaceHash: 1234UL, fps: 60);

            var ship = new _3dObject
            {
                ObjectId = 1,
                ObjectName = "Ship",
                WorldPosition = new Vector3(0, 0, 0),
                ObjectOffsets = new Vector3(10, 20, 30),
                Rotation = new Vector3(0, 0, 0)
            };

            var seeder = new _3dObject
            {
                ObjectId = 2,
                ObjectName = "Seeder",
                WorldPosition = new Vector3(100, 0, 100),
                ObjectOffsets = new Vector3(5, 15, 25),
                Rotation = new Vector3(0, 90, 0)
            };

            try
            {
                for (int frame = 0; frame < 3; frame++)
                {
                    ship.WorldPosition = new Vector3(frame, 0, frame * 2);
                    ship.ObjectOffsets = new Vector3(10 + frame, 20, 30);
                    ship.Rotation = new Vector3(0, frame * 5, 0);

                    seeder.WorldPosition = new Vector3(100 + frame, 0, 100 + frame);
                    seeder.ObjectOffsets = new Vector3(5, 15 + frame, 25);
                    seeder.Rotation = new Vector3(0, 90 + frame, 0);

                    recorder.RecordFrame(frame, new List<I3dObject> { ship, seeder });

                    if (frame == 1)
                    {
                        recorder.TriggerExplodeForCurrentFrame(ship.ObjectId);
                    }
                }

                var replay = recorder.EndRecording(path, "UnitTestReplay");
                ReplayIO.Save(path, (IGameReplay)replay);

                bool loaded = ReplayIO.TryLoad(path, out ulong loadedSurfaceHash, out int loadedFps, out var frames);

                Assert.IsTrue(loaded);
                Assert.AreEqual(1234UL, loadedSurfaceHash);
                Assert.AreEqual(60, loadedFps);
                Assert.AreEqual(3, frames.Count);

                Assert.AreEqual(2, frames[0].ObjectStates.Count);
                Assert.AreEqual(0, frames[0].ObjectStates[0].WorldPosition.x);
                Assert.AreEqual(10, frames[0].ObjectStates[0].ObjectOffset.x);

                Assert.AreEqual(true, frames[1].ObjectStates[0].TriggerExplode);
                Assert.AreEqual(102, frames[2].ObjectStates[1].WorldPosition.x);
                Assert.AreEqual(17, frames[2].ObjectStates[1].ObjectOffset.y);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveAndLoad_Replay_ObjectLookupById_IsStable()
        {
            string path = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.osrp");

            var recorder = new ReplayRecorder();
            recorder.BeginRecording(surfaceHash: 555UL, fps: 30);

            var ship = new _3dObject
            {
                ObjectId = 1,
                ObjectName = "Ship",
                WorldPosition = new Vector3(1, 0, 2),
                ObjectOffsets = new Vector3(10, 20, 30),
                Rotation = new Vector3(0, 0, 0)
            };

            var seeder = new _3dObject
            {
                ObjectId = 2,
                ObjectName = "Seeder",
                WorldPosition = new Vector3(101, 0, 102),
                ObjectOffsets = new Vector3(5, 16, 25),
                Rotation = new Vector3(0, 90, 0)
            };

            try
            {
                recorder.RecordFrame(0, new List<I3dObject> { seeder, ship });
                var replay = recorder.EndRecording(path, "UnitTestReplayLookup");
                ReplayIO.Save(path, (IGameReplay)replay);

                bool loaded = ReplayIO.TryLoad(path, out _, out _, out var frames);

                Assert.IsTrue(loaded);
                Assert.AreEqual(1, frames.Count);

                var shipState = FindState(frames[0].ObjectStates, 1);
                var seederState = FindState(frames[0].ObjectStates, 2);

                Assert.AreEqual(1, shipState.WorldPosition.x);
                Assert.AreEqual(10, shipState.ObjectOffset.x);
                Assert.AreEqual(101, seederState.WorldPosition.x);
                Assert.AreEqual(16, seederState.ObjectOffset.y);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveAndLoad_Replay_VaryingObjectCounts_RoundTrips()
        {
            string path = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.osrp");

            var recorder = new ReplayRecorder();
            recorder.BeginRecording(surfaceHash: 777UL, fps: 24);

            var ship = new _3dObject
            {
                ObjectId = 1,
                ObjectName = "Ship",
                WorldPosition = new Vector3(0, 0, 0),
                ObjectOffsets = new Vector3(10, 20, 30),
                Rotation = new Vector3(0, 0, 0)
            };

            var seeder = new _3dObject
            {
                ObjectId = 2,
                ObjectName = "Seeder",
                WorldPosition = new Vector3(100, 0, 100),
                ObjectOffsets = new Vector3(5, 15, 25),
                Rotation = new Vector3(0, 90, 0)
            };

            try
            {
                recorder.RecordFrame(0, new List<I3dObject> { ship, seeder });
                recorder.RecordFrame(1, new List<I3dObject> { ship });
                recorder.RecordFrame(2, new List<I3dObject> { ship, seeder });

                var replay = recorder.EndRecording(path, "UnitTestReplayCounts");
                ReplayIO.Save(path, (IGameReplay)replay);

                bool loaded = ReplayIO.TryLoad(path, out _, out _, out var frames);

                Assert.IsTrue(loaded);
                Assert.AreEqual(3, frames.Count);
                Assert.AreEqual(2, frames[0].ObjectStates.Count);
                Assert.AreEqual(1, frames[1].ObjectStates.Count);
                Assert.AreEqual(2, frames[2].ObjectStates.Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void TryLoad_Replay_ReturnsFalse_ForCorruptedFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.osrp");

            var recorder = new ReplayRecorder();
            recorder.BeginRecording(surfaceHash: 888UL, fps: 60);

            var ship = new _3dObject
            {
                ObjectId = 1,
                ObjectName = "Ship",
                WorldPosition = new Vector3(0, 0, 0),
                ObjectOffsets = new Vector3(10, 20, 30),
                Rotation = new Vector3(0, 0, 0)
            };

            try
            {
                recorder.RecordFrame(0, new List<I3dObject> { ship });

                var replay = recorder.EndRecording(path, "UnitTestReplayCorrupt");
                ReplayIO.Save(path, (IGameReplay)replay);

                var bytes = File.ReadAllBytes(path);
                if (bytes.Length > 0)
                {
                    bytes[^1] ^= 0xFF;
                    File.WriteAllBytes(path, bytes);
                }

                bool loaded = ReplayIO.TryLoad(path, out var loadedSurfaceHash, out var loadedFps, out var frames);

                Assert.IsFalse(loaded);
                Assert.AreEqual(0UL, loadedSurfaceHash);
                Assert.AreEqual(0, loadedFps);
                Assert.AreEqual(0, frames.Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static IReplayObjectState FindState(List<ReplayIO.ReplayObjectState> states, int objectId)
        {
            var match = states.Find(state => state.ObjectId == objectId);
            Assert.IsNotNull(match);
            return match;
        }
    }
}
