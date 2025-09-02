import { Table, Empty } from 'antd';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';

interface ReportTableProps<T> {
  columns: ColumnsType<T>;
  data: T[];
  loading?: boolean;
  scroll?: { x?: number; y?: number };
  pagination?: false | TablePaginationConfig;
  rowClassName?: (record: T, index: number) => string;
}

function ReportTable<T extends Record<string, any>>({
  columns,
  data,
  loading = false,
  scroll = { x: 800 },
  pagination = false,
  rowClassName,
}: ReportTableProps<T>) {
  return (
    <Table<T>
      columns={columns}
      dataSource={data}
      loading={loading}
      scroll={scroll}
      pagination={pagination}
      locale={{
        emptyText: <Empty description="暂无数据" />
      }}
      size="small"
      bordered
      rowKey={(record) => record.id || Math.random().toString()}
      rowClassName={rowClassName as any}
    />
  );
}

export default ReportTable;