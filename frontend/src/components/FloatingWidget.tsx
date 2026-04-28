import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Zap, X } from "lucide-react";
import styles from "./FloatingWidget.module.css";

export function FloatingWidget() {
  const [expanded, setExpanded] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  if (location.pathname === "/") return null;

  const handleNavigate = (path: string) => {
    setExpanded(false);
    navigate(path);
  };

  return (
    <div className={styles.wrapper}>
      {expanded && (
        <div className={styles.menu}>
          <button
            className={styles.menuItem}
            onClick={() => handleNavigate("/")}
          >
            Quick Entry
          </button>
          <button
            className={styles.menuItem}
            onClick={() => handleNavigate("/dashboard")}
          >
            Dashboard
          </button>
          <button
            className={styles.menuItem}
            onClick={() => handleNavigate("/customers")}
          >
            Customers
          </button>
        </div>
      )}

      <button
        className={`${styles.fab} ${expanded ? styles.fabActive : ""}`}
        onClick={() => setExpanded(!expanded)}
        aria-label={expanded ? "Close menu" : "Quick actions"}
      >
        {expanded ? <X size={22} /> : <Zap size={22} />}
      </button>
    </div>
  );
}
