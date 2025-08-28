using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Requests;
using Models.Responses;
using Services.Interfaces;

namespace Services.Reports
{
    public class DoctorAnalysisReportService
    {
        private readonly IFhirServiceRequestService _srService;
        private readonly IFhirLookupService _lookup;

        public DoctorAnalysisReportService(IFhirServiceRequestService srService, IFhirLookupService lookup)
        {
            _srService = srService;
            _lookup = lookup;
        }

        public async Task<DoctorAnalysisResponse> GetDoctorAppointmentAnalysisAsync(DoctorAnalysisReportRequest request)
        {
            var filter = new ServiceRequestFilter
            {
                Start = request.StartDate,
                End = request.EndDate,
                ExecOrgIds = request.ExecOrgIds ?? request.OrgIds,
                DoctorIds = request.DoctorIds
            };

            var orders = await _srService.CountOrdersAsync(filter);
            var appointments = await _srService.CountAppointmentsAsync(filter);

            var pairs = new HashSet<(string Dept, string Doc)>(orders.Keys.Select(k => (k.DepartmentId, k.DoctorId)));
            foreach (var k in appointments.Keys) pairs.Add((k.DepartmentId, k.DoctorId));

            var deptIds = pairs.Select(p => p.Dept).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var docIds = pairs.Select(p => p.Doc).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

            var deptNames = await _lookup.GetOrganizationNamesAsync(deptIds);
            var docNames = await _lookup.GetPractitionerNamesAsync(docIds);

            var list = new List<DoctorAnalysisItem>();
            foreach (var (deptId, docId) in pairs.OrderBy(x => x.Dept).ThenBy(x => x.Doc))
            {
                var oKey = new ServiceRequestStatKey { DepartmentId = deptId, DoctorId = docId };
                var ordersCount = orders.TryGetValue(oKey, out var oc) ? oc : 0;
                var apptCount = appointments.TryGetValue(oKey, out var ac) ? ac : 0;

                var rate = ordersCount > 0 ? Math.Round((double)apptCount / ordersCount * 100, 2) : 0;

                list.Add(new DoctorAnalysisItem
                {
                    DepartmentId = deptId,
                    DepartmentName = deptNames.TryGetValue(deptId, out var dn) ? dn : "",
                    DoctorId = docId,
                    DoctorName = docNames.TryGetValue(docId, out var pn) ? pn : "",
                    OrdersCount = ordersCount,
                    AppointmentCount = apptCount,
                    AppointmentRate = rate
                });
            }

            return new DoctorAnalysisResponse
            {
                Success = true,
                Data = list,
                Total = list.Count,
                Message = "查询成功"
            };
        }
    }
}