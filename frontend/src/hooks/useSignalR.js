import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";

export default function useSignalR(url, handlers) {
  const connectionRef = useRef(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Register all handlers
    Object.entries(handlers).forEach(([event, handler]) => {
      connection.on(event, handler);
    });

    connection.start().catch((err) =>
      console.error("SignalR connection failed:", err)
    );

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [url]); // eslint-disable-line

  return connectionRef;
}
