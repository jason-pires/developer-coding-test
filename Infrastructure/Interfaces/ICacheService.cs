using Application.Interfaces;
using Infrastructure.Interfaces.AssemblyMaker;

namespace Infrastructure.Interfaces
{
    public interface ICacheService : ICacheProvider, IInfrastructureAssemblyMarker
    {
    }
}
