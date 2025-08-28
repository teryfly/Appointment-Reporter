// Organization type for department
export interface Organization {
  id: string;
  name: string;
  code?: string;
  sequence?: string;
  type?: string;
}

// Doctor type
export interface Doctor {
  id: string;
  name: string;
  code?: string;
  departmentId?: string;
}

// Exam item for filter
export interface ExamItem {
  code: string;
  name: string;
  categoryName?: string;
}

// 门诊预约统计
export interface OutpatientAppointmentRow {
  id: string;
  date: string;
  orgId: string;
  orgName: string;
  doctorId: string;
  doctorName: string;
  personnelCount: number;    // 人员数量
  slotCount: number;         // 放号量
  appointmentCount: number;  // 预约量
  totalCount: number;        // 总数
}

// 医技预约统计
export interface MedicalTechAppointmentRow {
  id: string;
  date: string;
  department: string;
  examType: string; // 检查类型 (CT, DR, 超声等)
  appointmentCount: number;
}

// 医技预约来源
export interface MedicalTechSourceRow {
  id: string;
  date: string;
  department: string;
  outpatientCount: number; // 门诊预约量
  inpatientCount: number; // 住院预约量
  physicalExamCount: number; // 体检预约量
  total: number;
}

// 医技检查项目明细
export interface MedicalExamDetailRow {
  id: string;
  date: string;
  orgId?: string;      // 新增：后端返回 orgId
  department: string;  // 展示用科室名
  examItem: string;    // 展示用检查项目名
  itemCode?: string;   // 新增：后端返回 itemCode
  outpatientCount: number; // 门诊预约量
  inpatientCount: number;  // 住院预约量
  physicalExamCount: number; // 体检预约量
  total: number;        // 对应后端 totalCount
}

// 挂号预约时段分布（更新：使用新的返回结构，时间字段统一为 time）
export interface TimeSlotDistributionRow {
  id: string;
  time: string; // 时间（由后端 date 字段直接映射）
  department: string;
  doctor: string;
  appointmentCount: number; // 预约量
  visitCount: number; // 就诊量
  visitRate: number; // 预约就诊率（0-1）
  noShowRate?: number; // 爽约率（0-1），后端若未返回则可能为空
}

// 科室医生预约率
export interface DoctorAppointmentRateRow {
  id: string;
  date: string;
  department: string;
  doctor: string;
  orderCount: number; // 开单量
  appointmentCount: number; // 预约量
  appointmentRate: number; // 预约率
}

// API查询参数
export interface ReportQueryParams {
  startDate: string; // yyyy-MM-dd
  endDate: string; // yyyy-MM-dd
  orgIds?: string[]; // 科室ID列表
  groupBy: 'day' | 'month' | 'year'; // 聚合维度
  examTypes?: string[]; // 检查类型(医技预约统计)
  sourceTypes?: string[]; // 来源类型(医技预约来源)
  itemCodes?: string[]; // 项目代码(医技检查项目明细)
  timeInterval?: 'hour' | 'half-hour'; // 时间间隔(时段分布)
  doctorIds?: string[]; // 医生ID列表(医生预约率)
}