import React, { useState } from 'react';
import { Select, Spin } from 'antd';
import { getDepartments } from '../../services/api/reportApi';
import type { Organization } from '../../types/reportTypes';

interface DepartmentFilterProps {
  value?: string;
  onChange?: (value: string) => void;
  sceneCode?: '01' | '02'; // 01-门诊，02-医技
  placeholder?: string;
}

const DepartmentFilter: React.FC<DepartmentFilterProps> = ({
  value,
  onChange,
  sceneCode = '01',
  placeholder,
}) => {
  const [departments, setDepartments] = useState<Organization[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [hasLoaded, setHasLoaded] = useState(false);

  const defaultPlaceholder = placeholder || `选择${sceneCode === '01' ? '门诊' : '医技'}科室（可选）`;

  const handleOpenChange = async (open: boolean) => {
    // 只在打开下拉框且未加载过数据时才加载
    if (open && !hasLoaded && !loading) {
      setLoading(true);
      setLoadError(null);
      try {
        const list = await getDepartments(sceneCode);
        if (!list || list.length === 0) {
          setLoadError(`暂无${sceneCode === '01' ? '门诊' : '医技'}科室数据`);
          setDepartments([]);
        } else {
          setDepartments(list);
        }
        setHasLoaded(true);
      } catch (e: any) {
        setLoadError(e?.message || '获取科室失败');
        setDepartments([]);
      } finally {
        setLoading(false);
      }
    }
  };

  const handleChange = (selectedValue: string) => {
    onChange?.(selectedValue);
  };

  const handleClear = () => {
    onChange?.(undefined as any);
  };

  return (
    <div style={{ minWidth: 200 }}>
      <Select
        placeholder={defaultPlaceholder}
        value={value}
        onChange={handleChange}
        onClear={handleClear}
        onOpenChange={handleOpenChange}
        style={{ width: 240 }}
        allowClear
        loading={loading}
        showSearch
        filterOption={(input, option) =>
          (option?.children as string)?.toLowerCase().includes(input.toLowerCase())
        }
        notFoundContent={
          loading 
            ? <Spin size="small" /> 
            : loadError 
              ? `加载失败：${loadError}` 
              : hasLoaded 
                ? '暂无数据' 
                : '点击加载科室列表'
        }
      >
        {departments.map((d) => (
          <Select.Option key={d.id} value={d.id} title={`${d.name} (${d.id})`}>
            {d.name}
          </Select.Option>
        ))}
      </Select>
      {loadError && !loading && (
        <div style={{ fontSize: 12, color: '#ff4d4f', marginTop: 4 }}>
          {loadError}
        </div>
      )}
    </div>
  );
};

export default DepartmentFilter;