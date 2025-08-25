import React, { useState } from 'react';
import { Card, message, Alert, Space, Button, Statistic, Row, Col, Select } from 'antd';
import { SearchOutlined, ReloadOutlined } from '@ant-design/icons';
import DateRangeFilter from '../../components/Filters/DateRangeFilter';
import DepartmentFilter from '../../components/Filters/DepartmentFilter';
import ReportTable from '../../components/Tables/ReportTable';
import ExportButton from '../../components/Tables/ExportButton';
import { useReportData } from '../../hooks/useReportData';
import type { MedicalTechAppointmentRow } from '../../types/reportTypes';
import { columns } from './columns/medtechColumns';
import { exportMedTechReport } from '../../utils/exportUtils';
import dayjs, { Dayjs } from 'dayjs';

const defaultDate = { 
  type: 'day' as const, 
  startDate: dayjs().subtract(7, 'day'), 
  endDate: dayjs() 
};

// 检查类型选项
const examTypeOptions = [
  { label: 'CT', value: 'CT' },
  { label: 'DR', value: 'DR' },
  { label: '超声', value: 'US' },
  { label: 'MRI', value: 'MRI' },
  { label: '内镜', value: 'ENDO' },
];

const MedicalTechAppointmentReport: React.FC = () => {
  const [dateValue, setDateValue] = useState<{ 
    type: 'day' | 'month' | 'year'; 
    startDate: Dayjs | null; 
    endDate: Dayjs | null; 
  }>(defaultDate);
  const [departmentId, setDepartmentId] = useState<string | undefined>();
  const [examTypes, setExamTypes] = useState<string[]>([]);
  const { data, loading, error, fetchData } = useReportData<MedicalTechAppointmentRow>({
    type: 'medtech',
  });

  const handleQuery = () => {
    if (!dateValue.startDate || !dateValue.endDate) {
      message.warning('请选择开始日期和结束日期');
      return;
    }

    const start =
      dateValue.type === 'month'
        ? dateValue.startDate.startOf('month')
        : dateValue.type === 'year'
          ? dateValue.startDate.startOf('year')
          : dateValue.startDate;
    const end =
      dateValue.type === 'month'
        ? dateValue.endDate.endOf('month')
        : dateValue.type === 'year'
          ? dateValue.endDate.endOf('year')
          : dateValue.endDate;

    fetchData({
      startDate: start.format('YYYY-MM-DD'),
      endDate: end.format('YYYY-MM-DD'),
      groupBy: dateValue.type,
      orgIds: departmentId ? [departmentId] : undefined,
      examTypes,
    });
  };

  const handleExport = () => {
    if (!data || data.length === 0) {
      message.warning('无可导出数据');
      return;
    }
    exportMedTechReport(data);
    message.success('导出成功');
  };

  const handleReset = () => {
    setDateValue(defaultDate);
    setDepartmentId(undefined);
    setExamTypes([]);
  };

  // 计算汇总数据
  const totalAppointments = data.reduce((sum, item) => sum + (item.appointmentCount || 0), 0);
  const examTypeCount = new Set(data.map(item => item.examType)).size;

  const canQuery = dateValue.startDate != null && dateValue.endDate != null;

  return (
    <Card
      title="医技预约统计"
      extra={
        <Space>
          <ExportButton onExport={handleExport} disabled={!data || data.length === 0} />
        </Space>
      }
      styles={{ body: { padding: 0 } }}
    >
      {/* 查询条件 */}
      <div style={{ padding: 16, borderBottom: '1px solid #f0f0f0' }}>
        <Space size="middle" wrap>
          <DateRangeFilter value={dateValue} onChange={setDateValue} />
          <DepartmentFilter
            value={departmentId}
            onChange={setDepartmentId}
            sceneCode="02"
            placeholder="选择医技科室（可选）"
          />
          <Select
            mode="multiple"
            placeholder="选择检查类型（可选）"
            value={examTypes}
            onChange={setExamTypes}
            style={{ width: 200 }}
            allowClear
            options={examTypeOptions}
          />
          <Button
            type="primary"
            icon={<SearchOutlined />}
            onClick={handleQuery}
            loading={loading}
            disabled={!canQuery}
          >
            查询
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={handleReset}
          >
            重置
          </Button>
        </Space>
      </div>

      {/* 汇总统计 */}
      {data && data.length > 0 && (
        <div style={{ padding: 16, backgroundColor: '#fafafa', borderBottom: '1px solid #f0f0f0' }}>
          <Row gutter={16}>
            <Col span={6}>
              <Statistic title="总预约量" value={totalAppointments} />
            </Col>
            <Col span={6}>
              <Statistic title="检查类型数" value={examTypeCount} />
            </Col>
            <Col span={6}>
              <Statistic title="记录数" value={data.length} />
            </Col>
            <Col span={6}>
              <Statistic 
                title="平均预约量" 
                value={data.length > 0 ? totalAppointments / data.length : 0} 
                precision={1}
              />
            </Col>
          </Row>
        </div>
      )}

      {/* 数据表格 */}
      <ReportTable<MedicalTechAppointmentRow>
        columns={columns}
        data={data || []}
        loading={loading}
        scroll={{ x: 500 }}
        pagination={{ 
          showSizeChanger: true, 
          showQuickJumper: true,
          showTotal: (total) => `共 ${total} 条记录`
        }}
      />

      {/* 错误提示 */}
      {error && (
        <Alert 
          message="查询失败" 
          description={error.message} 
          type="error" 
          style={{ margin: 16 }} 
          showIcon
        />
      )}
    </Card>
  );
};

export default MedicalTechAppointmentReport;