using System.Threading.Tasks;
using Models.Requests;
using Models.Responses;
using Services.Interfaces;
using Services.Reports;

namespace Services
{
    public class ReportService : IReportService
    {
        private readonly OutpatientReportService _outpatientService;
        private readonly MedicalTechReportService _medicalTechService;
        private readonly DoctorAnalysisReportService _doctorAnalysisService;

        public ReportService(
            OutpatientReportService outpatientService,
            MedicalTechReportService medicalTechService,
            DoctorAnalysisReportService doctorAnalysisService)
        {
            _outpatientService = outpatientService;
            _medicalTechService = medicalTechService;
            _doctorAnalysisService = doctorAnalysisService;
        }

        public async Task<OutpatientReportResponse> GetOutpatientAppointmentsAsync(OutpatientReportRequest request)
        {
            return await _outpatientService.GetOutpatientAppointmentsAsync(request);
        }

        public async Task<MedicalTechReportResponse> GetMedicalTechAppointmentsAsync(MedicalTechReportRequest request)
        {
            return await _medicalTechService.GetMedicalTechAppointmentsAsync(request);
        }

        public async Task<MedicalTechSourceReportResponse> GetMedicalTechSourcesAsync(MedicalTechSourceReportRequest request)
        {
            return await _medicalTechService.GetMedicalTechSourcesAsync(request);
        }

        public async Task<MedicalTechItemDetailResponse> GetMedicalTechItemsAsync(MedicalTechItemDetailRequest request)
        {
            return await _medicalTechService.GetMedicalTechItemsAsync(request);
        }

        public async Task<OutpatientReportResponse> GetAppointmentTimeDistributionAsync(AppointmentTimeDistributionRequest request)
        {
            return await _outpatientService.GetAppointmentTimeDistributionAsync(request);
        }

        public async Task<DoctorAnalysisResponse> GetDoctorAppointmentAnalysisAsync(DoctorAnalysisReportRequest request)
        {
            return await _doctorAnalysisService.GetDoctorAppointmentAnalysisAsync(request);
        }
    }
}