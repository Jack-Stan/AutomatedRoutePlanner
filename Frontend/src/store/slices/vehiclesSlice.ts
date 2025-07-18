import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { Vehicle } from '../../types';
import { apiService } from '../../services/api';

interface VehiclesState {
  vehicles: Vehicle[];
  currentVehicle: Vehicle | null;
  loading: boolean;
  error: string | null;
}

const initialState: VehiclesState = {
  vehicles: [],
  currentVehicle: null,
  loading: false,
  error: null,
};

// Async thunks
export const fetchVehicles = createAsyncThunk(
  'vehicles/fetchVehicles',
  async () => {
    const response = await apiService.getVehicles();
    return response;
  }
);

export const fetchVehicle = createAsyncThunk(
  'vehicles/fetchVehicle',
  async (id: number) => {
    const response = await apiService.getVehicle(id);
    return response;
  }
);

export const createVehicle = createAsyncThunk(
  'vehicles/createVehicle',
  async (vehicle: Omit<Vehicle, 'id'>) => {
    const response = await apiService.createVehicle(vehicle);
    return response;
  }
);

export const updateVehicle = createAsyncThunk(
  'vehicles/updateVehicle',
  async ({ id, vehicle }: { id: number; vehicle: Partial<Vehicle> }) => {
    const response = await apiService.updateVehicle(id, vehicle);
    return response;
  }
);

export const deleteVehicle = createAsyncThunk(
  'vehicles/deleteVehicle',
  async (id: number) => {
    await apiService.deleteVehicle(id);
    return id;
  }
);

const vehiclesSlice = createSlice({
  name: 'vehicles',
  initialState,
  reducers: {
    clearCurrentVehicle: (state) => {
      state.currentVehicle = null;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch vehicles
      .addCase(fetchVehicles.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchVehicles.fulfilled, (state, action: PayloadAction<Vehicle[]>) => {
        state.loading = false;
        state.vehicles = action.payload;
      })
      .addCase(fetchVehicles.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch vehicles';
      })
      // Fetch single vehicle
      .addCase(fetchVehicle.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchVehicle.fulfilled, (state, action: PayloadAction<Vehicle>) => {
        state.loading = false;
        state.currentVehicle = action.payload;
      })
      .addCase(fetchVehicle.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch vehicle';
      })
      // Create vehicle
      .addCase(createVehicle.fulfilled, (state, action: PayloadAction<Vehicle>) => {
        state.vehicles.push(action.payload);
      })
      // Update vehicle
      .addCase(updateVehicle.fulfilled, (state, action: PayloadAction<Vehicle>) => {
        const index = state.vehicles.findIndex(vehicle => vehicle.id === action.payload.id);
        if (index !== -1) {
          state.vehicles[index] = action.payload;
        }
        if (state.currentVehicle?.id === action.payload.id) {
          state.currentVehicle = action.payload;
        }
      })
      // Delete vehicle
      .addCase(deleteVehicle.fulfilled, (state, action: PayloadAction<number>) => {
        state.vehicles = state.vehicles.filter(vehicle => vehicle.id !== action.payload);
        if (state.currentVehicle?.id === action.payload) {
          state.currentVehicle = null;
        }
      });
  },
});

export const { clearCurrentVehicle, clearError } = vehiclesSlice.actions;
export default vehiclesSlice.reducer;
