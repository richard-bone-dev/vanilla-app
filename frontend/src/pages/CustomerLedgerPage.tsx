import { useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, BookOpen } from "lucide-react";
import styles from "./PlaceholderPage.module.css";

export function CustomerLedgerPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <button
          className={styles.backBtn}
          onClick={() => navigate("/customers")}
          aria-label="Back"
        >
          <ArrowLeft size={20} />
        </button>
        <BookOpen size={22} className={styles.icon} />
        <h1 className={styles.title}>Ledger</h1>
      </header>
      <div className={styles.placeholder}>
        <BookOpen size={48} className={styles.placeholderIcon} />
        <p>Ledger for customer {id}</p>
        <p className={styles.sub}>Detailed view coming soon</p>
      </div>
    </div>
  );
}
