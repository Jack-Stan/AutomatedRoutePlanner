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

// Loading Screen
function LoadingScreen() {
  return (
    <View style={styles.loadingContainer}>
      <ActivityIndicator size="large" color={HoppyColors.primary} />
      <Text style={styles.loadingText}>Laden...</Text>
    </View>
  );
}

// Bottom Tab Navigator with Hoppy branding
function BottomTabNavigator() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          let iconName: keyof typeof Ionicons.glyphMap;

          if (route.name === 'Dashboard') {
            iconName = focused ? 'home' : 'home-outline';
          } else if (route.name === 'Routes') {
            iconName = focused ? 'map' : 'map-outline';
          } else if (route.name === 'Vehicles') {
            iconName = focused ? 'bicycle' : 'bicycle-outline'; // E-bike/scooter icon
          } else if (route.name === 'Map') {
            iconName = focused ? 'location' : 'location-outline';
          } else {
            iconName = focused ? 'settings' : 'settings-outline';
          }

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
        tabBarLabelStyle: {
          fontSize: 12,
          fontWeight: '500',
        },
        headerStyle: {
          backgroundColor: HoppyColors.primary,
        },
        headerTintColor: HoppyColors.white,
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      })}
    >
      <Tab.Screen 
        name="Dashboard" 
        component={DashboardScreen} 
        options={{ 
          title: 'Dashboard',
          headerShown: false, // Dashboard heeft eigen header
        }}
      />
      <Tab.Screen 
        name="Routes" 
        component={RoutesScreen} 
        options={{ 
          title: 'Routes',
          headerTitle: 'Route Beheer'
        }}
      />
      <Tab.Screen 
        name="Vehicles" 
        component={VehiclesScreen} 
        options={{ 
          title: 'Voertuigen',
          headerTitle: 'E-Scooters & E-Bikes'
        }}
      />
      <Tab.Screen 
        name="Map" 
        component={MapScreen} 
        options={{ 
          title: 'Kaart',
          headerTitle: 'Live Kaart'
        }}
      />
      <Tab.Screen 
        name="Settings" 
        component={SettingsScreen} 
        options={{ 
          title: 'Instellingen',
          headerTitle: 'App Instellingen'
        }}
      />
    </Tab.Navigator>
  );
}

// Authentication Stack Navigator
function AuthStack() {
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
      <Stack.Screen 
        name="Login" 
        component={LoginScreen}
        options={{ 
          title: 'Hoppy Login',
          headerShown: false, // LoginScreen heeft eigen styling
        }}
      />
    </Stack.Navigator>
  );
}

// Main App Stack Navigator
function AppStack() {
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
    </Stack.Navigator>
  );
}

// Main Root Navigator met authenticatie controle
export default function RootNavigator() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingScreen />;
  }

  return isAuthenticated ? <AppStack /> : <AuthStack />;
}

const styles = StyleSheet.create({
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HoppyColors.gray50,
  },
  loadingText: {
    marginTop: HoppyTheme.spacing.md,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray600,
  },
});
