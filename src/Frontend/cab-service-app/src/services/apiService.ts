import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';

class ApiService {
  private api: AxiosInstance;
  private baseURL: string;

  constructor() {
    this.baseURL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7000';
    
    this.api = axios.create({
      baseURL: this.baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors(): void {
    // Request interceptor to add auth token
    this.api.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('accessToken');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor for error handling
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('accessToken');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Passenger API
  public passenger = {
    register: (data: any) => this.api.post('/api/passengers', data),
    getProfile: (id: string) => this.api.get(`/api/passengers/${id}`),
    updateProfile: (id: string, data: any) => this.api.put(`/api/passengers/${id}`, data),
    verify: (id: string, data: any) => this.api.post(`/api/passengers/${id}/verify`, data),
  };

  // Driver API
  public driver = {
    register: (data: any) => this.api.post('/api/drivers', data),
    getProfile: (id: string) => this.api.get(`/api/drivers/${id}`),
    updateProfile: (id: string, data: any) => this.api.put(`/api/drivers/${id}`, data),
    updateStatus: (id: string, status: string) => this.api.patch(`/api/drivers/${id}/status`, { status }),
    updateLocation: (id: string, location: any) => this.api.patch(`/api/drivers/${id}/location`, location),
    getNearbyDrivers: (lat: number, lng: number, radius: number) => 
      this.api.get(`/api/drivers/nearby?latitude=${lat}&longitude=${lng}&radiusKm=${radius}`),
  };

  // Booking API
  public booking = {
    create: (data: any) => this.api.post('/api/bookings', data),
    getById: (id: string) => this.api.get(`/api/bookings/${id}`),
    getByPassenger: (passengerId: string) => this.api.get(`/api/bookings/passenger/${passengerId}`),
    getByDriver: (driverId: string) => this.api.get(`/api/bookings/driver/${driverId}`),
    accept: (id: string, driverId: string) => this.api.post(`/api/bookings/${id}/accept`, { driverId }),
    startTrip: (id: string) => this.api.post(`/api/bookings/${id}/start`),
    completeTrip: (id: string, data: any) => this.api.post(`/api/bookings/${id}/complete`, data),
    cancel: (id: string, reason: string) => this.api.post(`/api/bookings/${id}/cancel`, { reason }),
    rate: (id: string, rating: any) => this.api.post(`/api/bookings/${id}/rate`, rating),
    estimateFare: (data: any) => this.api.post('/api/bookings/estimate-fare', data),
  };

  // Payment API
  public payment = {
    process: (data: any) => this.api.post('/api/payments/process', data),
    getById: (id: string) => this.api.get(`/api/payments/${id}`),
    getByBooking: (bookingId: string) => this.api.get(`/api/payments/booking/${bookingId}`),
    retry: (id: string) => this.api.post(`/api/payments/${id}/retry`),
    refund: (id: string, data: any) => this.api.post(`/api/payments/${id}/refund`, data),
  };

  // Notification API
  public notification = {
    getByUser: (userId: string, page: number = 1, pageSize: number = 20, unreadOnly: boolean = false) =>
      this.api.get(`/api/notifications/user/${userId}?page=${page}&pageSize=${pageSize}&unreadOnly=${unreadOnly}`),
    markAsRead: (id: string) => this.api.patch(`/api/notifications/${id}/read`),
    markAllAsRead: (userId: string) => this.api.patch(`/api/notifications/user/${userId}/read-all`),
    getUnreadCount: (userId: string) => this.api.get(`/api/notifications/user/${userId}/unread-count`),
    delete: (id: string) => this.api.delete(`/api/notifications/${id}`),
  };

  // Generic methods
  public get<T = any>(url: string, config?: AxiosRequestConfig) {
    return this.api.get<T>(url, config);
  }

  public post<T = any>(url: string, data?: any, config?: AxiosRequestConfig) {
    return this.api.post<T>(url, data, config);
  }

  public put<T = any>(url: string, data?: any, config?: AxiosRequestConfig) {
    return this.api.put<T>(url, data, config);
  }

  public patch<T = any>(url: string, data?: any, config?: AxiosRequestConfig) {
    return this.api.patch<T>(url, data, config);
  }

  public delete<T = any>(url: string, config?: AxiosRequestConfig) {
    return this.api.delete<T>(url, config);
  }
}

export const apiService = new ApiService();
export default apiService;
