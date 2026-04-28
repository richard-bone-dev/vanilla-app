import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  LayoutDashboard,
  Users,
  TrendingUp,
  Wallet,
  ArrowLeft,
} from "lucide-react";
import { getDashboardSummary } from "../api/client";
import type { DashboardSummaryResponse } from "../api/types";
import styles from "./DashboardPage.module.css";

export function DashboardPage() {
  const navigate = useNavigate();
  const [summary, setSummary] = useState<DashboardSummaryResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getDashboardSummary()
      .then(setSummary)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <button
          className={styles.backBtn}
          onClick={() => navigate("/")}
          aria-label="Back"
        >
          <ArrowLeft size={20} />
        </button>
        <LayoutDashboard size={22} className={styles.icon} />
        <h1 className={styles.title}>Dashboard</h1>
      </header>

      {loading ? (
        <div className={styles.loading}>Loading...</div>
      ) : summary ? (
        <div className={styles.grid}>
          <div className={styles.card}>
            <div className={styles.cardIcon}>
              <Users size={20} />
            </div>
            <div className={styles.cardLabel}>Active Customers</div>
            <div className={styles.cardValue}>{summary.activeCustomerCount}</div>
          </div>
          <div className={styles.card}>
            <div className={styles.cardIcon}>
              <TrendingUp size={20} />
            </div>
            <div className={styles.cardLabel}>Total Orders</div>
            <div className={styles.cardValue}>
              {"\u00A3"}{summary.activeOrderTotal.toFixed(2)}
            </div>
          </div>
          <div className={styles.card}>
            <div className={styles.cardIcon}>
              <Wallet size={20} />
            </div>
            <div className={styles.cardLabel}>Total Payments</div>
            <div className={styles.cardValue}>
              {"\u00A3"}{summary.activePaymentTotal.toFixed(2)}
            </div>
          </div>
          <div className={styles.cardWide}>
            <div className={styles.cardLabel}>Outstanding Balance</div>
            <div className={styles.cardValueLarge}>
              {"\u00A3"}{summary.outstandingBalance.toFixed(2)}
            </div>
          </div>
        </div>
      ) : (
        <div className={styles.loading}>
          Unable to load summary. Check API connection.
        </div>
      )}

      <div className={styles.nav}>
        <button
          className={styles.navItem}
          onClick={() => navigate("/customers")}
        >
          <Users size={18} />
          <span>Customers</span>
        </button>
      </div>
    </div>
  );
}
