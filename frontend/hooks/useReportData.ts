import { useState } from 'react';
import {
  getOutpatientAppointments,
  getMedicalTechAppointments,
  getMedicalTechSources,
  getMedicalExamDetails,
  getTimeSlotDistributions,
  getDoctorAppointmentRates,
} from '../services/api/reportApi';

interface UseReportDataParams {
  type:
    | 'outpatient'
    | 'medtech'
    | 'medtechsource'
    | 'medexamdetail'
    | 'timeslot'
    | 'doctorrate';
}

// 新的查询参数接口
interface QueryParams {
  startDate: string; // yyyy-MM-dd
  endDate: string; // yyyy-MM-dd
  groupBy: 'day' | 'month' | 'year';
  orgIds?: string[];
  examTypes?: string[];
  sourceTypes?: string[];
  itemCodes?: string[];
  timeInterval?: 'hour' | 'half-hour';
  doctorIds?: string[];
}

export function useReportData<T>(params: UseReportDataParams) {
  const [data, setData] = useState<T[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | undefined>();

  const fetchData = async (queryParams: QueryParams) => {
    setLoading(true);
    setError(undefined);
    try {
      let result: T[] = [];
      switch (params.type) {
        case 'outpatient':
          result = await getOutpatientAppointments({
            startDate: queryParams.startDate,
            endDate: queryParams.endDate,
            groupBy: queryParams.groupBy,
            orgIds: queryParams.orgIds,
          }) as T[];
          break;
        case 'medtech':
          result = await getMedicalTechAppointments({
            startDate: queryParams.startDate,
            endDate: queryParams.endDate,
            groupBy: queryParams.groupBy,
            orgIds: queryParams.orgIds,
            examTypes: queryParams.examTypes,
          }) as T[];
          break;
        case 'medtechsource':
          result = await getMedicalTechSources({
            startDate: queryParams.startDate,
            endDate: queryParams.endDate,
            groupBy: queryParams.groupBy,
            orgIds: queryParams.orgIds,
            sourceTypes: queryParams.sourceTypes,
          }) as T[];
          break;
        case 'medexamdetail':
          result = await getMedicalExamDetails({
            startDate: queryParams.startDate,
            endDate: queryParams.endDate,
            groupBy: queryParams.groupBy,
            orgIds: queryParams.orgIds,
            itemCodes: queryParams.itemCodes,
          }) as T[];
          break;
        case 'timeslot':
          result = await getTimeSlotDistributions({
            startDate: queryParams.startDate,
            endDate: queryParams.endDate,
            groupBy: queryParams.groupBy,
            orgIds: queryParams.orgIds,
            timeInterval: queryParams.timeInterval,
          }) as T[];
          break;
        case 'doctorrate':
          result = await getDoctorAppointmentRates({
            startDate: queryParams.startDate,
            endDate: queryParams.endDate,
            groupBy: queryParams.groupBy,
            orgIds: queryParams.orgIds,
            doctorIds: queryParams.doctorIds,
          }) as T[];
          break;
        default:
          result = [];
      }
      setData(result);
    } catch (err) {
      setError(err as Error);
    } finally {
      setLoading(false);
    }
  };

  return { data, loading, error, fetchData };
}