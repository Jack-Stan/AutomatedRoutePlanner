import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { ZoneDto } from '../../services/api';
import { apiService } from '../../services/api';

interface ZonesState {
  zones: ZoneDto[];
  currentZone: ZoneDto | null;
  loading: boolean;
  error: string | null;
}

const initialState: ZonesState = {
  zones: [],
  currentZone: null,
  loading: false,
  error: null,
};

// Async thunks
export const fetchZones = createAsyncThunk(
  'zones/fetchZones',
  async () => {
    const response = await apiService.getZones();
    return response;
  }
);

export const fetchZone = createAsyncThunk(
  'zones/fetchZone',
  async (id: number) => {
    const response = await apiService.getZone(id);
    return response;
  }
);

export const createZone = createAsyncThunk(
  'zones/createZone',
  async (zone: Partial<ZoneDto>) => {
    const response = await apiService.createZone(zone);
    return response;
  }
);

export const updateZone = createAsyncThunk(
  'zones/updateZone',
  async ({ id, zone }: { id: number; zone: Partial<ZoneDto> }) => {
    const response = await apiService.updateZone(id, zone);
    return response;
  }
);

export const deleteZone = createAsyncThunk(
  'zones/deleteZone',
  async (id: number) => {
    await apiService.deleteZone(id);
    return id;
  }
);

const zonesSlice = createSlice({
  name: 'zones',
  initialState,
  reducers: {
    clearCurrentZone: (state) => {
      state.currentZone = null;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch zones
      .addCase(fetchZones.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchZones.fulfilled, (state, action: PayloadAction<ZoneDto[]>) => {
        state.loading = false;
        state.zones = action.payload;
      })
      .addCase(fetchZones.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch zones';
      })
      // Fetch single zone
      .addCase(fetchZone.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchZone.fulfilled, (state, action: PayloadAction<ZoneDto>) => {
        state.loading = false;
        state.currentZone = action.payload;
      })
      .addCase(fetchZone.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch zone';
      })
      // Create zone
      .addCase(createZone.fulfilled, (state, action: PayloadAction<ZoneDto>) => {
        state.zones.push(action.payload);
      })
      // Update zone
      .addCase(updateZone.fulfilled, (state, action: PayloadAction<ZoneDto>) => {
        const index = state.zones.findIndex(zone => zone.id === action.payload.id);
        if (index !== -1) {
          state.zones[index] = action.payload;
        }
        if (state.currentZone?.id === action.payload.id) {
          state.currentZone = action.payload;
        }
      })
      // Delete zone
      .addCase(deleteZone.fulfilled, (state, action: PayloadAction<number>) => {
        state.zones = state.zones.filter(zone => zone.id !== action.payload);
        if (state.currentZone?.id === action.payload) {
          state.currentZone = null;
        }
      });
  },
});

export const { clearCurrentZone, clearError } = zonesSlice.actions;
export default zonesSlice.reducer;
