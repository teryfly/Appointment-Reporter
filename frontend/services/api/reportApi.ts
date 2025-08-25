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

  // 映射后端返回到前端表格结构
  const response = await api.get(`/api/reports/outpatient-appointments?${queryParams.toString()}`);
  const rawList: any[] = response.data?.data ?? response.data ?? [];
  const mapped: OutpatientAppointmentRow[] = (rawList as any[]).map((r, idx) => ({
    id: r.id ?? `${r.orgId || ''}_${r.patientId || ''}_${r.date || ''}_${idx}`,
    date: r.date,
    department: r.orgName ?? '',
    patientName: r.patientName ?? '', // 新增：患者姓名
    totalSlots: r.slotCount ?? r.totalCount ?? 0, // 放号量
    appointmentCount: r.appointmentCount ?? 0,   // 预约量
    total: r.totalCount ?? (r.slotCount ?? 0)    // 汇总
  }));
  return mapped;
}

// 医技预约统计
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

  const { data } = await api.get(`/api/reports/medical-tech-appointments?${queryParams.toString()}`);
  return data || [];
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

  // 直接拿到 data 数组并进行字段映射，确保前端显示正确
  const response = await api.get(`/api/reports/medical-tech-items?${queryParams.toString()}`);
  const rawList: any[] = response.data?.data ?? response.data ?? [];
  // 将后端结构映射为前端表格行
  const mapped: MedicalExamDetailRow[] = (rawList as any[]).map((r, idx) => ({
    id: r.id ?? `${r.orgId || ''}_${r.itemCode || ''}_${r.date || ''}_${idx}`,
    date: r.date,
    orgId: r.orgId,
    department: r.orgName, // 展示科室名
    examItem: r.itemName,  // 展示项目名
    itemCode: r.itemCode,
    outpatientCount: r.outpatientCount ?? 0,
    inpatientCount: r.inpatientCount ?? 0,
    physicalExamCount: r.physicalExamCount ?? 0,
    total: r.totalCount ?? (r.outpatientCount ?? 0) + (r.inpatientCount ?? 0) + (r.physicalExamCount ?? 0),
  }));
  return mapped;
}

// 挂号预约时段分布
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

  const { data } = await api.get(`/api/reports/appointment-time-distribution?${queryParams.toString()}`);
  return data || [];
}

// 科室医生预约率
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

  const { data } = await api.get(`/api/reports/doctor-appointment-analysis?${queryParams.toString()}`);
  return data || [];
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
  // 这里可以根据需要调用相应的API获取检查项目
  // 目前返回模拟数据
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