using RetroMesh.Engine;
namespace TheOmegaStrain.Domain
{
    public class TriangleMeshWithColor : TriangleMesh, ITriangleMeshWithColorAndTexture
    {
        public string? Color { get; set; }
        public string? TextureId { get; set; }
        public TextureCoordinate Uv1 { get; set; }
        public TextureCoordinate Uv2 { get; set; }
        public TextureCoordinate Uv3 { get; set; }
    }
}
