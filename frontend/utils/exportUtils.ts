import type {
  OutpatientAppointmentRow,
  MedicalTechAppointmentRow,
  MedicalTechSourceRow,
  MedicalExamDetailRow,
  TimeSlotDistributionRow,
  DoctorAppointmentRateRow,
} from '../types/reportTypes';

export async function exportOutpatientReport(data: OutpatientAppointmentRow[]) {
  const xlsx = await import('xlsx');
  const ws = xlsx.utils.json_to_sheet(data.map(item => ({
    '日期': item.date,
    '科室': item.orgName,
    '医生': item.doctorName,
    '放号量（含加号）': item.slotCount,
    '预约量': item.appointmentCount
  })));
  const wb = xlsx.utils.book_new();
  xlsx.utils.book_append_sheet(wb, ws, '门诊预约统计');
  xlsx.writeFile(wb, `门诊预约统计_${new Date().toISOString().split('T')[0]}.xlsx`);
}

export async function exportMedTechReport(data: MedicalTechAppointmentRow[]) {
  const xlsx = await import('xlsx');
  const ws = xlsx.utils.json_to_sheet(data.map(item => ({
    '日期': (item as any).date,
    '科室': (item as any).orgName || (item as any).department || '',
    '检查类型': item.examType,
    '预约量': item.appointmentCount
  })));
  const wb = xlsx.utils.book_new();
  xlsx.utils.book_append_sheet(wb, ws, '医技预约统计');
  xlsx.writeFile(wb, `医技预约统计_${new Date().toISOString().split('T')[0]}.xlsx`);
}

export async function exportMedTechSourceReport(data: MedicalTechSourceRow[]) {
  const xlsx = await import('xlsx');
  const ws = xlsx.utils.json_to_sheet(data.map(item => ({
    '日期': item.date, // 使用 slot 映射后的 date
    '科室': item.department,
    '门诊预约量': item.outpatientCount,
    '住院预约量': item.inpatientCount,
    '体检预约量': item.physicalExamCount,
    '汇总': item.total
  })));
  const wb = xlsx.utils.book_new();
  xlsx.utils.book_append_sheet(wb, ws, '医技预约来源');
  xlsx.writeFile(wb, `医技预约来源_${new Date().toISOString().split('T')[0]}.xlsx`);
}

export async function exportMedExamDetailReport(data: MedicalExamDetailRow[]) {
  const xlsx = await import('xlsx');
  const ws = xlsx.utils.json_to_sheet(data.map(item => ({
    '日期': item.date,
    '科室': item.department,
    '项目代码': item.itemCode || '',
    '检查项目': item.examItem,
    '门诊预约量': item.outpatientCount,
    '住院预约量': item.inpatientCount,
    '体检预约量': item.physicalExamCount,
    '汇总': item.total
  })));
  const wb = xlsx.utils.book_new();
  xlsx.utils.book_append_sheet(wb, ws, '医技检查项目明细');
  xlsx.writeFile(wb, `医技检查项目明细_${new Date().toISOString().split('T')[0]}.xlsx`);
}

export async function exportTimeSlotReport(data: TimeSlotDistributionRow[]) {
  const xlsx = await import('xlsx');
  const ws = xlsx.utils.json_to_sheet(data.map(item => ({
    '时间': item.time,
    '科室': item.department,
    '医生': item.doctor,
    '预约量': item.appointmentCount,
    '就诊量': item.visitCount,
    '预约就诊率': `${(item.visitRate * 100).toFixed(2)}%`
  })));
  const wb = xlsx.utils.book_new();
  xlsx.utils.book_append_sheet(wb, ws, '挂号预约时段分布');
  xlsx.writeFile(wb, `挂号预约时段分布_${new Date().toISOString().split('T')[0]}.xlsx`);
}

export async function exportDoctorRateReport(data: DoctorAppointmentRateRow[]) {
  const xlsx = await import('xlsx');
  // 仅导出 科室、医生、开单量、预约量、预约率
  const ws = xlsx.utils.json_to_sheet(data.map(item => ({
    '科室': item.department,
    '医生': item.doctor,
    '开单量': item.orderCount,
    '预约量': item.appointmentCount,
    // 这里 item.appointmentRate 为 0-1，需要转为百分比显示
    '预约率': `${(item.appointmentRate * 100).toFixed(2)}%`
  })));
  const wb = xlsx.utils.book_new();
  xlsx.utils.book_append_sheet(wb, ws, '科室医生预约率');
  xlsx.writeFile(wb, `科室医生预约率_${new Date().toISOString().split('T')[0]}.xlsx`);
}