import { useState, useRef, useEffect, useCallback } from "react";
import { Search, User, ChevronDown } from "lucide-react";
import { searchCustomers } from "../api/client";
import type { CustomerSearchItem } from "../api/types";
import styles from "./CustomerSearch.module.css";

interface CustomerSearchProps {
  value: CustomerSearchItem | null;
  inputText: string;
  onSelect: (customer: CustomerSearchItem | null) => void;
  onInputChange: (text: string) => void;
}

export function CustomerSearch({
  value,
  inputText,
  onSelect,
  onInputChange,
}: CustomerSearchProps) {
  const [results, setResults] = useState<CustomerSearchItem[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout>>();

  const doSearch = useCallback(async (query: string) => {
    if (query.trim().length < 3) {
      setResults([]);
      setOpen(false);
      return;
    }
    setLoading(true);
    try {
      const data = await searchCustomers(query.trim());
      setResults(data);
      setOpen(true);
    } catch {
      setResults([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleChange = (text: string) => {
    onInputChange(text);
    if (value) onSelect(null);
    clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => doSearch(text), 300);
  };

  const handleSelect = (customer: CustomerSearchItem) => {
    onSelect(customer);
    onInputChange(customer.name);
    setOpen(false);
  };

  const handleClear = () => {
    onSelect(null);
    onInputChange("");
    setResults([]);
    setOpen(false);
  };

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (
        containerRef.current &&
        !containerRef.current.contains(e.target as Node)
      ) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  return (
    <div className={styles.wrapper} ref={containerRef}>
      <div className={styles.inputWrapper}>
        <Search size={18} className={styles.icon} />
        <input
          type="text"
          className={styles.input}
          placeholder="Search customer..."
          value={inputText}
          onChange={(e) => handleChange(e.target.value)}
          onFocus={() => {
            if (results.length > 0 && !value) setOpen(true);
          }}
        />
        {value ? (
          <button className={styles.clearBtn} onClick={handleClear} aria-label="Clear">
            &times;
          </button>
        ) : (
          <ChevronDown size={16} className={styles.chevron} />
        )}
      </div>

      {open && (
        <div className={styles.dropdown}>
          {loading && <div className={styles.hint}>Searching...</div>}
          {!loading && results.length === 0 && inputText.trim().length >= 3 && (
            <div className={styles.hint}>No customers found</div>
          )}
          {results.map((c) => (
            <button
              key={c.id}
              className={styles.option}
              onClick={() => handleSelect(c)}
            >
              <User size={16} className={styles.optionIcon} />
              <span className={styles.optionName}>{c.name}</span>
              <span className={styles.optionBalance}>
                {"\u00A3"}{c.currentBalance.toFixed(2)}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
