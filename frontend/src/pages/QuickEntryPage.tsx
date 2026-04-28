import { useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { Zap, Settings, StickyNote } from "lucide-react";
import { CustomerSearch } from "../components/CustomerSearch";
import { Modal, ModalActions } from "../components/Modal";
import { quickEntry, settleCustomer } from "../api/client";
import { useToast } from "../hooks/useToast";
import type { CustomerSearchItem, QuickEntryResponse } from "../api/types";
import styles from "./QuickEntryPage.module.css";

type EntryType = "Order" | "Payment";

export function QuickEntryPage() {
  const navigate = useNavigate();
  const { showToast } = useToast();

  const [selectedCustomer, setSelectedCustomer] =
    useState<CustomerSearchItem | null>(null);
  const [customerInput, setCustomerInput] = useState("");
  const [entryType, setEntryType] = useState<EntryType>("Order");
  const [amount, setAmount] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  const [newCustomerModal, setNewCustomerModal] = useState(false);
  const [settlementModal, setSettlementModal] = useState(false);
  const [pendingSettlement, setPendingSettlement] = useState<{
    customerId: string;
    response: QuickEntryResponse;
  } | null>(null);

  const resetForm = useCallback(() => {
    setSelectedCustomer(null);
    setCustomerInput("");
    setAmount("");
    setNote("");
  }, []);

  const handleSave = async (autoCreate = false) => {
    const parsedAmount = parseFloat(amount);
    if (!parsedAmount || parsedAmount <= 0) {
      showToast("Enter a valid amount", "error");
      return;
    }

    if (!selectedCustomer && customerInput.trim().length < 1) {
      showToast("Enter a customer name", "error");
      return;
    }

    setSaving(true);
    try {
      const res = await quickEntry({
        entryType,
        customerId: selectedCustomer?.id,
        customerName: selectedCustomer ? undefined : customerInput.trim(),
        amount: parsedAmount,
        note: note.trim() || undefined,
        autoCreateCustomerIfMissing: autoCreate,
      });

      if (res.requiresCustomerConfirmation) {
        setNewCustomerModal(true);
        return;
      }

      if (res.requiresSettlementConfirmation && res.customer) {
        setPendingSettlement({ customerId: res.customer.id, response: res });
        setSettlementModal(true);
        showToast(`${entryType} saved`, "success");
        resetForm();
        return;
      }

      showToast(`${entryType} saved`, "success");
      resetForm();
    } catch (err) {
      showToast(
        err instanceof Error ? err.message : "Failed to save",
        "error",
      );
    } finally {
      setSaving(false);
    }
  };

  const handleNewCustomerConfirm = async () => {
    setNewCustomerModal(false);
    await handleSave(true);
  };

  const handleSettlementConfirm = async () => {
    if (!pendingSettlement) return;
    try {
      const res = await settleCustomer(pendingSettlement.customerId);
      showToast(
        `Settled: ${res.totalRowsSoftDeleted} records archived`,
        "success",
      );
    } catch {
      showToast("Settlement failed", "error");
    } finally {
      setSettlementModal(false);
      setPendingSettlement(null);
    }
  };

  const handleAmountChange = (val: string) => {
    const cleaned = val.replace(/[^0-9.]/g, "");
    const parts = cleaned.split(".");
    if (parts.length > 2) return;
    if (parts[1] && parts[1].length > 2) return;
    setAmount(cleaned);
  };

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div className={styles.headerLeft}>
          <Zap size={22} className={styles.bolt} />
          <h1 className={styles.title}>Quick Entry</h1>
        </div>
        <button
          className={styles.settingsBtn}
          onClick={() => navigate("/dashboard")}
          aria-label="Dashboard"
        >
          <Settings size={20} />
        </button>
      </header>

      <div className={styles.form}>
        <CustomerSearch
          value={selectedCustomer}
          inputText={customerInput}
          onSelect={setSelectedCustomer}
          onInputChange={setCustomerInput}
        />

        <div className={styles.toggleGroup}>
          <button
            className={`${styles.toggleBtn} ${entryType === "Order" ? styles.toggleActive : ""}`}
            onClick={() => setEntryType("Order")}
          >
            Order
          </button>
          <button
            className={`${styles.toggleBtn} ${entryType === "Payment" ? styles.toggleActive : ""}`}
            onClick={() => setEntryType("Payment")}
          >
            Payment
          </button>
        </div>

        <div className={styles.amountWrapper}>
          <span className={styles.currency}>{"\u00A3"}</span>
          <input
            type="text"
            inputMode="decimal"
            className={styles.amountInput}
            placeholder="0.00"
            value={amount}
            onChange={(e) => handleAmountChange(e.target.value)}
          />
        </div>

        <div className={styles.noteWrapper}>
          <input
            type="text"
            className={styles.noteInput}
            placeholder="Add note (optional)"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            maxLength={200}
          />
          <StickyNote size={16} className={styles.noteIcon} />
        </div>

        <button
          className={styles.saveBtn}
          onClick={() => handleSave(false)}
          disabled={saving}
        >
          {saving ? "Saving..." : "Save"}
        </button>
      </div>

      <Modal open={newCustomerModal} onClose={() => setNewCustomerModal(false)}>
        <h2 className={styles.modalTitle}>Customer not found</h2>
        <p className={styles.modalText}>
          Add <strong>"{customerInput.trim()}"</strong> as a new customer?
        </p>
        <ModalActions>
          <button
            className={styles.btnSecondary}
            onClick={() => setNewCustomerModal(false)}
          >
            Cancel
          </button>
          <button
            className={styles.btnPrimary}
            onClick={handleNewCustomerConfirm}
          >
            Add Customer
          </button>
        </ModalActions>
      </Modal>

      <Modal open={settlementModal} onClose={() => setSettlementModal(false)}>
        <h2 className={styles.modalTitle}>Account settled</h2>
        <p className={styles.modalText}>
          Soft delete this customer and all order/payment history?
        </p>
        <ModalActions>
          <button
            className={styles.btnSecondary}
            onClick={() => {
              setSettlementModal(false);
              setPendingSettlement(null);
            }}
          >
            Keep
          </button>
          <button
            className={styles.btnPrimary}
            onClick={handleSettlementConfirm}
          >
            Settle
          </button>
        </ModalActions>
      </Modal>
    </div>
  );
}
