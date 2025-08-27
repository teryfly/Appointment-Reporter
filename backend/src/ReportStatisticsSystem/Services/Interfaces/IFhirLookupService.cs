using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IFhirLookupService
    {
        Task<Dictionary<string, string>> GetOrganizationNamesAsync(IEnumerable<string> orgIds);
        Task<Dictionary<string, string>> GetPractitionerNamesAsync(IEnumerable<string> practitionerIds);
    }
}