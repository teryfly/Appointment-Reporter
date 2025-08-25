using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public class PractitionerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public interface IPractitionerService
    {
        Task<List<PractitionerDto>> GetPractitionersAsync(List<string>? ids);
    }
}