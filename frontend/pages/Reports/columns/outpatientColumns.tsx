import React from 'react';
import type { ColumnsType } from 'antd/es/table';
import type { OutpatientAppointmentRow } from '../../../types/reportTypes';

export const columns: ColumnsType<OutpatientAppointmentRow> = [
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
  },
  {
    title: '医生',
    dataIndex: 'doctorName',
    key: 'doctorName',
    width: 120,
  },
  {
    title: '放号量',
    dataIndex: 'slotCount',
    key: 'slotCount',
    width: 100,
    align: 'right',
    render: (value: number) => value?.toLocaleString() || 0,
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