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

        [TestMethod]
        public void SaveAndLoad_Replay_ObjectNames_RoundTrip()
        {
            string path = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.osrp");

            var recorder = new ReplayRecorder();
            recorder.BeginRecording(surfaceHash: 999UL, fps: 60);

            var ship = new _3dObject
            {
                ObjectId = 42,
                ObjectName = "Ship",
                WorldPosition = new Vector3(0, 0, 0),
                ObjectOffsets = new Vector3(10, 20, 30),
                Rotation = new Vector3(0, 0, 0)
            };

            try
            {
                recorder.RecordFrame(0, new List<I3dObject> { ship });
                var replay = recorder.EndRecording(path, "UnitTestReplayNames");
                ReplayIO.Save(path, (IGameReplay)replay);

                bool loaded = ReplayIO.TryLoad(path, out _, out _, out var frames);

                Assert.IsTrue(loaded);
                Assert.AreEqual(1, frames.Count);
                Assert.AreEqual(1, frames[0].ObjectStates.Count);
                Assert.AreEqual("Ship", frames[0].ObjectStates[0].ObjectName);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveAndLoad_Replay_IncludesParticlesAndWeapons()
        {
            string path = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.osrp");

            var recorder = new ReplayRecorder();
            recorder.BeginRecording(surfaceHash: 111UL, fps: 60);

            var ship = new _3dObject
            {
                ObjectId = 1,
                ObjectName = "Ship",
                WorldPosition = new Vector3(0, 0, 0),
                ObjectOffsets = new Vector3(10, 20, 30),
                Rotation = new Vector3(0, 0, 0)
            };

            var particle = new _3dObject
            {
                ObjectId = 2,
                ObjectName = "Particle",
                WorldPosition = new Vector3(5, 0, 5),
                ObjectOffsets = new Vector3(1, 2, 3),
                Rotation = new Vector3(0, 0, 0)
            };

            var lazer = new _3dObject
            {
                ObjectId = 3,
                ObjectName = "Lazer",
                WorldPosition = new Vector3(7, 0, 7),
                ObjectOffsets = new Vector3(4, 5, 6),
                Rotation = new Vector3(0, 0, 0)
            };

            try
            {
                recorder.RecordFrame(0, new List<I3dObject> { ship, particle, lazer });
                var replay = recorder.EndRecording(path, "UnitTestReplayParticlesWeapons");
                ReplayIO.Save(path, (IGameReplay)replay);

                bool loaded = ReplayIO.TryLoad(path, out _, out _, out var frames);

                Assert.IsTrue(loaded);
                Assert.AreEqual(1, frames.Count);
                Assert.AreEqual(3, frames[0].ObjectStates.Count);

                var particleState = FindState(frames[0].ObjectStates, 2);
                var lazerState = FindState(frames[0].ObjectStates, 3);

                Assert.AreEqual("Particle", particleState.ObjectName);
                Assert.AreEqual("Lazer", lazerState.ObjectName);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveAndLoad_Replay_StoresExplosionTrigger()
        {
            string path = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.osrp");

            var recorder = new ReplayRecorder();
            recorder.BeginRecording(surfaceHash: 222UL, fps: 60);

            var seeder = new _3dObject
            {
                ObjectId = 10,
                ObjectName = "Seeder",
                WorldPosition = new Vector3(1, 0, 1),
                ObjectOffsets = new Vector3(0, 0, 0),
                Rotation = new Vector3(0, 0, 0)
            };

            try
            {
                recorder.RecordFrame(0, new List<I3dObject> { seeder });
                recorder.TriggerExplodeForCurrentFrame(seeder.ObjectId);

                var replay = recorder.EndRecording(path, "UnitTestReplayExplode");
                ReplayIO.Save(path, (IGameReplay)replay);

                bool loaded = ReplayIO.TryLoad(path, out _, out _, out var frames);

                Assert.IsTrue(loaded);
                Assert.AreEqual(1, frames.Count);
                Assert.AreEqual(1, frames[0].ObjectStates.Count);
                Assert.IsTrue(frames[0].ObjectStates[0].TriggerExplode);
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

        [TestMethod]
        public void Manual_ValidateReplayFiles_FromEnvironment()
        {
            var replayPathA = Environment.GetEnvironmentVariable("REPLAY_FILE_A");
            var replayPathB = Environment.GetEnvironmentVariable("REPLAY_FILE_B");

            if (string.IsNullOrWhiteSpace(replayPathA) || string.IsNullOrWhiteSpace(replayPathB))
            {
                Assert.Inconclusive("Set REPLAY_FILE_A and REPLAY_FILE_B environment variables to run this manual test.");
            }

            ValidateReplayFile(replayPathA);
            ValidateReplayFile(replayPathB);
        }

        private static void ValidateReplayFile(string path)
        {
            Assert.IsTrue(File.Exists(path), $"Replay file not found: {path}");

            bool loaded = ReplayIO.TryLoad(path, out var surfaceHash, out var fps, out var frames);

            Assert.IsTrue(loaded, $"Replay file failed to load: {path}");
            Assert.IsTrue(surfaceHash != 0UL, $"Replay surface hash missing in {path}");
            Assert.IsTrue(fps > 0, $"Replay fps missing in {path}");
            Assert.IsTrue(frames.Count > 0, $"Replay has no frames in {path}");

            var lastIndex = frames[^1].FrameIndex;
            Assert.IsTrue(lastIndex >= 0, $"Replay frame indices invalid in {path}");

            foreach (var frame in frames)
            {
                Assert.IsNotNull(frame.ObjectStates, $"Replay frame missing objects in {path}");
                Assert.IsTrue(frame.ObjectStates.Count > 0, $"Replay frame has no objects in {path}");
            }
        }

        [TestMethod]
        public void Manual_AnalyzeReplayFile_FromEnvironment()
        {
            var replayPath = Environment.GetEnvironmentVariable("REPLAY_FILE_PATH");
            var logToFile = Environment.GetEnvironmentVariable("REPLAY_LOG_TO_FILE");
            var logFrames = Environment.GetEnvironmentVariable("REPLAY_LOG_FRAMES");
            if (string.IsNullOrWhiteSpace(replayPath))
            {
                Assert.Inconclusive("Set REPLAY_FILE_PATH to run this manual test.");
            }

            bool writeToFile = string.Equals(logToFile, "true", StringComparison.OrdinalIgnoreCase);
            bool writeFrames = string.Equals(logFrames, "true", StringComparison.OrdinalIgnoreCase);

            if (writeToFile)
            {
                Logger.EnableFileLogging = true;
                Logger.ClearLog();
            }

            Assert.IsTrue(File.Exists(replayPath), $"Replay file not found: {replayPath}");

            bool loaded = ReplayIO.TryLoad(replayPath, out var surfaceHash, out var fps, out var frames);
            Assert.IsTrue(loaded, $"Replay file failed to load: {replayPath}");

            int longestUnchanged = 0;
            int currentUnchanged = 0;
            int unchangedFrames = 0;
            float maxMapDelta = 0f;
            float maxShipDelta = 0f;

            Vector3? lastMap = null;
            Vector3? lastShipOffset = null;

            foreach (var frame in frames)
            {
                var mapPos = frame.GlobalMapPosition;
                var shipState = frame.ObjectStates.FirstOrDefault(s => string.Equals(s.ObjectName, "Ship", StringComparison.OrdinalIgnoreCase))
                    ?? frame.ObjectStates.FirstOrDefault();
                var shipOffset = shipState != null ? shipState.ObjectOffset : new Vector3();

                if (writeFrames)
                {
                    Logger.Log($"[ReplayFrame] index={frame.FrameIndex} map=({mapPos.x:0.##},{mapPos.y:0.##},{mapPos.z:0.##}) shipOff=({shipOffset.x:0.##},{shipOffset.y:0.##},{shipOffset.z:0.##})", "Replay");
                }

                if (lastMap != null)
                {
                    var mapDelta = Distance((Vector3)lastMap, mapPos);
                    var shipDelta = Distance((Vector3)lastShipOffset, shipOffset);

                    maxMapDelta = Math.Max(maxMapDelta, mapDelta);
                    maxShipDelta = Math.Max(maxShipDelta, shipDelta);

                    if (mapDelta < 0.001f && shipDelta < 0.001f)
                    {
                        currentUnchanged++;
                        unchangedFrames++;
                        longestUnchanged = Math.Max(longestUnchanged, currentUnchanged);
                    }
                    else
                    {
                        currentUnchanged = 0;
                    }
                }

                lastMap = mapPos;
                lastShipOffset = shipOffset;
            }

            var summary = $"Replay: {replayPath}\nSurfaceHash={surfaceHash} FPS={fps} Frames={frames.Count}\nUnchangedFrames={unchangedFrames} LongestUnchangedStreak={longestUnchanged}\nMaxMapDelta={maxMapDelta:0.###} MaxShipDelta={maxShipDelta:0.###}";
            Console.WriteLine(summary);
            if (writeToFile)
            {
                Logger.Log(summary.Replace("\n", " | "), "Replay");
                Logger.Flush();
            }
        }

        private static float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float dz = a.z - b.z;
            return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        [TestMethod]
        public void Manual_VerifyReplayMovementWindow_FromEnvironment()
        {
            var replayPath = Environment.GetEnvironmentVariable("REPLAY_FILE_PATH");
            var expectedStartText = Environment.GetEnvironmentVariable("REPLAY_EXPECT_START");
            var expectedStopText = Environment.GetEnvironmentVariable("REPLAY_EXPECT_STOP");

            if (string.IsNullOrWhiteSpace(replayPath) ||
                string.IsNullOrWhiteSpace(expectedStartText) ||
                string.IsNullOrWhiteSpace(expectedStopText))
            {
                Assert.Inconclusive("Set REPLAY_FILE_PATH, REPLAY_EXPECT_START, and REPLAY_EXPECT_STOP to run this manual test.");
            }

            Assert.IsTrue(int.TryParse(expectedStartText, out var expectedStart));
            Assert.IsTrue(int.TryParse(expectedStopText, out var expectedStop));

            Assert.IsTrue(File.Exists(replayPath), $"Replay file not found: {replayPath}");
            bool loaded = ReplayIO.TryLoad(replayPath, out _, out _, out var frames);
            Assert.IsTrue(loaded, $"Replay file failed to load: {replayPath}");

            var epsilonText = Environment.GetEnvironmentVariable("REPLAY_EPSILON");
            const float defaultEpsilon = 0.001f;
            float epsilon = defaultEpsilon;
            if (!string.IsNullOrWhiteSpace(epsilonText) && float.TryParse(epsilonText, out var parsedEpsilon))
                epsilon = parsedEpsilon;

            int firstMoveFrame = -1;
            int lastMoveFrame = -1;

            Vector3? lastMap = null;
            Vector3? lastShip = null;

            foreach (var frame in frames)
            {
                var mapPos = frame.GlobalMapPosition;
                var shipState = frame.ObjectStates.FirstOrDefault(s => string.Equals(s.ObjectName, "Ship", StringComparison.OrdinalIgnoreCase))
                    ?? frame.ObjectStates.FirstOrDefault();
                var shipOffset = shipState != null ? shipState.ObjectOffset : new Vector3();

                if (lastMap != null)
                {
                    var mapDelta = Distance((Vector3)lastMap, mapPos);
                    var shipDelta = Distance((Vector3)lastShip, shipOffset);
                    if (mapDelta > epsilon || shipDelta > epsilon)
                    {
                        if (firstMoveFrame < 0)
                            firstMoveFrame = frame.FrameIndex;
                        lastMoveFrame = frame.FrameIndex;
                    }
                }

                lastMap = mapPos;
                lastShip = shipOffset;
            }

            Assert.IsTrue(firstMoveFrame >= 0, "No movement detected in replay.");
            Assert.AreEqual(expectedStart, firstMoveFrame, $"Expected movement to start at frame {expectedStart} but got {firstMoveFrame}.");
            Assert.AreEqual(expectedStop, lastMoveFrame, $"Expected movement to stop at frame {expectedStop} but got {lastMoveFrame}.");
        }
    }
}
