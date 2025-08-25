import React, { ReactNode } from 'react';
import { Layout, Menu } from 'antd';
import { Link, Outlet, useLocation } from 'react-router-dom';

const { Header, Sider, Content } = Layout;

const menuItems = [
  { key: '/outpatient', label: <Link to="/outpatient">门诊预约统计</Link> },
  { key: '/medtech', label: <Link to="/medtech">医技预约统计</Link> },
  { key: '/medtechsource', label: <Link to="/medtechsource">医技预约来源</Link> },
  { key: '/medexamdetail', label: <Link to="/medexamdetail">医技检查项目明细</Link> },
  { key: '/timeslot', label: <Link to="/timeslot">挂号预约时段分布</Link> },
  { key: '/doctorrate', label: <Link to="/doctorrate">科室医生预约率</Link> },
];

type AppLayoutProps = {
  children?: ReactNode;
};

const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const location = useLocation();
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center' }}>
        <div style={{ color: 'white', fontSize: '18px', fontWeight: 'bold' }}>
          报表系统
        </div>
      </Header>
      <Layout>
        <Sider width={200} style={{ background: '#fff' }}>
          <Menu
            mode="inline"
            selectedKeys={[location.pathname]}
            style={{ height: '100%', borderRight: 0 }}
            items={menuItems}
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