// Types gebaseerd op de backend DTOs
export interface Route {
  id: number;
  swapperId: number;
  swapperName: string;
  zoneId: number;
  zoneName: string;
  date: string;
  targetDurationMinutes: number;
  status: RouteStatus;
  createdAt: string;
  confirmedAt?: string;
  stops: RouteStop[];
}

export interface RouteStop {
  id: number;
  routeId: number;
  vehicleId: number;
  vehicle: Vehicle;
  order: number;
  estimatedArrivalOffset: string;
  estimatedDurationAtStop: string;
  status: RouteStopStatus;
  actualArrivalTime?: string;
  actualDepartureTime?: string;
}

export interface Vehicle {
  id: number;
  externalId: string;
  zoneId: number;
  zoneName: string;
  latitude: number;
  longitude: number;
  batteryLevel: number;
  lastUpdated: string;
}

export interface Zone {
  id: number;
  name: string;
  countryCode: string;
  geoJsonBoundary: string;
}

export interface Swapper {
  id: number;
  name: string;
  email: string;
  phone?: string;
}

export interface RouteGenerationRequest {
  assignedSwapperId: string;
  zoneId: string;
  targetDurationMinutes: number;
  batteryThreshold: number;
  name: string;
  description: string;
  routeType?: string;
}

export enum RouteStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export enum RouteStopStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Skipped = 3
}

// API Response types
export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Navigation types
export type RootStackParamList = {
  Home: undefined;
  Login: undefined;
  FirstLogin: undefined;
  Routes: undefined;
  RouteGeneration: undefined;
  RouteDetails: { routeId: number };
  Vehicles: undefined;
  VehicleDetails: { vehicleId: number };
  Zones: undefined;
  ZoneDetails: { zoneId: number };
  Map: undefined;
  Settings: undefined;
  UserManagement: undefined;
};

export type BottomTabParamList = {
  Dashboard: undefined;
  Routes: undefined;
  Vehicles: undefined;
  Map: undefined;
  Settings: undefined;
};
