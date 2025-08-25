import React, { useState } from 'react';
import { Card, message, Alert, Space, Button, Select, Statistic, Row, Col } from 'antd';
import { SearchOutlined, ReloadOutlined } from '@ant-design/icons';
import DateRangeFilter from '../../components/Filters/DateRangeFilter';
import DepartmentFilter from '../../components/Filters/DepartmentFilter';
import ReportTable from '../../components/Tables/ReportTable';
import ExportButton from '../../components/Tables/ExportButton';
import { useReportData } from '../../hooks/useReportData';
import type { MedicalExamDetailRow } from '../../types/reportTypes';
import { getExamItems } from '../../services/api/reportApi';
import { exportMedExamDetailReport } from '../../utils/exportUtils';
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
    title: '检查项目',
    dataIndex: 'examItem',
    key: 'examItem',
    width: 150,
  },
  {
    title: '门诊预约量',
    dataIndex: 'outpatientCount',
    key: 'outpatientCount',
    width: 120,
    align: 'right' as const,
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '住院预约量',
    dataIndex: 'inpatientCount',
    key: 'inpatientCount',
    width: 120,
    align: 'right' as const,
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '体检预约量',
    dataIndex: 'physicalExamCount',
    key: 'physicalExamCount',
    width: 120,
    align: 'right' as const,
    render: (value: number) => value?.toLocaleString() || 0,
  },
  {
    title: '汇总',
    dataIndex: 'total',
    key: 'total',
    width: 100,
    align: 'right' as const,
    render: (value: number) => value?.toLocaleString() || 0,
  },
];

const MedicalExamDetailReport: React.FC = () => {
  const [dateValue, setDateValue] = useState<{ 
    type: 'day' | 'month' | 'year'; 
    startDate: Dayjs | null; 
    endDate: Dayjs | null; 
  }>(defaultDate);
  const [departmentId, setDepartmentId] = useState<string | undefined>();
  const [examItem, setExamItem] = useState<string | undefined>();
  const [examItems, setExamItems] = useState<Array<{code: string; name: string}>>([]);
  const { data, loading, error, fetchData } = useReportData<MedicalExamDetailRow>({
    type: 'medexamdetail',
  });

  const handleDepartmentChange = async (value: string) => {
    setDepartmentId(value);
    setExamItem(undefined);
    if (value) {
      const items = await getExamItems(value);
      setExamItems(items);
    } else {
      setExamItems([]);
    }
  };

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
      itemCodes: examItem ? [examItem] : undefined,
    });
  };

  const handleExport = () => {
    if (!data || data.length === 0) {
      message.warning('无可导出数据');
      return;
    }
    exportMedExamDetailReport(data);
    message.success('导出成功');
  };

  const handleReset = () => {
    setDateValue(defaultDate);
    setDepartmentId(undefined);
    setExamItem(undefined);
    setExamItems([]);
  };

  // 计算汇总数据
  const totalOutpatient = data.reduce((sum, item) => sum + (item.outpatientCount || 0), 0);
  const totalInpatient = data.reduce((sum, item) => sum + (item.inpatientCount || 0), 0);
  const totalPhysicalExam = data.reduce((sum, item) => sum + (item.physicalExamCount || 0), 0);
  const grandTotal = data.reduce((sum, item) => sum + (item.total || 0), 0);

  const canQuery = dateValue.startDate != null && dateValue.endDate != null;

  return (
    <Card
      title="医技检查项目明细统计"
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
            onChange={handleDepartmentChange}
            sceneCode="02"
            placeholder="选择医技科室（可选）"
          />
          <Select
            placeholder="选择检查项目（可选）"
            value={examItem}
            onChange={setExamItem}
            style={{ width: 200 }}
            allowClear
            disabled={!departmentId}
          >
            {examItems.map((item) => (
              <Select.Option key={item.code} value={item.code}>
                {item.name}
              </Select.Option>
            ))}
          </Select>
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
      <ReportTable<MedicalExamDetailRow>
        columns={columns}
        data={data || []}
        loading={loading}
        scroll={{ x: 800 }}
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

export default MedicalExamDetailReport;