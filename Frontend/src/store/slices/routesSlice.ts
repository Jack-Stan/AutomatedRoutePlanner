import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { RouteDto, RouteGenerationRequest } from '../../services/api';
import { apiService } from '../../services/api';

interface RoutesState {
  routes: RouteDto[];
  currentRoute: RouteDto | null;
  loading: boolean;
  error: string | null;
}

const initialState: RoutesState = {
  routes: [],
  currentRoute: null,
  loading: false,
  error: null,
};

// Async thunks
export const fetchRoutes = createAsyncThunk(
  'routes/fetchRoutes',
  async () => {
    const response = await apiService.getRoutes();
    return response;
  }
);

export const fetchRoute = createAsyncThunk(
  'routes/fetchRoute',
  async (id: number) => {
    const response = await apiService.getRoute(id);
    return response;
  }
);

export const createRoute = createAsyncThunk(
  'routes/createRoute',
  async (request: RouteGenerationRequest) => {
    const response = await apiService.createRoute(request);
    return response;
  }
);

export const updateRoute = createAsyncThunk(
  'routes/updateRoute',
  async ({ id, route }: { id: number; route: Partial<RouteDto> }) => {
    const response = await apiService.updateRoute(id, route);
    return response;
  }
);

export const deleteRoute = createAsyncThunk(
  'routes/deleteRoute',
  async (id: number) => {
    await apiService.deleteRoute(id);
    return id;
  }
);

export const optimizeRoute = createAsyncThunk(
  'routes/optimizeRoute',
  async (routeId: number) => {
    const response = await apiService.optimizeRoute(routeId);
    return response;
  }
);

const routesSlice = createSlice({
  name: 'routes',
  initialState,
  reducers: {
    clearCurrentRoute: (state) => {
      state.currentRoute = null;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch routes
      .addCase(fetchRoutes.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchRoutes.fulfilled, (state, action: PayloadAction<RouteDto[]>) => {
        state.loading = false;
        state.routes = action.payload;
      })
      .addCase(fetchRoutes.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch routes';
      })
      // Fetch single route
      .addCase(fetchRoute.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchRoute.fulfilled, (state, action: PayloadAction<RouteDto>) => {
        state.loading = false;
        state.currentRoute = action.payload;
      })
      .addCase(fetchRoute.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch route';
      })
      // Create route
      .addCase(createRoute.fulfilled, (state, action: PayloadAction<RouteDto>) => {
        state.routes.push(action.payload);
      })
      // Update route
      .addCase(updateRoute.fulfilled, (state, action: PayloadAction<RouteDto>) => {
        const index = state.routes.findIndex(route => route.id === action.payload.id);
        if (index !== -1) {
          state.routes[index] = action.payload;
        }
        if (state.currentRoute?.id === action.payload.id) {
          state.currentRoute = action.payload;
        }
      })
      // Delete route
      .addCase(deleteRoute.fulfilled, (state, action: PayloadAction<number>) => {
        state.routes = state.routes.filter(route => route.id !== action.payload);
        if (state.currentRoute?.id === action.payload) {
          state.currentRoute = null;
        }
      })
      // Optimize route
      .addCase(optimizeRoute.fulfilled, (state, action: PayloadAction<RouteDto>) => {
        const index = state.routes.findIndex(route => route.id === action.payload.id);
        if (index !== -1) {
          state.routes[index] = action.payload;
        }
        if (state.currentRoute?.id === action.payload.id) {
          state.currentRoute = action.payload;
        }
      });
  },
});

export const { clearCurrentRoute, clearError } = routesSlice.actions;
export default routesSlice.reducer;
