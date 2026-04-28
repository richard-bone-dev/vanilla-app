import { useNavigate } from "react-router-dom";
import { ArrowLeft, Users } from "lucide-react";
import styles from "./PlaceholderPage.module.css";

export function CustomersPage() {
  const navigate = useNavigate();

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <button
          className={styles.backBtn}
          onClick={() => navigate("/dashboard")}
          aria-label="Back"
        >
          <ArrowLeft size={20} />
        </button>
        <Users size={22} className={styles.icon} />
        <h1 className={styles.title}>Customers</h1>
      </header>
      <div className={styles.placeholder}>
        <Users size={48} className={styles.placeholderIcon} />
        <p>Customer list coming soon</p>
      </div>
    </div>
  );
}
