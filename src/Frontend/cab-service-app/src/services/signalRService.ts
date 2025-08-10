import * as signalR from '@microsoft/signalr';

export class SignalRService {
  private static instance: SignalRService;
  private connection: signalR.HubConnection | null = null;
  private listeners: { [key: string]: ((data: any) => void)[] } = {};

  private constructor() {}

  public static getInstance(): SignalRService {
    if (!SignalRService.instance) {
      SignalRService.instance = new SignalRService();
    }
    return SignalRService.instance;
  }

  public async startConnection(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const apiBaseUrl = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7005';
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/notificationHub`, {
        accessTokenFactory: () => {
          // Get token from localStorage or session
          return localStorage.getItem('accessToken') || '';
        }
      })
      .withAutomaticReconnect()
      .build();

    try {
      await this.connection.start();
      console.log('SignalR Connected');
      this.setupEventHandlers();
    } catch (error) {
      console.error('SignalR Connection Error:', error);
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      console.log('SignalR Disconnected');
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Handle new notifications
    this.connection.on('NewNotification', (notification) => {
      this.emit('NewNotification', notification);
    });

    // Handle notification read
    this.connection.on('NotificationRead', (notificationId) => {
      this.emit('NotificationRead', notificationId);
    });

    // Handle all notifications read
    this.connection.on('AllNotificationsRead', () => {
      this.emit('AllNotificationsRead', null);
    });

    // Handle booking updates
    this.connection.on('BookingUpdate', (booking) => {
      this.emit('BookingUpdate', booking);
    });

    // Handle driver location updates
    this.connection.on('DriverLocationUpdate', (location) => {
      this.emit('DriverLocationUpdate', location);
    });

    // Handle trip updates
    this.connection.on('TripUpdate', (trip) => {
      this.emit('TripUpdate', trip);
    });

    // Handle payment updates
    this.connection.on('PaymentUpdate', (payment) => {
      this.emit('PaymentUpdate', payment);
    });
  }

  public on(event: string, callback: (data: any) => void): void {
    if (!this.listeners[event]) {
      this.listeners[event] = [];
    }
    this.listeners[event].push(callback);
  }

  public off(event: string, callback: (data: any) => void): void {
    if (this.listeners[event]) {
      this.listeners[event] = this.listeners[event].filter(cb => cb !== callback);
    }
  }

  private emit(event: string, data: any): void {
    if (this.listeners[event]) {
      this.listeners[event].forEach(callback => callback(data));
    }
  }

  public async joinGroup(groupName: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinUserGroup', groupName);
    }
  }

  public async leaveGroup(groupName: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveUserGroup', groupName);
    }
  }
}
