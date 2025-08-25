import React from 'react';
import { Button } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';

interface ExportButtonProps {
  onExport: () => void;
  disabled?: boolean;
  loading?: boolean;
}

const ExportButton: React.FC<ExportButtonProps> = ({ 
  onExport, 
  disabled = false, 
  loading = false 
}) => {
  return (
    <Button
      type="default"
      icon={<DownloadOutlined />}
      onClick={onExport}
      disabled={disabled}
      loading={loading}
    >
      导出Excel
    </Button>
  );
};

export default ExportButton;