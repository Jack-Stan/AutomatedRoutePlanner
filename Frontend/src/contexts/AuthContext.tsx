import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { apiService } from '../services/api';

export interface User {
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

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  changePassword: (oldPassword: string, newPassword: string) => Promise<void>;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    checkAuthStatus();
  }, []);

  const checkAuthStatus = async () => {
    try {
      const token = await AsyncStorage.getItem('authToken');
      const userData = await AsyncStorage.getItem('userData');
      
      if (token && userData) {
        const parsedUser = JSON.parse(userData);
        setUser(parsedUser);
      }
    } catch (error) {
      console.error('Error checking auth status:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (username: string, password: string) => {
    try {
      setIsLoading(true);
      
      // Use API service for login
      const { token, user: userData } = await apiService.login({ username, password });

      // Store authentication data
      await AsyncStorage.setItem('authToken', token);
      await AsyncStorage.setItem('userData', JSON.stringify(userData));
      
      setUser(userData);
    } catch (error: any) {
      console.error('Login error:', error);
      throw new Error(error.response?.data?.message || 'Ongeldige inloggegevens');
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    try {
      await AsyncStorage.removeItem('authToken');
      await AsyncStorage.removeItem('userData');
      setUser(null);
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  const changePassword = async (oldPassword: string, newPassword: string) => {
    try {
      setIsLoading(true);
      
      const response = await apiService.changePassword(oldPassword, newPassword);
      
      // Update user data to reflect password change
      if (user) {
        const updatedUser = { 
          ...user, 
          isTemporaryPassword: false, 
          hasCompletedFirstLogin: true 
        };
        setUser(updatedUser);
        await AsyncStorage.setItem('userData', JSON.stringify(updatedUser));
      }
      
    } catch (error: any) {
      console.error('Change password error:', error);
      throw new Error(error.response?.data?.message || 'Kon wachtwoord niet wijzigen');
    } finally {
      setIsLoading(false);
    }
  };

  const value: AuthContextType = {
    user,
    isLoading,
    login,
    logout,
    changePassword,
    isAuthenticated: !!user,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

// Helper functies voor rol checks
export const isAdmin = (user: User | null): boolean => user?.roleName?.toLowerCase() === 'admin';
export const isFleetManager = (user: User | null): boolean => user?.roleName?.toLowerCase() === 'fleetmanager';
export const isSwapper = (user: User | null): boolean => user?.roleName?.toLowerCase() === 'batteryswapper';

export const canManageUsers = (user: User | null): boolean => 
  user?.roleName?.toLowerCase() === 'admin' || user?.roleName?.toLowerCase() === 'fleetmanager';

export const getRoleDisplayName = (role: string): string => {
  switch (role.toLowerCase()) {
    case 'admin': return 'Administrator';
    case 'fleetmanager': return 'Fleet Manager';
    case 'batteryswapper': return 'Battery Swapper';
    default: return 'Onbekend';
  }
}
