import axios, { AxiosInstance, AxiosResponse } from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { FORBIDDEN_MESSAGE } from '../utils/apiError';

// DTOs gebaseerd op de werkelijke backend
interface VehicleDto {
  id: number;
  externalId: string;
  registrationNumber: string;
  vehicleType: string;
  zoneId: number;
  zoneName: string;
  currentParkingZoneId?: number;
  currentParkingZoneName?: string;
  latitude: number;
  longitude: number;
  batteryLevel: number;
  needsBatteryReplacement: boolean;
  isAvailable: boolean;
  lastUpdated: string;
}

interface ZoneDto {
  id: number;
  name: string;
  countryCode: string;
  geoJsonBoundary: string;
  regionId: number;
  regionName: string;
}

interface RegionDto {
  id: number;
  name: string;
  country: string;
  countryCode: string;
  description?: string;
  isActive: boolean;
  latitude?: number;
  longitude?: number;
  zones: ZoneDto[];
}

interface CountryDto {
  countryCode: string;
  countryName: string;
  regions: RegionDto[];
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
  notes?: string; // Opmerking voor de battery swapper (max 500 tekens)
  skipReason?: string; // Reden waarom de swapper de stop oversloeg (max 500 tekens)
}

interface RouteDto {
  id: number;
  assignedSwapperId: number;
  swapperName: string;
  zoneId: number;
  zoneName: string;
  date: string;
  targetDurationMinutes: number;
  status: number; // RouteStatus enum
  approvalStatus: number; // RouteApprovalStatus enum
  type?: number; // RouteType enum
  approvedBy?: number;
  approvedByName?: string;
  approvedAt?: string;
  managerNotes?: string;
  createdAt: string;
  confirmedAt?: string;
  stops: RouteStopDto[];
}

interface RouteGenerationRequest {
  assignedSwapperId: number;
  zoneId: number;
  regionId?: number;
  targetDurationMinutes: number;
  batteryThreshold?: number;
  startTime?: string;
  name: string;
  description?: string;
  routeType?: string;
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
  assignedRegionId?: number;
  assignedRegionName?: string;
  isTemporaryPassword: boolean; // New field for temporary password status
  hasCompletedFirstLogin: boolean; // New field to track first login completion
}

interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  role: number; // UserRole enum value (0=Admin, 1=FleetManager, 2=BatterySwapper)
  assignedZoneId?: number;
  assignedRegionId?: number;
}

class ApiService {
  private api: AxiosInstance;
  private onAuthLogout?: () => void;

