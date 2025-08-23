using System.Collections.Generic;
using System.Threading.Tasks;
using Models.Entities;

namespace ExternalServices.Interfaces
{
    public interface IOrganizationApiClient
    {
        Task<List<OrganizationEntity>> GetOrganizationsAsync(string sceneCode);
    }
}