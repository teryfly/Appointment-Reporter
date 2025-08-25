import React, { useState } from 'react';
import { Card, message, Alert, Space, Button, Statistic, Row, Col } from 'antd';
import { SearchOutlined, ReloadOutlined } from '@ant-design/icons';
import DateRangeFilter from '../../components/Filters/DateRangeFilter';
import DepartmentFilter from '../../components/Filters/DepartmentFilter';
import ReportTable from '../../components/Tables/ReportTable';
import ExportButton from '../../components/Tables/ExportButton';
import { useReportData } from '../../hooks/useReportData';
import type { MedicalTechSourceRow } from '../../types/reportTypes';
import { columns } from './columns/medtechSourceColumns';
import { exportMedTechSourceReport } from '../../utils/exportUtils';
import dayjs, { Dayjs } from 'dayjs';

const defaultDate = { 
  type: 'day' as const, 
  startDate: dayjs().subtract(7, 'day'), 
  endDate: dayjs() 
};

const MedicalTechSourceReport: React.FC = () => {
  const [dateValue, setDateValue] = useState<{ 
    type: 'day' | 'month' | 'year'; 
    startDate: Dayjs | null; 
    endDate: Dayjs | null; 
  }>(defaultDate);
  const [departmentId, setDepartmentId] = useState<string | undefined>();
  const { data, loading, error, fetchData } = useReportData<MedicalTechSourceRow>({
    type: 'medtechsource',
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
    exportMedTechSourceReport(data);
    message.success('导出成功');
  };

  const handleReset = () => {
    setDateValue(defaultDate);
    setDepartmentId(undefined);
  };

  // 计算汇总数据
  const totalOutpatient = data.reduce((sum, item) => sum + (item.outpatientCount || 0), 0);
  const totalInpatient = data.reduce((sum, item) => sum + (item.inpatientCount || 0), 0);
  const totalPhysicalExam = data.reduce((sum, item) => sum + (item.physicalExamCount || 0), 0);
  const grandTotal = data.reduce((sum, item) => sum + (item.total || 0), 0);

  const canQuery = dateValue.startDate != null && dateValue.endDate != null;

  return (
    <Card
      title="医技预约来源"
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
              <Statistic title="门诊预约总量" value={totalOutpatient} />
            </Col>
            <Col span={6}>
              <Statistic title="住院预约总量" value={totalInpatient} />
            </Col>
            <Col span={6}>
              <Statistic title="体检预约总量" value={totalPhysicalExam} />
            </Col>
            <Col span={6}>
              <Statistic title="总计" value={grandTotal} />
            </Col>
          </Row>
        </div>
      )}

      {/* 数据表格 */}
      <ReportTable<MedicalTechSourceRow>
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

export default MedicalTechSourceReport;