using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Controllers
{
    [ApiController]
    [Route("api/doctors")]
    public class DoctorsController : ControllerBase
    {
        private readonly IPractitionerService _practitionerService;

        public DoctorsController(IPractitionerService practitionerService)
        {
            _practitionerService = practitionerService;
        }

        // GET /api/doctors?ids=123&ids=456
        // - ids omitted: return all doctors
        // - ids present: return only specified doctors
        [HttpGet]
        public async Task<IActionResult> GetDoctors([FromQuery(Name = "ids")] List<string>? ids)
        {
            var list = await _practitionerService.GetPractitionersAsync(ids);
            return Ok(new
            {
                success = true,
                data = list, // each item: { id, name }
                total = list.Count,
                message = "查询成功"
            });
        }
    }
}