import React, { useState } from 'react';
import { Card, message, Alert, Space, Button, Statistic, Row, Col } from 'antd';
import { SearchOutlined, ReloadOutlined } from '@ant-design/icons';
import DateRangeFilter from '../../components/Filters/DateRangeFilter';
import DepartmentFilter from '../../components/Filters/DepartmentFilter';
import ReportTable from '../../components/Tables/ReportTable';
import ExportButton from '../../components/Tables/ExportButton';
import { useReportData } from '../../hooks/useReportData';
import type { DoctorAppointmentRateRow } from '../../types/reportTypes';
import { exportDoctorRateReport } from '../../utils/exportUtils';
import dayjs, { Dayjs } from 'dayjs';

const defaultDate = { 
  type: 'day' as const, 
  startDate: dayjs().subtract(7, 'day'), 
  endDate: dayjs() 
};

const columns = [
  {
    title: '日期',
    dataIndex: 'date',
    key: 'date',
    width: 120,
    fixed: 'left' as const,
  },
  {
    title: '科室',
    dataIndex: 'department',
    key: 'department',
    width: 150,
  },
  {
    title: '医生',
    dataIndex: 'doctor',
    key: 'doctor',
    width: 120,
  },
  {
    title: '开单量',
    dataIndex: 'orderCount',
    key: 'orderCount',
    width: 100,
    align: 'right' as const,
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '预约量',
    dataIndex: 'appointmentCount',
    key: 'appointmentCount',
    width: 100,
    align: 'right' as const,
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '预约率',
    dataIndex: 'appointmentRate',
    key: 'appointmentRate',
    width: 100,
    align: 'right' as const,
    render: (value: number) => `${(value * 100).toFixed(2)}%`,
  },
];

const DoctorAppointmentRateReport: React.FC = () => {
  const [dateValue, setDateValue] = useState<{ 
    type: 'day' | 'month' | 'year'; 
    startDate: Dayjs | null; 
    endDate: Dayjs | null; 
  }>(defaultDate);
  const [departmentId, setDepartmentId] = useState<string | undefined>();
  const { data, loading, error, fetchData } = useReportData<DoctorAppointmentRateRow>({
    type: 'doctorrate',
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
    });
  };

  const handleExport = () => {
    if (!data || data.length === 0) {
      message.warning('无可导出数据');
      return;
    }
    exportDoctorRateReport(data);
    message.success('导出成功');
  };

  const handleReset = () => {
    setDateValue(defaultDate);
    setDepartmentId(undefined);
  };

  // 计算汇总数据
  const totalOrders = data.reduce((sum, item) => sum + (item.orderCount || 0), 0);
  const totalAppointments = data.reduce((sum, item) => sum + (item.appointmentCount || 0), 0);
  const overallAppointmentRate = totalOrders > 0 ? (totalAppointments / totalOrders) : 0;
  const doctorCount = new Set(data.map(item => item.doctor)).size;

  const canQuery = dateValue.startDate != null && dateValue.endDate != null;

  return (
    <Card
      title="科室医生检查预约率分析"
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
            sceneCode="01"
            placeholder="选择门诊科室（可选）"
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
              <Statistic title="总开单量" value={totalOrders} />
            </Col>
            <Col span={6}>
              <Statistic title="总预约量" value={totalAppointments} />
            </Col>
            <Col span={6}>
              <Statistic 
                title="整体预约率" 
                value={overallAppointmentRate * 100} 
                precision={2}
                suffix="%" 
              />
            </Col>
            <Col span={6}>
              <Statistic title="医生数" value={doctorCount} />
            </Col>
          </Row>
        </div>
      )}

      {/* 数据表格 */}
      <ReportTable<DoctorAppointmentRateRow>
        columns={columns}
        data={data || []}
        loading={loading}
        scroll={{ x: 700 }}
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

export default DoctorAppointmentRateReport;