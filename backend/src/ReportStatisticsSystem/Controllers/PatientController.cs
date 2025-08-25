using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Threading.Tasks;

namespace Controllers
{
    [ApiController]
    [Route("api/patient")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // GET /api/patient?patientId=123
        [HttpGet]
        public async Task<IActionResult> GetPatientName([FromQuery] string patientId)
        {
            var name = await _patientService.GetPatientNameAsync(patientId);
            return Ok(name);
        }
    }
}