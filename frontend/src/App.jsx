import { useState } from "react";
import TransactionFeed from "./components/TransactionFeed";
import MetricsChart from "./components/MetricsChart";
import SubmitTransaction from "./components/SubmitTransaction";
import useSignalR from "./hooks/useSignalR";
import "./App.css";

export default function App() {
  const [transactions, setTransactions] = useState([]);

  // Live updates from SignalR
  useSignalR("http://localhost:5000/hubs/dashboard", {
    TransactionCreated: (tx) => {
      setTransactions((prev) => [tx, ...prev].slice(0, 100));
    },
    TransactionUpdated: (update) => {
      setTransactions((prev) =>
        prev.map((tx) =>
          tx.id === update.id ? { ...tx, status: update.status } : tx
        )
      );
    },
  });

  return (
    <div className="app">
      <header className="header">
        <h1>FinFlow <span>Dashboard</span></h1>
        <p>Real-time payment event pipeline</p>
      </header>

      <main className="grid">
        <section className="card submit-card">
          <h2>Submit Payment</h2>
          <SubmitTransaction onSubmitted={(tx) =>
            setTransactions((prev) => [tx, ...prev].slice(0, 100))
          } />
        </section>

        <section className="card chart-card">
          <h2>Transaction Volume</h2>
          <MetricsChart transactions={transactions} />
        </section>

        <section className="card feed-card">
          <h2>Live Transaction Feed <span className="badge">{transactions.length}</span></h2>
          <TransactionFeed transactions={transactions} />
        </section>
      </main>
    </div>
  );
}
