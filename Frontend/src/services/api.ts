import axios, { AxiosInstance, AxiosResponse } from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

// DTOs gebaseerd op de werkelijke backend
interface VehicleDto {
  id: number;
  externalId: string;
  zoneId: number;
  zoneName: string;
  latitude: number;
  longitude: number;
  batteryLevel: number;
  lastUpdated: string;
}

interface ZoneDto {
  id: number;
  name: string;
  countryCode: string;
  geoJsonBoundary: string;
}

interface SwapperDto {
  id: number;
  name: string;
  email: string;
  phone?: string;
}

interface RouteStopDto {
  id: number;
  routeId: number;
  vehicleId: number;
  vehicle: VehicleDto;
  order: number;
  estimatedArrivalOffset: string; // TimeSpan als string
  estimatedDurationAtStop: string; // TimeSpan als string
  status: number; // RouteStopStatus enum
  actualArrivalTime?: string;
  actualDepartureTime?: string;
}

interface RouteDto {
  id: number;
  swapperId: number;
  swapperName: string;
  zoneId: number;
  zoneName: string;
  date: string;
  targetDurationMinutes: number;
  status: number; // RouteStatus enum
  createdAt: string;
  confirmedAt?: string;
  stops: RouteStopDto[];
}

interface RouteGenerationRequest {
  zoneId: number;
  swapperId: number;
  batteryThreshold?: number;
  targetDurationMinutes?: number;
}

interface RouteGenerationResponse {
  success: boolean;
  message?: string;
  route?: RouteDto;
  errors?: string[];
}

// Auth DTOs
interface LoginRequest {
  username: string;
  password: string;
}

interface LoginResponse {
  token: string;
  user: UserDto;
}

interface UserDto {
  id: number;
  username: string;
  email: string;
  role: number; // UserRole enum value (0=Admin, 1=FleetManager, 2=BatterySwapper)
  roleName: string; // UserRole as string
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  assignedZoneId?: number;
  assignedZoneName?: string;
  isTemporaryPassword: boolean; // New field for temporary password status
  hasCompletedFirstLogin: boolean; // New field to track first login completion
}

interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  role: number; // UserRole enum value (0=Admin, 1=FleetManager, 2=BatterySwapper)
  assignedZoneId?: number;
}

class ApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: __DEV__ 
        ? 'http://localhost:5033/api' // Development - ASP.NET Core backend port
        : 'https://your-production-api.com/api', // Production URL
      timeout: 15000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor
    this.api.interceptors.request.use(
      async (config) => {
        console.log(`API Request: ${config.method?.toUpperCase()} ${config.url}`);
        // Add authorization header if token exists
        try {
          const token = await AsyncStorage.getItem('authToken');
          console.log('Auth token found:', token ? 'YES' : 'NO');
          if (token) {
            config.headers.Authorization = `Bearer ${token}`;
            console.log('Authorization header set with token');
          } else {
            console.log('No auth token found in storage');
          }
        } catch (error) {
          console.error('Error getting auth token:', error);
        }
        console.log('Final request config:', {
          url: config.url,
          method: config.method,
          headers: config.headers,
          data: config.data
        });
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor
    this.api.interceptors.response.use(
      (response) => {
        console.log(`API Response: ${response.status} ${response.config.url}`);
        return response;
      },
      async (error) => {
        console.error('API Error:', {
          url: error.config?.url,
          status: error.response?.status,
          data: error.response?.data,
          message: error.message
        });

        // Handle 401 Unauthorized errors - token might be expired or invalid
        if (error.response?.status === 401) {
          console.log('401 Unauthorized - removing stored auth data');
          try {
            await AsyncStorage.removeItem('authToken');
            await AsyncStorage.removeItem('userData');
          } catch (storageError) {
            console.error('Error clearing auth storage:', storageError);
          }
        }
        
        return Promise.reject(error);
      }
    );
  }

  // Route endpoints (aangepast aan werkelijke backend)
  async suggestRoute(request: RouteGenerationRequest): Promise<RouteGenerationResponse> {
    const response: AxiosResponse<RouteGenerationResponse> = await this.api.post('/routes/suggest', request);
    return response.data;
  }

  async getRouteSuggestions(zoneId: number): Promise<RouteDto[]> {
    const response: AxiosResponse<RouteDto[]> = await this.api.get(`/routes/suggestions?zoneId=${zoneId}`);
    return response.data;
  }

  async confirmRoute(routeId: number): Promise<RouteDto> {
    const response: AxiosResponse<RouteDto> = await this.api.post(`/routes/${routeId}/confirm`);
    return response.data;
  }

  async getTodaysRoute(swapperId: number): Promise<RouteDto> {
    const response: AxiosResponse<RouteDto> = await this.api.get(`/routes/today?swapperId=${swapperId}`);
    return response.data;
  }

  async getRoute(id: number): Promise<RouteDto> {
    const response: AxiosResponse<RouteDto> = await this.api.get(`/routes/${id}`);
    return response.data;
  }

  // Additional route methods
  async getRoutes(): Promise<RouteDto[]> {
    const response: AxiosResponse<RouteDto[]> = await this.api.get('/routes');
    return response.data;
  }

  async createRoute(request: RouteGenerationRequest): Promise<RouteDto> {
    const response: AxiosResponse<RouteDto> = await this.api.post('/routes', request);
    return response.data;
  }

  async updateRoute(id: number, route: Partial<RouteDto>): Promise<RouteDto> {
    const response: AxiosResponse<RouteDto> = await this.api.put(`/routes/${id}`, route);
    return response.data;
  }

  async deleteRoute(id: number): Promise<void> {
    await this.api.delete(`/routes/${id}`);
  }

  async optimizeRoute(id: number): Promise<RouteDto> {
    const response: AxiosResponse<RouteDto> = await this.api.post(`/routes/${id}/optimize`);
    return response.data;
  }

  // Vehicle endpoints (aangepast aan werkelijke backend)
  async getLowBatteryVehicles(zoneId: number, batteryThreshold: number = 25): Promise<VehicleDto[]> {
    const response: AxiosResponse<VehicleDto[]> = await this.api.get(`/vehicles/lowbattery?zoneId=${zoneId}&batteryThreshold=${batteryThreshold}`);
    return response.data;
  }

  async getVehiclesByZone(zoneId: number): Promise<VehicleDto[]> {
    const response: AxiosResponse<VehicleDto[]> = await this.api.get(`/vehicles/zone/${zoneId}`);
    return response.data;
  }

  async getVehicle(id: number): Promise<VehicleDto> {
    const response: AxiosResponse<VehicleDto> = await this.api.get(`/vehicles/${id}`);
    return response.data;
  }

  // Additional vehicle methods
  async getVehicles(): Promise<VehicleDto[]> {
    const response: AxiosResponse<VehicleDto[]> = await this.api.get('/vehicles');
    return response.data;
  }

  async createVehicle(vehicle: Partial<VehicleDto>): Promise<VehicleDto> {
    const response: AxiosResponse<VehicleDto> = await this.api.post('/vehicles', vehicle);
    return response.data;
  }

  async updateVehicle(id: number, vehicle: Partial<VehicleDto>): Promise<VehicleDto> {
    const response: AxiosResponse<VehicleDto> = await this.api.put(`/vehicles/${id}`, vehicle);
    return response.data;
  }

  async deleteVehicle(id: number): Promise<void> {
    await this.api.delete(`/vehicles/${id}`);
  }

  // Zone endpoints (aangepast aan werkelijke backend)
  async getZonesByCountry(countryCode: string): Promise<ZoneDto[]> {
    const response: AxiosResponse<ZoneDto[]> = await this.api.get(`/zones/country/${countryCode}`);
    return response.data;
  }

  async getZone(id: number): Promise<ZoneDto> {
    const response: AxiosResponse<ZoneDto> = await this.api.get(`/zones/${id}`);
    return response.data;
  }

  // Additional zone methods
  async getZones(): Promise<ZoneDto[]> {
    const response: AxiosResponse<ZoneDto[]> = await this.api.get('/zones');
    return response.data;
  }

  async createZone(zone: Partial<ZoneDto>): Promise<ZoneDto> {
    const response: AxiosResponse<ZoneDto> = await this.api.post('/zones', zone);
    return response.data;
  }

  async updateZone(id: number, zone: Partial<ZoneDto>): Promise<ZoneDto> {
    const response: AxiosResponse<ZoneDto> = await this.api.put(`/zones/${id}`, zone);
    return response.data;
  }

  async deleteZone(id: number): Promise<void> {
    await this.api.delete(`/zones/${id}`);
  }

  // RouteStops endpoints (aangepast aan werkelijke backend)
  async updateRouteStopStatus(routeId: number, stopId: number, status: number): Promise<RouteStopDto> {
    const response: AxiosResponse<RouteStopDto> = await this.api.put(`/routestops/${routeId}/stops/${stopId}/status`, { status });
    return response.data;
  }

  async completeRouteStop(routeId: number, stopId: number): Promise<RouteStopDto> {
    const response: AxiosResponse<RouteStopDto> = await this.api.post(`/routestops/${routeId}/stops/${stopId}/complete`);
    return response.data;
  }

  // Health check endpoint
  async healthCheck(): Promise<{ status: string; timestamp: string }> {
    const response: AxiosResponse<{ status: string; timestamp: string }> = await this.api.get('/health');
    return response.data;
  }

  // Authentication endpoints
  async login(loginData: LoginRequest): Promise<LoginResponse> {
    console.log('API Request: POST /auth/login');
    console.log('Login data being sent:', loginData);
    const response: AxiosResponse<LoginResponse> = await this.api.post('/auth/login', loginData);
    return response.data;
  }

  async getCurrentUser(): Promise<UserDto> {
    const response: AxiosResponse<UserDto> = await this.api.get('/auth/me');
    return response.data;
  }

  async changePassword(oldPassword: string, newPassword: string): Promise<void> {
    await this.api.post('/auth/users/change-password', { 
      currentPassword: oldPassword, 
      newPassword: newPassword 
    });
  }

  async createUser(userData: CreateUserRequest): Promise<UserDto> {
    console.log('createUser - sending request to backend:', userData);
    try {
      const response: AxiosResponse<UserDto> = await this.api.post('/auth/users', userData);
      console.log('createUser - success response:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('createUser - error:', error);
      console.error('createUser - error response:', error.response);
      console.error('createUser - error status:', error.response?.status);
      console.error('createUser - error data:', error.response?.data);
      throw error;
    }
  }

  async getUsers(): Promise<UserDto[]> {
    const response: AxiosResponse<UserDto[]> = await this.api.get('/auth/users');
    return response.data;
  }

  async updateUser(id: number, userData: Partial<CreateUserRequest>): Promise<UserDto> {
    const response: AxiosResponse<UserDto> = await this.api.put(`/auth/users/${id}`, userData);
    return response.data;
  }

  async deleteUser(id: number): Promise<void> {
    await this.api.delete(`/auth/users/${id}`);
  }

  async resetUserPassword(id: number): Promise<{ message: string; temporaryPassword: string }> {
    const response: AxiosResponse<{ message: string; temporaryPassword: string }> = await this.api.post(`/auth/users/${id}/reset-password`);
    return response.data;
  }

  // Password reset functionality
  async forgotPassword(email: string): Promise<{ message: string }> {
    const response: AxiosResponse<{ message: string }> = await this.api.post('/auth/forgot-password', { email });
    return response.data;
  }

  async resetPassword(token: string, newPassword: string): Promise<{ message: string }> {
    const response: AxiosResponse<{ message: string }> = await this.api.post('/auth/reset-password', { 
      token, 
      newPassword 
    });
    return response.data;
  }

  // Statistics endpoints
  async getActiveUsersCount(): Promise<number> {
    const response: AxiosResponse<{ count: number }> = await this.api.get('/auth/users/count');
    return response.data.count;
  }

  async getZonesCount(): Promise<number> {
    const response: AxiosResponse<{ count: number }> = await this.api.get('/zones/count');
    return response.data.count;
  }
}

export const apiService = new ApiService();
export type { 
  VehicleDto, 
  ZoneDto, 
  SwapperDto, 
  RouteDto, 
  RouteStopDto, 
  RouteGenerationRequest, 
  RouteGenerationResponse,
  LoginRequest,
  LoginResponse,
  UserDto,
  CreateUserRequest
};
export default ApiService;
