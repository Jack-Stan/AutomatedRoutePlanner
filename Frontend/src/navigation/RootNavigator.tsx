import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createStackNavigator } from '@react-navigation/stack';
import { Ionicons } from '@expo/vector-icons';
import { View, Text, ActivityIndicator, StyleSheet } from 'react-native';
import { HoppyColors, HoppyTheme } from '../theme';
import { BottomTabParamList, RootStackParamList } from '../types';
import { useAuth } from '../contexts/AuthContext';

// Import screens
import LoginScreen from '../screens/LoginScreen';
import DashboardScreen from '../screens/DashboardScreen';
import RoutesScreen from '../screens/RoutesScreen';
import RouteDetailsScreen from '../screens/RouteDetailsScreen';
import VehiclesScreen from '../screens/VehiclesScreen';
import VehicleDetailsScreen from '../screens/VehicleDetailsScreen';
import MapScreen from '../screens/MapScreen';
import SettingsScreen from '../screens/SettingsScreen';
import UserManagementScreen from '../screens/UserManagementScreen';

const Tab = createBottomTabNavigator<BottomTabParamList>();
const Stack = createStackNavigator<RootStackParamList>();

// Helper function for role display names
const getRoleDisplayName = (role: string): string => {
  switch (role.toLowerCase()) {
    case 'admin': return 'Administrator';
    case 'fleetmanager': return 'Fleet Manager';
    case 'swapper': return 'Battery Swapper';
    default: return 'Gebruiker';
  }
};

// Loading Screen
function LoadingScreen() {
  return (
    <View style={styles.loadingContainer}>
      <ActivityIndicator size="large" color={HoppyColors.primary} />
      <Text style={styles.loadingText}>Laden...</Text>
    </View>
  );
}

// Bottom Tab Navigator with Role-based Access
function BottomTabNavigator() {
  const { user } = useAuth();
  const userRole = user?.roleName?.toLowerCase(); // Use roleName instead of role
  
  // Helper function to determine tab icon
  const getTabIcon = (routeName: string, focused: boolean) => {
    let iconName: keyof typeof Ionicons.glyphMap;
    
    switch (routeName) {
      case 'Dashboard':
        iconName = focused ? 'home' : 'home-outline';
        break;
      case 'Routes':
        iconName = focused ? 'map' : 'map-outline';
        break;
      case 'Vehicles':
        iconName = focused ? 'bicycle' : 'bicycle-outline';
        break;
      case 'Map':
        iconName = focused ? 'location' : 'location-outline';
        break;
      case 'Settings':
        iconName = focused ? 'settings' : 'settings-outline';
        break;
      default:
        iconName = focused ? 'ellipse' : 'ellipse-outline';
    }
    return iconName;
  };
  
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          const iconName = getTabIcon(route.name, focused);
          return <Ionicons name={iconName} size={size} color={color} />;
        },
        tabBarActiveTintColor: HoppyColors.primary,
        tabBarInactiveTintColor: HoppyColors.gray400,
        tabBarStyle: {
          backgroundColor: HoppyColors.white,
          borderTopColor: HoppyColors.gray200,
          height: 60,
          paddingBottom: 8,
          paddingTop: 8,
        },
        headerStyle: {
          backgroundColor: HoppyColors.primary,
          shadowColor: HoppyColors.gray300,
        },
        headerTintColor: HoppyColors.white,
        headerTitleStyle: {
          fontWeight: 'bold',
          fontSize: 18,
        },
      })}
    >
      {/* Dashboard - Available to all roles */}
      <Tab.Screen 
        name="Dashboard" 
        component={DashboardScreen} 
        options={{ 
          title: 'Dashboard',
          headerShown: false
        }}
      />
      
      {/* Routes - Different functionality per role */}
      <Tab.Screen 
        name="Routes" 
        component={RoutesScreen} 
        options={{ 
          title: userRole === 'swapper' ? 'Mijn Routes' : 'Routes',
          headerTitle: userRole === 'swapper' ? 'Toegewezen Routes' : 'Route Beheer',
          headerShown: false
        }}
      />
      
      {/* Vehicles - Fleet Managers see all, Swappers see assigned zone */}
      <Tab.Screen 
        name="Vehicles" 
        component={VehiclesScreen} 
        options={{ 
          title: 'Voertuigen',
          headerTitle: userRole === 'swapper' ? 'Te Swappen Voertuigen' : 'Vloot Overzicht'
        }}
      />
      
      {/* Map - Different views per role */}
      <Tab.Screen 
        name="Map" 
        component={MapScreen} 
        options={{ 
          title: 'Kaart',
          headerTitle: userRole === 'swapper' ? 'Route Kaart' : 'Live Vloot Kaart'
        }}
      />
      
      {/* Settings - Role-specific settings */}
      <Tab.Screen 
        name="Settings" 
        component={SettingsScreen} 
        options={{ 
          title: 'Instellingen',
          headerTitle: `${getRoleDisplayName(user?.roleName || '')} Instellingen`
        }}
      />
    </Tab.Navigator>
  );
}

// Main Stack Navigator with Authentication
export default function RootNavigator() {
  const { user, isLoading } = useAuth();

  // Show loading screen while checking authentication
  if (isLoading) {
    return <LoadingScreen />;
  }

  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: HoppyColors.primary,
        },
        headerTintColor: HoppyColors.white,
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      {user ? (
        // User is authenticated - show main app
        <>
          <Stack.Screen 
            name="Home" 
            component={BottomTabNavigator} 
            options={{ headerShown: false }}
          />
          <Stack.Screen 
            name="RouteDetails" 
            component={RouteDetailsScreen}
            options={{ 
              title: 'Route Details',
            }}
          />
          <Stack.Screen 
            name="VehicleDetails" 
            component={VehicleDetailsScreen}
            options={{ 
              title: 'Voertuig Details',
            }}
          />
          <Stack.Screen 
            name="UserManagement" 
            component={UserManagementScreen}
            options={{ 
              title: 'Gebruikersbeheer',
            }}
          />
        </>
      ) : (
        // User is not authenticated - show login
        <Stack.Screen 
          name="Login" 
          component={LoginScreen}
          options={{ 
            title: 'Inloggen',
            headerShown: false // Login screen has its own header
          }}
        />
      )}
    </Stack.Navigator>
  );
}

const styles = StyleSheet.create({
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HoppyColors.white,
  },
  loadingText: {
    marginTop: 10,
    fontSize: 16,
    color: HoppyColors.primary,
  },
});
