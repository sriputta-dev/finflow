import { useState } from "react";
import axios from "axios";

const API = "http://localhost:5000/api";

const inputStyle = {
  width: "100%",
  padding: "8px 12px",
  border: "1px solid #E2E8F0",
  borderRadius: 6,
  fontSize: 13,
  color: "#1E293B",
  outline: "none",
  boxSizing: "border-box",
};

export default function SubmitTransaction({ onSubmitted }) {
  const [form, setForm] = useState({
    amount: "",
    currency: "USD",
    senderId: "",
    receiverId: "",
    description: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const set = (field) => (e) =>
    setForm((prev) => ({ ...prev, [field]: e.target.value }));

  const submit = async () => {
    if (!form.amount || !form.senderId || !form.receiverId) {
      setError("Amount, Sender ID and Receiver ID are required.");
      return;
    }
    setError(null);
    setLoading(true);
    try {
      const { data } = await axios.post(`${API}/transactions`, {
        ...form,
        amount: parseFloat(form.amount),
      });
      onSubmitted(data);
      setForm({ amount: "", currency: "USD", senderId: "", receiverId: "", description: "" });
    } catch (err) {
      setError(err.response?.data?.error || "Request failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
        <div>
          <label style={{ fontSize: 11, fontWeight: 600, color: "#64748B" }}>Sender ID</label>
          <input style={inputStyle} value={form.senderId} onChange={set("senderId")} placeholder="acc-001" />
        </div>
        <div>
          <label style={{ fontSize: 11, fontWeight: 600, color: "#64748B" }}>Receiver ID</label>
          <input style={inputStyle} value={form.receiverId} onChange={set("receiverId")} placeholder="acc-002" />
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: 8 }}>
        <div>
          <label style={{ fontSize: 11, fontWeight: 600, color: "#64748B" }}>Amount</label>
          <input style={inputStyle} type="number" min="0" value={form.amount} onChange={set("amount")} placeholder="1500.00" />
        </div>
        <div>
          <label style={{ fontSize: 11, fontWeight: 600, color: "#64748B" }}>Currency</label>
          <select style={inputStyle} value={form.currency} onChange={set("currency")}>
            <option>USD</option><option>EUR</option><option>GBP</option>
          </select>
        </div>
      </div>

      <div>
        <label style={{ fontSize: 11, fontWeight: 600, color: "#64748B" }}>Description</label>
        <input style={inputStyle} value={form.description} onChange={set("description")} placeholder="Invoice payment" />
      </div>

      {error && <div style={{ color: "#EF4444", fontSize: 12 }}>{error}</div>}

      <button
        onClick={submit}
        disabled={loading}
        style={{
          background: loading ? "#94A3B8" : "#1F5C9E",
          color: "#fff",
          border: "none",
          borderRadius: 6,
          padding: "10px 0",
          fontWeight: 600,
          fontSize: 14,
          cursor: loading ? "not-allowed" : "pointer",
          transition: "background 0.2s",
        }}
      >
        {loading ? "Submitting..." : "Submit Payment →"}
      </button>
    </div>
  );
}
