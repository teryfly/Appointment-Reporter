import React, { ReactNode } from 'react';
import { Layout, Menu } from 'antd';
import { Link, Outlet, useLocation } from 'react-router-dom';

const { Header, Sider, Content } = Layout;

const menuItems = [
  {
    key: 'outpatient-group',
    label: '门诊预约统计',
    children: [
      { key: '/outpatient', label: <Link to="/outpatient">门诊预约量统计</Link> },
      { key: '/timeslot', label: <Link to="/timeslot">挂号预约时段分布统计</Link> },
      { key: '/doctorrate', label: <Link to="/doctorrate">科室医生检查预约率分析</Link> },
    ],
  },
  {
    key: 'medtech-group',
    label: '医技预约统计',
    children: [
      { key: '/medtech', label: <Link to="/medtech">医技预约量统计</Link> },
      { key: '/medtechsource', label: <Link to="/medtechsource">医技预约来源统计</Link> },
      { key: '/medexamdetail', label: <Link to="/medexamdetail">医技检查项目明细统计</Link> },
    ],
  },
];

type AppLayoutProps = {
  children?: ReactNode;
};

const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const location = useLocation();

  // Ensure correct open submenu based on current route
  const selectedKey = location.pathname;
  const openKeys = menuItems
    .filter(group =>
      (group as any).children?.some((child: any) => child.key === selectedKey)
    )
    .map(group => (group as any).key);

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center' }}>
        <div style={{ color: 'white', fontSize: '18px', fontWeight: 'bold' }}>
          预约统计报表
        </div>
      </Header>
      <Layout>
        <Sider width={220} style={{ background: '#fff' }}>
          <Menu
            mode="inline"
            selectedKeys={[selectedKey]}
            defaultOpenKeys={openKeys}
            style={{ height: '100%', borderRight: 0 }}
            items={menuItems as any}
          />
        </Sider>
        <Layout style={{ padding: '0 24px 24px' }}>
          <Content style={{ padding: 24, margin: 0, minHeight: 280, background: '#fff' }}>
            {children || <Outlet />}
          </Content>
        </Layout>
      </Layout>
    </Layout>
  );
};

export default AppLayout;