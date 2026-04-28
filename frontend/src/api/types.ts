export interface CustomerSearchItem {
  id: string;
  name: string;
  currentBalance: number;
}

export interface CustomerSummaryResponse {
  id: string;
  name: string;
  currentBalance: number;
}

export interface LedgerEntryResponse {
  id: string;
  amount: number;
  notes: string | null;
  createdUtc: string;
}

export interface CustomerLedgerResponse {
  customer: CustomerSummaryResponse;
  orders: LedgerEntryResponse[];
  payments: LedgerEntryResponse[];
}

export interface CreatedItemResponse {
  id: string;
  entryType: string;
  customerId: string;
  amount: number;
  notes: string | null;
  createdUtc: string;
}

export interface QuickEntryRequest {
  entryType: "Order" | "Payment";
  customerId?: string;
  customerName?: string;
  amount: number;
  note?: string;
  autoCreateCustomerIfMissing: boolean;
}

export interface QuickEntryResponse {
  createdItem: CreatedItemResponse | null;
  customer: CustomerSummaryResponse | null;
  resultingBalance: number | null;
  requiresCustomerConfirmation: boolean;
  requiresSettlementConfirmation: boolean;
  message: string | null;
}

export interface SettlementResponse {
  customersSoftDeleted: number;
  ordersSoftDeleted: number;
  paymentsSoftDeleted: number;
  totalRowsSoftDeleted: number;
}

export interface DashboardSummaryResponse {
  activeCustomerCount: number;
  activeOrderTotal: number;
  activePaymentTotal: number;
  outstandingBalance: number;
}
