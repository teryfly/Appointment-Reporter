using System.Threading.Tasks;
using Models.Requests;
using Models.Responses;

namespace Services.Interfaces
{
    public interface IReportService
    {
        Task<OutpatientReportResponse> GetOutpatientAppointmentsAsync(OutpatientReportRequest request);
        Task<MedicalTechReportResponse> GetMedicalTechAppointmentsAsync(MedicalTechReportRequest request);
        Task<MedicalTechSourceReportResponse> GetMedicalTechSourcesAsync(MedicalTechSourceReportRequest request);
        Task<MedicalTechItemDetailResponse> GetMedicalTechItemsAsync(MedicalTechItemDetailRequest request);
        Task<OutpatientReportResponse> GetAppointmentTimeDistributionAsync(AppointmentTimeDistributionRequest request);
        Task<DoctorAnalysisResponse> GetDoctorAppointmentAnalysisAsync(DoctorAnalysisReportRequest request);
    }
}