  constructor() {
    this.api = axios.create({
      baseURL: __DEV__ 
        ? 'http://192.168.129.51:5033/api' // Development - Use local IP for React Native
        : 'https://your-production-api.com/api', // Production URL
      timeout: 15000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  setAuthLogoutCallback(callback: () => void) {
    this.onAuthLogout = callback;
  }

  private setupInterceptors() {
    // Logging rule: at most method + URL + status, and only in __DEV__.
    // Never log request config, headers (Authorization), bodies (passwords) or response data.

    // Request interceptor
    this.api.interceptors.request.use(
      async (config) => {
        // Add authorization header if token exists
        try {
          const token = await AsyncStorage.getItem('authToken');
          if (token) {
            config.headers.Authorization = `Bearer ${token}`;
          }
        } catch {
          if (__DEV__) console.warn('Could not read auth token from storage');
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor
    this.api.interceptors.response.use(
      (response) => {
        if (__DEV__) {
          console.log(`API ${response.config.method?.toUpperCase()} ${response.config.url} -> ${response.status}`);
        }
        return response;
      },
      async (error) => {
        if (__DEV__) {
          console.log(`API ${error.config?.method?.toUpperCase()} ${error.config?.url} -> ${error.response?.status ?? 'no response'}`);
        }

        // 401: token expired or invalid. Drop the stored session and send the
        // user to login.
        if (error.response?.status === 401) {
          try {
            await AsyncStorage.removeItem('authToken');
            await AsyncStorage.removeItem('userData');
          } catch {
            if (__DEV__) console.warn('Could not clear auth storage');
          }
          this.onAuthLogout?.();
        }

        // 403: the user is logged in but not allowed (role or zone scope).
        // That is a normal answer, so the session stays. Give callers a clear message.
        if (error.response?.status === 403) {
          const backendMessage = error.response?.data?.message;
          error.message = typeof backendMessage === 'string' && backendMessage.trim()
            ? backendMessage
            : FORBIDDEN_MESSAGE;
        }

        return Promise.reject(error);
      }
    );
  }

  // Route endpoints
  async generateRoute(request: RouteGenerationRequest): Promise<RouteGenerationResponse> {
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

  async getRoute(id: number): Promise<RouteDto> {
    const response: AxiosResponse<RouteDto> = await this.api.get(`/routes/${id}`);
    return response.data;
  }

  async getAllRoutes(): Promise<RouteDto[]> {
    const response: AxiosResponse<RouteDto[]> = await this.api.get('/routes/all');
    return response.data;
  }

  // Route stop endpoints (keyed by stop id only, see docs/api-contract.md)
  // Manager only. `notes` is max 500 characters; an empty string clears it.
  async updateRouteStopInstructions(stopId: number, notes: string): Promise<RouteStopDto> {
    const response: AxiosResponse<RouteStopDto> = await this.api.put(`/routestops/${stopId}/instructions`, { notes });
    return response.data;
  }

  // Vehicle endpoints (aangepast aan werkelijke backend)
  async getAllLowBatteryVehicles(batteryThreshold: number = 25): Promise<VehicleDto[]> {
    const response: AxiosResponse<VehicleDto[]> = await this.api.get(`/vehicles/lowbattery/all?batteryThreshold=${batteryThreshold}`);
    return response.data;
  }

  async getVehiclesByZone(zoneId: number): Promise<VehicleDto[]> {
    const response: AxiosResponse<VehicleDto[]> = await this.api.get(`/vehicles/zone/${zoneId}`);
    return response.data;
  }

  async getAllVehicles(): Promise<VehicleDto[]> {
    const response: AxiosResponse<VehicleDto[]> = await this.api.get(`/vehicles/all`);
    return response.data;
  }

  // Zone endpoints
  async getZones(): Promise<ZoneDto[]> {
    const response: AxiosResponse<ZoneDto[]> = await this.api.get('/zones');
    return response.data;
  }

  // Region endpoints
  async getCountries(): Promise<CountryDto[]> {
    const response: AxiosResponse<CountryDto[]> = await this.api.get('/regions/countries');
    return response.data;
  }

  async getRegionsByCountry(countryCode: string): Promise<RegionDto[]> {
    const response: AxiosResponse<RegionDto[]> = await this.api.get(`/regions/country/${countryCode}`);
    return response.data;
  }

  async getRegions(): Promise<RegionDto[]> {
    const response: AxiosResponse<RegionDto[]> = await this.api.get('/regions');
    return response.data;
  }

  // Accepted values: RouteStopStatus.InProgress (2) and RouteStopStatus.Skipped (4).
  // Completing goes through completeRouteStop. `reason` (max 500) is only allowed with Skipped.
  async updateRouteStopStatus(stopId: number, status: number, reason?: string): Promise<RouteStopDto> {
    const body = reason ? { status, reason } : { status };
    const response: AxiosResponse<RouteStopDto> = await this.api.put(`/routestops/${stopId}/status`, body);
    return response.data;
  }

  // The backend answers { message }, which no caller needs.
  async completeRouteStop(stopId: number): Promise<void> {
    await this.api.post(`/routestops/${stopId}/complete`);
  }

  // Authentication endpoints
  async login(loginData: LoginRequest): Promise<LoginResponse> {
    const response: AxiosResponse<LoginResponse> = await this.api.post('/auth/login', loginData);
    return response.data;
  }

  async changePassword(oldPassword: string, newPassword: string): Promise<void> {
    await this.api.post('/auth/users/change-password', { 
      currentPassword: oldPassword, 
      newPassword: newPassword 
    });
  }

  async createUser(userData: CreateUserRequest): Promise<UserDto> {
    const response: AxiosResponse<UserDto> = await this.api.post('/auth/users', userData);
    return response.data;
  }

  async getUsers(): Promise<UserDto[]> {
    const response: AxiosResponse<UserDto[]> = await this.api.get('/auth/users');
    return response.data;
  }

  async getSwappers(zoneId?: number): Promise<UserDto[]> {
    let url = '/auth/users?role=2'; // BatterySwapper = 2
    if (zoneId) {
      url += `&zoneId=${zoneId}`;
    }
    const response: AxiosResponse<UserDto[]> = await this.api.get(url);
    return response.data;
  }

  async updateUser(id: number, userData: Partial<CreateUserRequest>): Promise<UserDto> {
    const response: AxiosResponse<UserDto> = await this.api.put(`/auth/users/${id}`, userData);
    return response.data;
  }

  async deleteUser(id: number): Promise<void> {
    await this.api.delete(`/auth/users/${id}/permanent`);
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
  CreateUserRequest,
  RegionDto,
  CountryDto
};
export default ApiService;
