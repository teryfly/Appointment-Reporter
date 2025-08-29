import { api } from './baseApi';
import type {
  OutpatientAppointmentRow,
  MedicalTechAppointmentRow,
  MedicalTechSourceRow,
  MedicalExamDetailRow,
  TimeSlotDistributionRow,
  DoctorAppointmentRateRow,
  Organization,
  Doctor,
  ExamItem,
  ReportQueryParams
} from '../../types/reportTypes';

// 通用查询参数接口
interface BaseQueryParams {
  startDate: string; // yyyy-MM-dd
  endDate: string; // yyyy-MM-dd
  groupBy: 'day' | 'month' | 'year';
  orgIds?: string[];
}

// 门诊预约统计
export async function getOutpatientAppointments(params: BaseQueryParams): Promise<OutpatientAppointmentRow[]> {
  const queryParams = new URLSearchParams({
    StartDate: params.startDate,
    EndDate: params.endDate,
    GroupBy: params.groupBy
  });

  if (params.orgIds?.length) {
    params.orgIds.forEach(id => queryParams.append('OrgIds', id));
  }

  const response = await api.get(`/api/reports/outpatient-appointments?${queryParams.toString()}`);
  const rawList: any[] = response.data?.data ?? response.data ?? [];
  
  const mapped: OutpatientAppointmentRow[] = rawList.map((r, idx) => ({
    id: r.id ?? `${r.orgId}_${r.doctorId}_${r.date}_${idx}`,
    date: r.date,
    orgId: r.orgId,
    orgName: r.orgName,
    doctorId: r.doctorId,
    doctorName: r.doctorName,
    personnelCount: r.personnelCount ?? 0,
    slotCount: r.slotCount ?? 0,
    appointmentCount: r.appointmentCount ?? 0,
    totalCount: r.totalCount ?? 0,
  }));
  
  return mapped;
}

// 医技预约统计（更新：slot 字段作为 date，orgName, examType, appointmentCount）
export async function getMedicalTechAppointments(params: BaseQueryParams & {
  examTypes?: string[];
}): Promise<MedicalTechAppointmentRow[]> {
  const queryParams = new URLSearchParams({
    StartDate: params.startDate,
    EndDate: params.endDate,
    GroupBy: params.groupBy
  });

  if (params.orgIds?.length) {
    params.orgIds.forEach(id => queryParams.append('OrgIds', id));
  }

  if (params.examTypes?.length) {
    params.examTypes.forEach(type => queryParams.append('ExamTypes', type));
  }

  const response = await api.get(`/api/reports/medical-tech-appointments?${queryParams.toString()}`);
  const rawList: any[] = response.data?.data ?? response.data ?? [];
  
  // 兼容 slot 字段为日期
  const mapped: MedicalTechAppointmentRow[] = rawList.map((r, idx) => ({
    id: r.id ?? `${r.orgId || ''}_${r.examType || ''}_${r.slot || ''}_${idx}`,
    date: r.slot ?? r.date ?? '', // slot 字段作为日期
    department: r.orgName ?? r.department ?? '',
    examType: r.examType ?? '',
    appointmentCount: r.appointmentCount ?? 0,
  }));

  return mapped;
}

// 医技预约来源
export async function getMedicalTechSources(params: BaseQueryParams & {
  sourceTypes?: string[];
}): Promise<MedicalTechSourceRow[]> {
  const queryParams = new URLSearchParams({
    StartDate: params.startDate,
    EndDate: params.endDate,
    GroupBy: params.groupBy
  });

  if (params.orgIds?.length) {
    params.orgIds.forEach(id => queryParams.append('OrgIds', id));
  }

  if (params.sourceTypes?.length) {
    params.sourceTypes.forEach(type => queryParams.append('SourceTypes', type));
  }

  const { data } = await api.get(`/api/reports/medical-tech-sources?${queryParams.toString()}`);
  return data || [];
}

// 医技检查项目明细
export async function getMedicalExamDetails(params: BaseQueryParams & {
  itemCodes?: string[];
}): Promise<MedicalExamDetailRow[]> {
  const queryParams = new URLSearchParams({
    StartDate: params.startDate,
    EndDate: params.endDate,
    GroupBy: params.groupBy
  });

  if (params.orgIds?.length) {
    params.orgIds.forEach(id => queryParams.append('OrgIds', id));
  }

  if (params.itemCodes?.length) {
    params.itemCodes.forEach(code => queryParams.append('ItemCodes', code));
  }

  const response = await api.get(`/api/reports/medical-tech-items?${queryParams.toString()}`);
  const rawList: any[] = response.data?.data ?? response.data ?? [];
  const mapped: MedicalExamDetailRow[] = (rawList as any[]).map((r, idx) => ({
    id: r.id ?? `${r.orgId || ''}_${r.itemCode || ''}_${r.date || ''}_${idx}`,
    date: r.date,
    orgId: r.orgId,
    department: r.orgName,
    examItem: r.itemName,
    itemCode: r.itemCode,
    outpatientCount: r.outpatientCount ?? 0,
    inpatientCount: r.inpatientCount ?? 0,
    physicalExamCount: r.physicalExamCount ?? 0,
    total: r.totalCount ?? (r.outpatientCount ?? 0) + (r.inpatientCount ?? 0) + (r.physicalExamCount ?? 0),
  }));
  return mapped;
}

