import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr';
import { authService } from '../api/authService';
import {
  foundationHubUrl,
  notificationHubUrl,
} from '../api/config';
import { SignalREvents } from './signalrEvents';
import type { ConnectionState } from './signalrTypes';

type EventListener = (payload: unknown) => void;

export class SignalRClient {
  constructor(private readonly hubUrl: string) { }

  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private currentState: ConnectionState = 'disconnected';
  private readonly stateListeners = new Set<
    (state: ConnectionState) => void
  >();
  private readonly eventListeners = new Map<
    string,
    Set<EventListener>
  >();

  getState(): ConnectionState {
    return this.currentState;
  }

  subscribe(listener: (state: ConnectionState) => void): () => void {
    listener(this.currentState);
    this.stateListeners.add(listener);

    return () => {
      this.stateListeners.delete(listener);
    };
  }

  on(event: string, listener: EventListener): () => void {
    let listeners = this.eventListeners.get(event);

    if (!listeners) {
      listeners = new Set();
      this.eventListeners.set(event, listeners);
    }

    listeners.add(listener);

    return () => {
      listeners?.delete(listener);
    };
  }

  off(event: string, listener?: EventListener): void {
    if (!listener) {
      this.eventListeners.delete(event);
      return;
    }

    this.eventListeners.get(event)?.delete(listener);
  }

  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    if (this.startPromise) {
      return this.startPromise;
    }

    this.startPromise = this.connectCore().finally(() => {
      this.startPromise = null;
    });

    return this.startPromise;
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
      } catch {
        // Ignore shutdown errors.
      }

      this.connection = null;
    }

    this.setState('disconnected');
  }

  private async connectCore(): Promise<void> {
    try {
      if (!this.connection) {
        this.connection = this.createConnection();
      }

      this.setState('connecting');

      await this.connection.start();

      this.setState('connected');
    } catch (error) {
      this.setState('error');
      this.connection = null;
      throw error;
    }
  }

  private createConnection(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => authService.getValidAccessToken(),
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    connection.onclose((error) => {
      if (error) {
        console.error('SignalR connection closed with error.', error);
      }

      this.connection = null;
      this.setState('disconnected');
    });

    connection.onreconnecting(() => {
      this.setState('reconnecting');
    });

    connection.onreconnected(() => {
      this.setState('connected');
    });

    // Register all SignalR events dynamically.
    for (const eventName of Object.values(SignalREvents)) {
      connection.on(eventName, (payload: unknown) => {
        this.emit(eventName, payload);
      });
    }

    return connection;
  }

  private setState(state: ConnectionState): void {
    this.currentState = state;

    for (const listener of this.stateListeners) {
      listener(state);
    }
  }

  private emit(event: string, payload: unknown): void {
    const listeners = this.eventListeners.get(event);

    if (!listeners) {
      return;
    }

    for (const listener of listeners) {
      listener(payload);
    }
  }
}

export const foundationSignalRClient =
  new SignalRClient(foundationHubUrl);

export const notificationSignalRClient =
  new SignalRClient(notificationHubUrl);
