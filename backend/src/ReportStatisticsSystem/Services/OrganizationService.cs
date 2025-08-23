using ExternalServices.Interfaces;
using Models.Entities;
using Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationApiClient _apiClient;

        public OrganizationService(IOrganizationApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<OrganizationEntity>> GetOrganizationsBySceneAsync(string sceneCode)
        {
            return await _apiClient.GetOrganizationsAsync(sceneCode);
        }

        public async Task<OrganizationEntity?> GetOrganizationByIdAsync(string sceneCode, string orgId)
        {
            var list = await _apiClient.GetOrganizationsAsync(sceneCode);
            return list.FirstOrDefault(x => x.Id == orgId);
        }
    }
}