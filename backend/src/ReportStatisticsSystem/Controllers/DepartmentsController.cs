using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Controllers
{
    [ApiController]
    [Route("api/departments")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IOrganizationService _organizationService;
        public DepartmentsController(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }
        // GET /api/departments?sceneCode=01
        // sceneCode: 门诊=01, 医技=02; not provided => return all unique departments from both 01 and 02
        [HttpGet]
        public async Task<IActionResult> GetDepartments([FromQuery] string? sceneCode)
        {
            var list = new List<object>();
            if (string.IsNullOrWhiteSpace(sceneCode))
            {
                // If no scene specified, merge 01 and 02 and distinct by id
                var o1 = await _organizationService.GetOrganizationsBySceneAsync("01");
                var o2 = await _organizationService.GetOrganizationsBySceneAsync("02");
                list = o1.Concat(o2)
                         .GroupBy(x => x.Id)
                         .Select(g => new { id = g.Key, name = g.First().Name })
                         .Cast<object>()
                         .ToList();
            }
            else
            {
                var orgs = await _organizationService.GetOrganizationsBySceneAsync(sceneCode);
                list = orgs.Select(o => new { id = o.Id, name = o.Name })
                           .Cast<object>()
                           .ToList();
            }
            return Ok(new
            {
                success = true,
                data = list,
                total = list.Count,
                message = "查询成功"
            });
        }
    }
}