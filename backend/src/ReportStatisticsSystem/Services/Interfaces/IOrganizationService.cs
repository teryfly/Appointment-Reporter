using System.Collections.Generic;
using System.Threading.Tasks;
using Models.Entities;

namespace Services.Interfaces
{
    public interface IOrganizationService
    {
        Task<List<OrganizationEntity>> GetOrganizationsBySceneAsync(string sceneCode);
        Task<OrganizationEntity?> GetOrganizationByIdAsync(string sceneCode, string orgId);
    }
}