// 挂号预约时段分布（更新：visitCount 使用 totalCount；计算 visitRate / noShowRate）
export async function getTimeSlotDistributions(params: BaseQueryParams & {
  timeInterval?: 'hour' | 'half-hour';
}): Promise<TimeSlotDistributionRow[]> {
  const queryParams = new URLSearchParams({
    StartDate: params.startDate,
    EndDate: params.endDate,
    GroupBy: params.groupBy
  });

  if (params.orgIds?.length) {
    params.orgIds.forEach(id => queryParams.append('OrgIds', id));
  }

  if (params.timeInterval) {
    queryParams.append('TimeInterval', params.timeInterval);
  }

  const response = await api.get(`/api/reports/appointment-time-distribution?${queryParams.toString()}`);
  const rawList: any[] = response.data?.data ?? response.data ?? [];

  const mapped: TimeSlotDistributionRow[] = rawList.map((r, idx) => {
    const appointment = Number(r.appointmentCount ?? 0);
    // 按需求：就诊量取 totalCount
    const visits = Number(r.totalCount ?? 0);
    const visitRate = appointment > 0 ? visits / appointment : 0; // 0-1
    const noShowRate = appointment > 0 ? 1 - visitRate : undefined; // 0-1，若无预约量显示为 '-'
    return {
      id: r.id ?? `${r.orgId || ''}_${r.doctorId || ''}_${r.date || ''}_${idx}`,
      time: r.time ?? r.date ?? '',
      department: r.department ?? r.orgName ?? '',
      doctor: r.doctor ?? r.doctorName ?? '',
      appointmentCount: appointment,
      visitCount: visits,
      visitRate,
      noShowRate,
    };
  });

  return mapped;
}

// 科室医生预约率 - 按新API返回结构映射
export async function getDoctorAppointmentRates(params: BaseQueryParams & {
  doctorIds?: string[];
}): Promise<DoctorAppointmentRateRow[]> {
  const queryParams = new URLSearchParams({
    StartDate: params.startDate,
    EndDate: params.endDate,
    GroupBy: params.groupBy
  });

  if (params.orgIds?.length) {
    params.orgIds.forEach(id => queryParams.append('OrgIds', id));
  }

  if (params.doctorIds?.length) {
    params.doctorIds.forEach(id => queryParams.append('DoctorIds', id));
  }

  const response = await api.get(`/api/reports/doctor-appointment-analysis?${queryParams.toString()}`);
  const rawList: any[] = response.data?.data ?? response.data ?? [];

  const mapped: DoctorAppointmentRateRow[] = rawList.map((r, idx) => ({
    id: r.id ?? `${r.departmentId || ''}_${r.doctorId || ''}_${idx}`,
    date: r.date ?? '',
    department: r.departmentName || '',
    doctor: r.doctorName || r.doctorId || '',
    orderCount: r.ordersCount ?? 0,
    appointmentCount: r.appointmentCount ?? 0,
    appointmentRate: (typeof r.appointmentRate === 'number') ? (r.appointmentRate / 100) : 0,
  }));

  return mapped;
}

// 获取科室列表
export async function getDepartments(sceneCode?: string): Promise<Organization[]> {
  try {
    const queryParams = new URLSearchParams();
    if (sceneCode) {
      queryParams.append('sceneCode', sceneCode);
    }

    const { data } = await api.get(`/api/departments?${queryParams.toString()}`);
    return data || [];
  } catch (error) {
    console.error('获取科室列表失败:', error);
    return [];
  }
}

// 根据场景代码获取科室列表（兼容旧接口）
export async function getDepartmentsByScene(sceneCode: '01' | '02'): Promise<Organization[]> {
  return getDepartments(sceneCode);
}

// 获取门诊科室
export async function getOutpatientDepartments(): Promise<Organization[]> {
  return getDepartments('01');
}

// 获取医技科室
export async function getMedtechDepartments(): Promise<Organization[]> {
  return getDepartments('02');
}

// 获取医生列表
export async function getDoctors(ids?: string[]): Promise<Doctor[]> {
  try {
    const queryParams = new URLSearchParams();
    if (ids?.length) {
      ids.forEach(id => queryParams.append('ids', id));
    }

    const { data } = await api.get(`/api/doctors?${queryParams.toString()}`);
    return data || [];
  } catch (error) {
    console.error('获取医生列表失败:', error);
    return [];
  }
}

// 获取检查项目列表（模拟数据，实际可能需要额外的API）
export async function getExamItems(departmentId?: string): Promise<ExamItem[]> {
  const mockData: Record<string, ExamItem[]> = {
    '30700': [
      { code: 'CT001', name: 'CT头部平扫' },
      { code: 'CT002', name: 'CT胸部平扫' },
      { code: 'DR001', name: 'DR胸部正侧位' },
    ],
    '30800': [
      { code: 'US001', name: '腹部超声' },
      { code: 'US002', name: '心脏超声' },
      { code: 'US003', name: '甲状腺超声' },
    ],
    '30900': [
      { code: 'LAB001', name: '血常规' },
      { code: 'LAB002', name: '尿常规' },
      { code: 'LAB003', name: '肝功能' },
    ],
  };
  return Promise.resolve(departmentId ? (mockData[departmentId] || []) : []);
}