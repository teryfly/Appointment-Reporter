import type { ColumnsType } from 'antd/es/table';
import type { MedicalTechAppointmentRow } from '../../../types/reportTypes';

export const columns: ColumnsType<MedicalTechAppointmentRow> = [
  {
    title: '日期',
    dataIndex: 'date',
    key: 'date',
    width: 120,
    fixed: 'left',
  },
  {
    title: '科室',
    dataIndex: 'orgName',
    key: 'orgName',
    width: 150,
    render: (_: any, record) => (record as any).orgName || (record as any).department || '',
  },
  {
    title: '检查类型',
    dataIndex: 'examType',
    key: 'examType',
    width: 120,
  },
  {
    title: '预约量',
    dataIndex: 'appointmentCount',
    key: 'appointmentCount',
    width: 100,
    align: 'right',
    render: (value: number) => value?.toLocaleString() || 0,
  },
];