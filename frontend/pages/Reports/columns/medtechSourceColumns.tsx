import React from 'react';
import type { ColumnsType } from 'antd/es/table';
import type { MedicalTechSourceRow } from '../../../types/reportTypes';

export const columns: ColumnsType<MedicalTechSourceRow> = [
  {
    title: '日期',
    dataIndex: 'date',
    key: 'date',
    width: 120,
    fixed: 'left',
  },
  {
    title: '科室',
    dataIndex: 'department',
    key: 'department',
    width: 150,
  },
  {
    title: '门诊预约量',
    dataIndex: 'outpatientCount',
    key: 'outpatientCount',
    width: 120,
    align: 'right',
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '住院预约量',
    dataIndex: 'inpatientCount',
    key: 'inpatientCount',
    width: 120,
    align: 'right',
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '体检预约量',
    dataIndex: 'physicalExamCount',
    key: 'physicalExamCount',
    width: 120,
    align: 'right',
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '汇总',
    dataIndex: 'total',
    key: 'total',
    width: 100,
    align: 'right',
    render: (value: number) => value?.toLocaleString() || 0,
  },
];