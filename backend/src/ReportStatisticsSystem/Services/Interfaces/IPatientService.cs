using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPatientService
    {
        Task<string> GetPatientNameAsync(string patientId);
    }
}