using Microsoft.AspNetCore.Mvc;
using Models.Requests;
using Services.Interfaces;
using System.Threading.Tasks;
using Models.Responses;

namespace Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("outpatient-appointments")]
        public async Task<IActionResult> GetOutpatientAppointments([FromQuery] OutpatientReportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reportService.GetOutpatientAppointmentsAsync(request);
            return Ok(result);
        }

        [HttpGet("medical-tech-appointments")]
        public async Task<IActionResult> GetMedicalTechAppointments([FromQuery] MedicalTechReportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reportService.GetMedicalTechAppointmentsAsync(request);
            return Ok(result);
        }

        [HttpGet("medical-tech-sources")]
        public async Task<IActionResult> GetMedicalTechSources([FromQuery] MedicalTechSourceReportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reportService.GetMedicalTechSourcesAsync(request);
            return Ok(result);
        }

        [HttpGet("medical-tech-items")]
        public async Task<IActionResult> GetMedicalTechItems([FromQuery] MedicalTechItemDetailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reportService.GetMedicalTechItemsAsync(request);
            return Ok(result);
        }

        [HttpGet("appointment-time-distribution")]
        public async Task<IActionResult> GetAppointmentTimeDistribution([FromQuery] AppointmentTimeDistributionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reportService.GetAppointmentTimeDistributionAsync(request);
            return Ok(result);
        }

        [HttpGet("doctor-appointment-analysis")]
        public async Task<IActionResult> GetDoctorAppointmentAnalysis([FromQuery] DoctorAnalysisReportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reportService.GetDoctorAppointmentAnalysisAsync(request);
            return Ok(result);
        }
    }
}