import type {
  CustomerSearchItem,
  QuickEntryRequest,
  QuickEntryResponse,
  SettlementResponse,
  DashboardSummaryResponse,
  CustomerLedgerResponse,
} from "./types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL as string | undefined;

function apiUrl(path: string): string {
  const base = BASE_URL?.replace(/\/+$/, "") ?? "";
  return `${base}${path}`;
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...init?.headers,
    },
  });

  if (!res.ok) {
    const body = await res.text().catch(() => "");
    throw new ApiError(res.status, body);
  }

  return res.json() as Promise<T>;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: string,
  ) {
    super(`API ${status}: ${body}`);
    this.name = "ApiError";
  }
}

export async function searchCustomers(
  query: string,
): Promise<CustomerSearchItem[]> {
  return request<CustomerSearchItem[]>(
    apiUrl(`/api/customers/search?query=${encodeURIComponent(query)}`),
  );
}

export async function quickEntry(
  req: QuickEntryRequest,
): Promise<QuickEntryResponse> {
  return request<QuickEntryResponse>(apiUrl("/api/quick-entry"), {
    method: "POST",
    body: JSON.stringify(req),
  });
}

export async function settleCustomer(
  customerId: string,
): Promise<SettlementResponse> {
  return request<SettlementResponse>(
    apiUrl(`/api/customers/${customerId}/settle`),
    { method: "POST" },
  );
}

export async function getDashboardSummary(): Promise<DashboardSummaryResponse> {
  return request<DashboardSummaryResponse>(apiUrl("/api/dashboard/summary"));
}

export async function getCustomerLedger(
  customerId: string,
): Promise<CustomerLedgerResponse> {
  return request<CustomerLedgerResponse>(
    apiUrl(`/api/customers/${customerId}/ledger`),
  );
}
