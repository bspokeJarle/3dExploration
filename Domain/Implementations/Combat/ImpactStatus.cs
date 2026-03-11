namespace Domain
{
    public partial class _3dSpecificsImplementations
    {
        public class ImpactStatus : IImpactStatus
        {
            public bool HasCrashed { get; set; }
            public string ObjectName { get; set; }
            public ImpactDirection? ImpactDirection { get; set; }
            public IParticle SourceParticle { get; set; }
            public int? ObjectHealth { get; set; } = 100;
            public bool HasExploded { get; set; }
        }
    }
}
