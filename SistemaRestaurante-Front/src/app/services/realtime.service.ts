import { Injectable, effect, inject, signal } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserRole } from '../models/auth.model';
import { AuthService } from './auth.service';

export type RealtimeStatus = 'off' | 'connecting' | 'online' | 'reconnecting';

/** Algo cambió en una cuenta (platos nuevos, estados, mesa...). */
export interface OrderChangedEvent {
  orderId: number;
}

/** Cocina dejó platos listos para servir. */
export interface ItemsReadyEvent {
  orderId: number;
  tableNumber: number;
  waiterId: number;
  items: string[];
}

/** Roles con pantallas que escuchan los avisos (los mismos que admite el hub). */
const LISTENING_ROLES: UserRole[] = ['Admin', 'Manager', 'Staff', 'Kitchen'];

/** Esperas entre reintentos: rápido al principio y luego cada 15 s, sin rendirse nunca. */
const RETRY_DELAYS_MS = [0, 2_000, 5_000, 10_000];
const MAX_RETRY_DELAY_MS = 15_000;

export function retryDelay(attempt: number): number {
  return RETRY_DELAYS_MS[attempt] ?? MAX_RETRY_DELAY_MS;
}

/**
 * Conexión en tiempo real con la API (SignalR). Se abre sola al iniciar sesión con un rol del
 * restaurante y se cierra al salir. Las pantallas se suscriben a los avisos y, por si se pierde
 * alguno mientras no hay conexión, recargan todo cuando llega resync$.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);

  private connection: HubConnection | null = null;
  private retryTimer: ReturnType<typeof setTimeout> | null = null;
  private attempt = 0;
  private loading = false;
  private disconnected: string = 'Disconnected';

  readonly status = signal<RealtimeStatus>('off');
  readonly orderChanged$ = new Subject<OrderChangedEvent>();
  readonly itemsReady$ = new Subject<ItemsReadyEvent>();
  /** Se volvió a conectar: puede que se haya perdido algún aviso, hay que recargar. */
  readonly resync$ = new Subject<void>();

  constructor() {
    effect(() => {
      if (this.wanted()) this.start();
      else this.stop();
    });
  }

  private start(): void {
    if (this.connection || this.loading) return;
    // La librería se descarga solo al entrar con sesión: la pantalla de acceso no la necesita.
    this.loading = true;
    import('@microsoft/signalr').then(
      (signalR) => {
        this.loading = false;
        if (this.wanted()) this.build(signalR);
      },
      () => {
        this.loading = false;
        this.status.set('reconnecting');
        if (this.retryTimer) clearTimeout(this.retryTimer);
        this.retryTimer = setTimeout(() => {
          this.retryTimer = null;
          if (this.wanted()) this.start();
        }, MAX_RETRY_DELAY_MS);
      },
    );
  }

  private wanted(): boolean {
    const user = this.auth.user();
    return !!user && LISTENING_ROLES.includes(user.role);
  }

  private build(signalR: typeof import('@microsoft/signalr')): void {
    if (this.connection) return;
    this.disconnected = signalR.HubConnectionState.Disconnected;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/orders`, {
        // El token viaja en ?access_token= (los WebSocket del navegador no admiten cabeceras).
        accessTokenFactory: () => this.auth.token() ?? '',
        // La API no usa cookies; con credenciales el CORS de desarrollo lo rechazaría.
        withCredentials: false,
      })
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: (ctx) => retryDelay(ctx.previousRetryCount) })
      .configureLogging(environment.production ? signalR.LogLevel.None : signalR.LogLevel.Warning)
      .build();

    connection.on('OrderChanged', (e: OrderChangedEvent) => this.orderChanged$.next(e));
    connection.on('ItemsReady', (e: ItemsReadyEvent) => this.itemsReady$.next(e));
    connection.onreconnecting(() => this.status.set('reconnecting'));
    connection.onreconnected(() => {
      this.status.set('online');
      this.resync$.next();
    });
    // Se cerró del todo (p. ej. caducó el token o la API estuvo caída mucho rato): se reabre
    // mientras siga habiendo sesión.
    connection.onclose(() => {
      if (this.connection !== connection) return;
      this.status.set('reconnecting');
      this.scheduleConnect();
    });

    this.connection = connection;
    this.attempt = 0;
    this.connect();
  }

  private connect(): void {
    const connection = this.connection;
    if (!connection || connection.state !== this.disconnected) return;
    if (!this.auth.token()) return; // la sesión caducó: el efecto llamará a stop()
    if (this.status() === 'off') this.status.set('connecting');
    connection.start().then(
      () => {
        if (this.connection !== connection) return;
        const wasRetrying = this.attempt > 0;
        this.attempt = 0;
        this.status.set('online');
        if (wasRetrying) this.resync$.next();
      },
      () => {
        if (this.connection !== connection) return;
        this.status.set('reconnecting');
        this.scheduleConnect();
      },
    );
  }

  private scheduleConnect(): void {
    if (this.retryTimer) clearTimeout(this.retryTimer);
    const delay = retryDelay(++this.attempt);
    this.retryTimer = setTimeout(() => {
      this.retryTimer = null;
      this.connect();
    }, delay);
  }

  private stop(): void {
    if (this.retryTimer) {
      clearTimeout(this.retryTimer);
      this.retryTimer = null;
    }
    const connection = this.connection;
    this.connection = null;
    this.status.set('off');
    connection?.stop().catch(() => undefined);
  }
}
