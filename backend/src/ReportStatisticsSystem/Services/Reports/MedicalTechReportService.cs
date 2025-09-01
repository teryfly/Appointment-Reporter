using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Requests;
using Models.Responses;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Fhir;

namespace Services.Reports
{
    public class MedicalTechReportService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IOrganizationService _orgService;
        private readonly FhirMedicalTechAggregationService _fhirAgg;
        private readonly FhirServiceRequestSourceAggregationService _fhirSourceAgg;
        private readonly FhirMedicalTechItemAggregationService _fhirItemAgg;

        public MedicalTechReportService(
            IAppointmentRepository repository,
            IOrganizationService orgService,
            FhirMedicalTechAggregationService fhirAgg,
            FhirServiceRequestSourceAggregationService fhirSourceAgg,
            FhirMedicalTechItemAggregationService fhirItemAgg)
        {
            _repository = repository;
            _orgService = orgService;
            _fhirAgg = fhirAgg;
            _fhirSourceAgg = fhirSourceAgg;
            _fhirItemAgg = fhirItemAgg;
        }

        public async Task<MedicalTechReportResponse> GetMedicalTechAppointmentsAsync(MedicalTechReportRequest request)
        {
            var performerOrgIds = request.ExecOrgIds ?? request.OrgIds;

            var rows = await _fhirAgg.QueryAppointmentsAsync(
                request.StartDate,
                request.EndDate,
                request.GroupBy,
                performerOrgIds
            );

            var orgs = await _orgService.GetOrganizationsBySceneAsync("02");
            var orgNameMap = orgs.ToDictionary(o => o.Id, o => o.Name);

            var wantedCategories = request.ExamTypes?.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet()
                                   ?? new HashSet<string>();

            var items = new List<MedicalTechReportItem>();
            foreach (var r in rows)
            {
                if (wantedCategories.Count > 0 && !wantedCategories.Contains(r.CategoryDisplay))
                    continue;

                items.Add(new MedicalTechReportItem
                {
                    Slot = r.Slot,
                    OrgId = r.DepartmentId,
                    OrgName = orgNameMap.TryGetValue(r.DepartmentId, out var n) ? n : "",
                    ExamType = r.CategoryDisplay,
                    AppointmentCount = r.Count,
                    CompletedCount = 0,
                    CancelledCount = 0
                });
            }

            return new MedicalTechReportResponse
            {
                Success = true,
                Data = items,
                Total = items.Count,
                Message = "查询成功"
            };
        }

        public async Task<MedicalTechSourceReportResponse> GetMedicalTechSourcesAsync(MedicalTechSourceReportRequest request)
        {
            var performerOrgIds = request.ExecOrgIds ?? request.OrgIds;

            var rows = await _fhirSourceAgg.AggregateRowsAsync(
                request.StartDate,
                request.EndDate,
                request.GroupBy,
                performerOrgIds
            );

            var orgs = await _orgService.GetOrganizationsBySceneAsync("02");
            var orgNameMap = orgs.ToDictionary(o => o.Id, o => o.Name);

            var data = rows.Select(r => new MedicalTechSourceFlatItem
            {
                OrgId = r.OrgId,
                OrgName = orgNameMap.TryGetValue(r.OrgId, out var n) ? n : "",
                Slot = r.Slot,
                OutpatientCount = r.Outpatient,
                InpatientCount = r.Inpatient,
                PhysicalExamCount = r.PhysicalExam,
                TotalCount = r.Outpatient + r.Inpatient + r.PhysicalExam
            })
            .OrderBy(x => x.Slot)
            .ThenBy(x => x.OrgId)
            .ToList();

            return new MedicalTechSourceReportResponse
            {
                Success = true,
                Data = data,
                Total = data.Count,
                Message = "查询成功"
            };
        }

        // Updated: include Date slot according to groupBy
        public async Task<MedicalTechItemResponse> GetMedicalTechItemsV2Async(MedicalTechItemDetailRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;
            var itemCodes = request.ItemCodes;

            var rows = await _fhirItemAgg.AggregateAsync(
                request.StartDate,
                request.EndDate,
                request.GroupBy,
                execOrgIds,
                itemCodes
            );

            var orgs = await _orgService.GetOrganizationsBySceneAsync("02");
            var orgNameDict = orgs.ToDictionary(o => o.Id, o => o.Name);

            var data = rows.Select(r => new MedicalTechItemRow
            {
                Date = r.Slot, // slot formatted by groupBy: yyyy / yyyy-MM / yyyy-MM-dd
                OrgId = r.OrgId,
                OrgName = orgNameDict.TryGetValue(r.OrgId, out var on) ? on : "",
                ItemCode = r.ItemCode,
                ItemName = r.ItemDisplay,
                OutpatientCount = r.Outpatient,
                InpatientCount = r.Inpatient,
                PhysicalExamCount = r.PhysicalExam,
                TotalCount = r.Outpatient + r.Inpatient + r.PhysicalExam
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.OrgId)
            .ThenBy(x => x.ItemCode)
            .ThenBy(x => x.ItemName)
            .ToList();

            var summary = new MedicalTechItemSummary
            {
                OutpatientTotal = data.Sum(x => x.OutpatientCount),
                InpatientTotal = data.Sum(x => x.InpatientCount),
                PhysicalExamTotal = data.Sum(x => x.PhysicalExamCount),
                GrandTotal = data.Sum(x => x.TotalCount)
            };

            return new MedicalTechItemResponse
            {
                Success = true,
                Data = data,
                Total = data.Count,
                Message = "查询成功",
                Summary = summary
            };
        }
    }
}