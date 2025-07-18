import { configureStore } from '@reduxjs/toolkit';
import routesSlice from './slices/routesSlice';
import vehiclesSlice from './slices/vehiclesSlice';
import zonesSlice from './slices/zonesSlice';

export const store = configureStore({
  reducer: {
    routes: routesSlice,
    vehicles: vehiclesSlice,
    zones: zonesSlice,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
