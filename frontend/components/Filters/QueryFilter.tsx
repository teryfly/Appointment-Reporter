import React from 'react';
import { Space, Button } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import DateRangeFilter, { DateType } from './DateRangeFilter';
import DepartmentFilter from './DepartmentFilter';

interface QueryFilterProps {
  dateValue: { 
    type: DateType; 
    startDate: any; 
    endDate: any; 
  };
  onDateChange: (v: { 
    type: DateType; 
    startDate: any; 
    endDate: any; 
  }) => void;
  departmentValue?: string;
  onDepartmentChange?: (v: string) => void;
  onQuery: () => void;
  loading?: boolean;
  extra?: React.ReactNode;
  sceneCode?: '01' | '02'; // 新增场景代码参数
}

const QueryFilter: React.FC<QueryFilterProps> = ({
  dateValue,
  onDateChange,
  departmentValue,
  onDepartmentChange,
  onQuery,
  loading,
  extra,
  sceneCode = '01',
}) => {
  const canQuery = dateValue.startDate != null && dateValue.endDate != null;
  
  return (
    <Space size="middle" wrap>
      <DateRangeFilter value={dateValue} onChange={onDateChange} />
      <DepartmentFilter 
        value={departmentValue} 
        onChange={onDepartmentChange}
        sceneCode={sceneCode}
      />
      {extra}
      <Button
        type="primary"
        icon={<SearchOutlined />}
        onClick={onQuery}
        loading={loading}
        disabled={!canQuery}
      >
        查询
      </Button>
    </Space>
  );
};

export default QueryFilter;