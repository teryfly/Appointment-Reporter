import React, { useState, useMemo } from 'react';
import { Card, message, Alert, Space, Button, Statistic, Row, Col } from 'antd';
import { SearchOutlined, ReloadOutlined } from '@ant-design/icons';
import DateRangeFilter from '../../components/Filters/DateRangeFilter';
import DepartmentFilter from '../../components/Filters/DepartmentFilter';
import ReportTable from '../../components/Tables/ReportTable';
import ExportButton from '../../components/Tables/ExportButton';
import { useReportData } from '../../hooks/useReportData';
import type { OutpatientAppointmentRow } from '../../types/reportTypes';
import { columns } from './columns/outpatientColumns';
import { exportOutpatientReport } from '../../utils/exportUtils';
import dayjs, { Dayjs } from 'dayjs';

const defaultDate = { 
  type: 'day' as const, 
  startDate: dayjs().subtract(7, 'day'), 
  endDate: dayjs() 
};

const OutpatientAppointmentReport: React.FC = () => {
  const [dateValue, setDateValue] = useState<{ 
    type: 'day' | 'month' | 'year'; 
    startDate: Dayjs | null; 
    endDate: Dayjs | null; 
  }>(defaultDate);
  const [departmentId, setDepartmentId] = useState<string | undefined>();
  const { data, loading, error, fetchData } = useReportData<OutpatientAppointmentRow>({
    type: 'outpatient',
  });

  // 计算汇总数据和表格数据（包含汇总行）
  const { tableData, summaryData } = useMemo(() => {
    if (!data || data.length === 0) {
      return { tableData: [], summaryData: null };
    }

    const totalSlots = data.reduce((sum, item) => sum + (item.slotCount || 0), 0);
    const totalAppointments = data.reduce((sum, item) => sum + (item.appointmentCount || 0), 0);

    const summaryRow: OutpatientAppointmentRow = {
      id: 'summary',
      date: '汇总',
      orgId: '',
      orgName: '',
      doctorId: '',
      doctorName: '',
      personnelCount: 0,
      slotCount: totalSlots,
      appointmentCount: totalAppointments,
      totalCount: 0,
    };

    return {
      tableData: [...data, summaryRow],
      summaryData: {
        totalSlots,
        totalAppointments,
        appointmentRate: totalSlots > 0 ? (totalAppointments / totalSlots * 100) : 0,
        recordCount: data.length,
      }
    };
  }, [data]);

  const handleQuery = () => {
    if (!dateValue.startDate || !dateValue.endDate) {
      message.warning('请选择开始日期和结束日期');
      return;
    }

    // Normalize dates based on type
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
    exportOutpatientReport(data);
    message.success('导出成功');
  };

  const handleReset = () => {
    setDateValue(defaultDate);
    setDepartmentId(undefined);
  };

  const canQuery = dateValue.startDate != null && dateValue.endDate != null;

  // 自定义行样式
  const rowClassName = (record: OutpatientAppointmentRow) => {
    return record.id === 'summary' ? 'summary-row' : '';
  };

  return (
    <Card
      title="门诊预约统计"
      extra={
        <Space>
          <ExportButton onExport={handleExport} disabled={!data || data.length === 0} />
        </Space>
      }
      styles={{ body: { padding: 0 } }}
    >
      <style>{`
        .summary-row {
          background-color: #f5f5f5 !important;
          font-weight: bold;
        }
        .summary-row td {
          border-top: 2px solid #d9d9d9 !important;
        }
      `}</style>

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
      {summaryData && (
        <div style={{ padding: 16, backgroundColor: '#fafafa', borderBottom: '1px solid #f0f0f0' }}>
          <Row gutter={16}>
            <Col span={6}>
              <Statistic title="总放号量" value={summaryData.totalSlots} />
            </Col>
            <Col span={6}>
              <Statistic title="总预约量" value={summaryData.totalAppointments} />
            </Col>
            <Col span={6}>
              <Statistic 
                title="预约率" 
                value={summaryData.appointmentRate} 
                precision={2}
                suffix="%" 
              />
            </Col>
            <Col span={6}>
              <Statistic title="记录数" value={summaryData.recordCount} />
            </Col>
          </Row>
        </div>
      )}

      {/* 数据表格 */}
      <ReportTable<OutpatientAppointmentRow>
        columns={columns}
        data={tableData}
        loading={loading}
        scroll={{ x: 500 }}
        pagination={false}
        rowClassName={rowClassName}
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

export default OutpatientAppointmentReport;