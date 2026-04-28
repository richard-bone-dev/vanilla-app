import { BrowserRouter, Routes, Route } from "react-router-dom";
import { ToastProvider } from "./hooks/useToast";
import { ToastContainer } from "./components/ToastContainer";
import { FloatingWidget } from "./components/FloatingWidget";
import { QuickEntryPage } from "./pages/QuickEntryPage";
import { DashboardPage } from "./pages/DashboardPage";
import { CustomersPage } from "./pages/CustomersPage";
import { CustomerLedgerPage } from "./pages/CustomerLedgerPage";
import "./index.css";

export function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <ToastContainer />
        <Routes>
          <Route path="/" element={<QuickEntryPage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/customers" element={<CustomersPage />} />
          <Route path="/customers/:id/ledger" element={<CustomerLedgerPage />} />
        </Routes>
        <FloatingWidget />
      </ToastProvider>
    </BrowserRouter>
  );
}
