using GameplayHelpers.SurfaceIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Domain;
using System;
using System.IO;

namespace _3DSpesificsUnitTests
{
    [TestClass]
    public class SurfaceIOTests
    {
        [TestMethod]
        public void SaveAndLoad_Surface_RoundTripsData()
        {
            string path = Path.Combine(Path.GetTempPath(), $"surfaceio_{Guid.NewGuid():N}.ossd");

            var surface = new SurfaceData[2, 2];
            surface[0, 0] = new SurfaceData { mapDepth = 10, mapId = 1, hasLandbasedObject = true, isInfected = false };
            surface[0, 1] = new SurfaceData { mapDepth = 20, mapId = 2, hasLandbasedObject = false, isInfected = true };
            surface[1, 0] = new SurfaceData { mapDepth = 30, mapId = 3, hasLandbasedObject = false, isInfected = false };
            surface[1, 1] = new SurfaceData
            {
                mapDepth = 40,
                mapId = 4,
                hasLandbasedObject = true,
                isInfected = true,
                crashBox = new SurfaceData.CrashBoxData { width = 2, height = 3, boxDepth = 4 }
            };

            try
            {
                ulong savedHash = SurfaceIO.Save(path, surface);

                bool loaded = SurfaceIO.TryLoad(path, out var loadedSurface, out var loadedHash);

                Assert.IsTrue(loaded);
                Assert.AreEqual(savedHash, loadedHash);
                Assert.AreEqual(surface.GetLength(0), loadedSurface.GetLength(0));
                Assert.AreEqual(surface.GetLength(1), loadedSurface.GetLength(1));

                for (int z = 0; z < surface.GetLength(0); z++)
                {
                    for (int x = 0; x < surface.GetLength(1); x++)
                    {
                        var expected = surface[z, x];
                        var actual = loadedSurface[z, x];

                        Assert.AreEqual(expected.mapDepth, actual.mapDepth, $"Depth mismatch at [{z},{x}]");
                        Assert.AreEqual(expected.mapId, actual.mapId, $"MapId mismatch at [{z},{x}]");
                        Assert.AreEqual(expected.hasLandbasedObject, actual.hasLandbasedObject, $"Landbased flag mismatch at [{z},{x}]");
                        Assert.AreEqual(expected.isInfected, actual.isInfected, $"Infected flag mismatch at [{z},{x}]");

                        if (expected.crashBox.HasValue)
                        {
                            Assert.IsTrue(actual.crashBox.HasValue, $"CrashBox missing at [{z},{x}]");
                            Assert.AreEqual(expected.crashBox.Value.width, actual.crashBox.Value.width, $"CrashBox width mismatch at [{z},{x}]");
                            Assert.AreEqual(expected.crashBox.Value.height, actual.crashBox.Value.height, $"CrashBox height mismatch at [{z},{x}]");
                            Assert.AreEqual(expected.crashBox.Value.boxDepth, actual.crashBox.Value.boxDepth, $"CrashBox depth mismatch at [{z},{x}]");
                        }
                        else
                        {
                            Assert.IsFalse(actual.crashBox.HasValue, $"Unexpected CrashBox at [{z},{x}]");
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void TryLoad_ReturnsFalse_ForCorruptedFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"surfaceio_{Guid.NewGuid():N}.ossd");

            var surface = new SurfaceData[1, 1];
            surface[0, 0] = new SurfaceData
            {
                mapDepth = 10,
                mapId = 1,
                hasLandbasedObject = false,
                isInfected = false
            };

            try
            {
                SurfaceIO.Save(path, surface);

                var bytes = File.ReadAllBytes(path);
                if (bytes.Length > 0)
                {
                    bytes[^1] ^= 0xFF;
                    File.WriteAllBytes(path, bytes);
                }

                bool loaded = SurfaceIO.TryLoad(path, out var loadedSurface, out var loadedHash);

                Assert.IsFalse(loaded);
                Assert.AreEqual(0, loadedSurface.Length);
                Assert.AreEqual(0UL, loadedHash);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
