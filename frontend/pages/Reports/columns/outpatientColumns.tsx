import React from 'react';
import type { ColumnsType } from 'antd/es/table';
import type { OutpatientAppointmentRow } from '../../../types/reportTypes';

export const columns: ColumnsType<OutpatientAppointmentRow> = [
  {
    title: '日期',
    dataIndex: 'date',
    key: 'date',
    width: 150,
    fixed: 'left',
  },
  {
    title: '科室',
    dataIndex: 'department',
    key: 'department',
    width: 160,
  },
  {
    title: '患者',
    dataIndex: 'patientName',
    key: 'patientName',
    width: 140,
  },
  {
    title: '放号量',
    dataIndex: 'totalSlots',
    key: 'totalSlots',
    width: 120,
    align: 'right',
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '预约量',
    dataIndex: 'appointmentCount',
    key: 'appointmentCount',
    width: 120,
    align: 'right',
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '汇总',
    dataIndex: 'total',
    key: 'total',
    width: 120,
    align: 'right',
    render: (value?: number, record) => (value ?? record.totalSlots ?? 0).toLocaleString(),
  },
];