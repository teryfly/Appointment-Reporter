import React from 'react';
import { DatePicker, Radio, Space } from 'antd';
import type { RadioChangeEvent } from 'antd';
import type { Dayjs } from 'dayjs';

const { RangePicker } = DatePicker;

export type DateType = 'day' | 'month' | 'year';

interface DateRangeFilterProps {
  value: {
    type: DateType;
    startDate: Dayjs | null;
    endDate: Dayjs | null;
  };
  onChange: (value: { type: DateType; startDate: Dayjs | null; endDate: Dayjs | null }) => void;
}

const DateRangeFilter: React.FC<DateRangeFilterProps> = ({ value, onChange }) => {
  const handleTypeChange = (e: RadioChangeEvent) => {
    const newType = e.target.value as DateType;
    // 当切换类型时，重置日期
    onChange({ type: newType, startDate: null, endDate: null });
  };

  const handleDateChange = (dates: [Dayjs | null, Dayjs | null] | null) => {
    if (dates) {
      onChange({
        ...value,
        startDate: dates[0],
        endDate: dates[1],
      });
    } else {
      onChange({
        ...value,
        startDate: null,
        endDate: null,
      });
    }
  };

  const getPickerType = () => {
    switch (value.type) {
      case 'day':
        return 'date';
      case 'month':
        return 'month';
      case 'year':
        return 'year';
      default:
        return 'date';
    }
  };

  const getPlaceholder = (): [string, string] => {
    switch (value.type) {
      case 'day':
        return ['开始日期', '结束日期'];
      case 'month':
        return ['开始月份', '结束月份'];
      case 'year':
        return ['开始年份', '结束年份'];
      default:
        return ['开始日期', '结束日期'];
    }
  };

  return (
    <Space direction="vertical" size="small">
      <Radio.Group value={value.type} onChange={handleTypeChange} size="small">
        <Radio.Button value="day">按日</Radio.Button>
        <Radio.Button value="month">按月</Radio.Button>
        <Radio.Button value="year">按年</Radio.Button>
      </Radio.Group>
      <RangePicker
        picker={getPickerType() as any}
        value={[value.startDate, value.endDate] as any}
        onChange={handleDateChange as any}
        placeholder={getPlaceholder()}
        style={{ width: 280 }}
      />
    </Space>
  );
};

export default DateRangeFilter;