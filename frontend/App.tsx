import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { ConfigProvider } from 'antd';
import zhCN from 'antd/locale/zh_CN';
import AppLayout from './components/Layout/AppLayout';

import OutpatientAppointmentReport from './pages/Reports/OutpatientAppointmentReport';
import MedicalTechAppointmentReport from './pages/Reports/MedicalTechAppointmentReport';
import MedicalTechSourceReport from './pages/Reports/MedicalTechSourceReport';
import MedicalExamDetailReport from './pages/Reports/MedicalExamDetailReport';
import TimeSlotDistributionReport from './pages/Reports/TimeSlotDistributionReport';
import DoctorAppointmentRateReport from './pages/Reports/DoctorAppointmentRateReport';

const App: React.FC = () => {
  return (
    <ConfigProvider locale={zhCN}>
      <Router>
        <AppLayout>
          <Routes>
            <Route path="/" element={<Navigate to="/outpatient" replace />} />
            <Route path="/outpatient" element={<OutpatientAppointmentReport />} />
            <Route path="/medtech" element={<MedicalTechAppointmentReport />} />
            <Route path="/medtechsource" element={<MedicalTechSourceReport />} />
            <Route path="/medexamdetail" element={<MedicalExamDetailReport />} />
            <Route path="/timeslot" element={<TimeSlotDistributionReport />} />
            <Route path="/doctorrate" element={<DoctorAppointmentRateReport />} />
          </Routes>
        </AppLayout>
      </Router>
    </ConfigProvider>
  );
};

export default App;