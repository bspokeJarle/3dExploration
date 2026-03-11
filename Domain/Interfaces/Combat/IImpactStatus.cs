namespace Domain
{
    public interface IImpactStatus
    {
        bool HasExploded { get; set; }
        bool HasCrashed { get; set; }
        string ObjectName { get; set; }
        ImpactDirection? ImpactDirection { get; set; }
        IParticle? SourceParticle { get; set; }
        int? ObjectHealth { get; set; }
    }
}
