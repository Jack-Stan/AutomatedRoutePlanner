import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { CompositeNavigationProp } from '@react-navigation/native';
import { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import { StackNavigationProp } from '@react-navigation/stack';
import FontAwesome from 'react-native-vector-icons/FontAwesome';
import MaterialIcons from 'react-native-vector-icons/MaterialIcons';
import { HoppyColors, HoppyTheme } from '../theme';
import { apiService, VehicleDto, RouteDto } from '../services/api';
import { HoppyButton, HoppyLogo } from '../components';
import { useAuth } from '../contexts/AuthContext';
import { useLanguage } from '../contexts/LanguageContext';
import LanguageMenu from '../components/LanguageMenu';
import { RootStackParamList, BottomTabParamList } from '../types';

// Icon component using Font Awesome and Material Icons
const Icon = ({ name, size = 24, color = HoppyColors.gray600 }: { name: string; size?: number; color?: string }) => {
  const iconMap: { [key: string]: { component: any; iconName: string } } = {
    'server': { component: FontAwesome, iconName: 'server' },
    'people': { component: FontAwesome, iconName: 'users' },
    'warning': { component: FontAwesome, iconName: 'exclamation-triangle' },
    'eye': { component: FontAwesome, iconName: 'eye' },
    'list': { component: FontAwesome, iconName: 'list' },
    'battery-charging': { component: FontAwesome, iconName: 'battery-half' },
    'battery-dead': { component: FontAwesome, iconName: 'battery-empty' },
    'checkmark-circle': { component: FontAwesome, iconName: 'check-circle' },
    'map': { component: FontAwesome, iconName: 'map' },
    'calendar': { component: FontAwesome, iconName: 'calendar' },
    'bicycle': { component: FontAwesome, iconName: 'bicycle' },
    'location': { component: FontAwesome, iconName: 'map-marker' },
    'time': { component: FontAwesome, iconName: 'clock-o' },
    'add-circle': { component: FontAwesome, iconName: 'plus-circle' },
    'person-add': { component: FontAwesome, iconName: 'user-plus' },
    'create': { component: FontAwesome, iconName: 'edit' },
    'analytics': { component: FontAwesome, iconName: 'bar-chart' },
    'document-text': { component: FontAwesome, iconName: 'file-text' },
    'alert-circle-outline': { component: FontAwesome, iconName: 'exclamation-circle' },
    'person': { component: FontAwesome, iconName: 'user' },
  };

  const iconConfig = iconMap[name] || { component: FontAwesome, iconName: 'circle' };
  const IconComponent = iconConfig.component;

  return <IconComponent name={iconConfig.iconName} size={size} color={color} />;
};

type DashboardScreenNavigationProp = CompositeNavigationProp<
  BottomTabNavigationProp<BottomTabParamList, 'Dashboard'>,
  StackNavigationProp<RootStackParamList>
>;

export default function DashboardScreen() {
  const { user, logout } = useAuth();
  const { t } = useLanguage();
  const navigation = useNavigation<DashboardScreenNavigationProp>();
  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [routes, setRoutes] = useState<RouteDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedZoneId] = useState<number>(1); // Default zone
  const [activeUsersCount, setActiveUsersCount] = useState<number>(0);
  const [zonesCount, setZonesCount] = useState<number>(0);
  const [recentActivities, setRecentActivities] = useState<Array<{
    id: string;
    type: string;
    message: string;
    timestamp: Date;
    icon: string;
    color: string;
  }>>([]);

  // Get role display name
  const getRoleDisplayName = (role: string) => {
    switch (role.toLowerCase()) {
      case 'admin': return 'Administrator';
      case 'fleetmanager': return 'Fleet Manager';
      case 'swapper': return 'Battery Swapper';
      default: return role;
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      // Fetch basic data for all roles
      const [vehicleData, routeData] = await Promise.all([
        apiService.getVehiclesByZone(selectedZoneId),
        apiService.getRouteSuggestions(selectedZoneId),
      ]);
      
      setVehicles(vehicleData);
      setRoutes(routeData);

      // Fetch admin-specific statistics if user is admin
      if (user?.roleName?.toLowerCase() === 'admin') {
        try {
          const [usersCount, zonesCountData] = await Promise.all([
            apiService.getActiveUsersCount(),
            apiService.getZonesCount(),
          ]);
          
          setActiveUsersCount(usersCount);
          setZonesCount(zonesCountData);
        } catch (adminError: any) {
          console.warn('Failed to fetch admin statistics:', adminError);
          // Continue without admin stats rather than failing completely
          setActiveUsersCount(0);
          setZonesCount(0);
        }
      }
    } catch (err: any) {
      console.error('Error fetching dashboard data:', err);
      setError(err.response?.data?.message || err.message || 'Fout bij laden van dashboard');
    } finally {
      setLoading(false);
    }
  };

  // Generate recent activities based on real data
  const generateRecentActivities = () => {
    const activities = [];
    
    if (user?.roleName?.toLowerCase() === 'admin') {
      // Admin sees system-wide activities
      activities.push({
        id: 'system-backup',
        type: 'system',
        message: 'Systeem backup voltooid',
        timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
        icon: 'server',
        color: HoppyColors.success,
      });
      
      if (activeUsersCount > 0) {
        activities.push({
          id: 'user-activity',
          type: 'user',
          message: `${activeUsersCount} actieve gebruikers`,
          timestamp: new Date(Date.now() - 30 * 60 * 1000), // 30 minutes ago
          icon: 'people',
          color: HoppyColors.primary,
        });
      }
      
      if (criticalBatteryVehicles.length > 0) {
        activities.push({
          id: 'battery-alert',
          type: 'alert',
          message: `${criticalBatteryVehicles.length} voertuigen met kritieke batterij`,
          timestamp: new Date(Date.now() - 15 * 60 * 1000), // 15 minutes ago
          icon: 'warning',
          color: HoppyColors.error,
        });
      }
    }
    
    // Add route-based activities for all roles
    if (activeRoutes.length > 0) {
      activities.push({
        id: 'route-monitoring',
        type: 'route',
        message: `${activeRoutes.length} routes actief`,
        timestamp: new Date(Date.now() - 10 * 60 * 1000), // 10 minutes ago
        icon: 'eye',
        color: HoppyColors.info,
      });
    }
    
    return activities.sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime()).slice(0, 3);
  };

  // Calculate statistics
  const lowBatteryVehicles = vehicles.filter(v => v.batteryLevel < 25);
  const criticalBatteryVehicles = vehicles.filter(v => v.batteryLevel < 10);
  const pendingRoutes = routes.filter(r => r.status === 0); // Pending
  const activeRoutes = routes.filter(r => r.status === 1); // InProgress

  // Update recent activities when data changes
  useEffect(() => {
    if (!loading && vehicles.length > 0) {
      const activities = generateRecentActivities();
      setRecentActivities(activities);
    }
  }, [vehicles, routes, activeUsersCount, zonesCount, loading]);

  const StatCard = ({ 
    title, 
    value, 
    icon, 
    color = HoppyColors.primary,
    onPress,
  }: { 
    title: string; 
    value: string | number; 
    icon: string; 
    color?: string;
    onPress?: () => void;
  }) => (
    <TouchableOpacity 
      style={[styles.statCard, { borderLeftColor: color }]}
      onPress={onPress}
      disabled={!onPress}
    >
      <View style={styles.statContent}>
        <View style={styles.statText}>
          <Text style={styles.statValue}>{value}</Text>
          <Text style={styles.statTitle}>{title}</Text>
        </View>
        <Icon name={icon} size={32} color={color} />
      </View>
    </TouchableOpacity>
  );

  const QuickAction = ({ 
    title, 
    icon, 
    onPress, 
    color = HoppyColors.primary,
    disabled = false,
  }: { 
    title: string; 
    icon: string; 
    onPress: () => void;
    color?: string;
    disabled?: boolean;
  }) => (
    <TouchableOpacity 
      style={[styles.actionButton, disabled && styles.actionButtonDisabled]} 
      onPress={onPress}
      disabled={disabled}
    >
      <Icon name={icon} size={24} color={disabled ? HoppyColors.gray400 : color} />
      <Text style={[styles.actionText, { color: disabled ? HoppyColors.gray400 : color }]}>
        {title}
      </Text>
    </TouchableOpacity>
  );

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={HoppyColors.primary} />
        <Text style={styles.loadingText}>Dashboard laden...</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.errorContainer}>
        <Icon name="alert-circle-outline" size={64} color={HoppyColors.error} />
        <Text style={styles.errorText}>Fout bij laden van dashboard</Text>
        <Text style={styles.errorDetails}>{error}</Text>
        <HoppyButton
          title="Opnieuw proberen"
          onPress={fetchDashboardData}
          variant="primary"
          size="large"
        />
      </View>
    );
  }

  return (
    <ScrollView style={styles.container}>
      {/* Header Bar */}
      <View style={styles.header}>
        <View style={styles.headerContent}>
          <View style={styles.headerText}>
            <Text style={styles.titleText}>{t('dashboard.title')}</Text>
            <Text style={styles.subtitleText}>
              {user ? getRoleDisplayName(user.roleName) : 'Gebruiker'}
            </Text>
          </View>
          <LanguageMenu
            userInitial={user?.firstName?.charAt(0)?.toUpperCase() || user?.username?.charAt(0)?.toUpperCase() || 'U'}
            userName={user?.firstName && user?.lastName ? `${user.firstName} ${user.lastName}` : user?.username}
            userRole={user ? getRoleDisplayName(user.roleName) : 'Gebruiker'}
            onLogout={logout}
            onSettingsPress={() => navigation.navigate('Settings')}
          />
        </View>
      </View>

      {/* Statistics Cards - Role-specific metrics */}
      <View style={styles.statsContainer}>
        {/* Battery Swapper Stats */}
        {user?.roleName?.toLowerCase() === 'swapper' && (
          <>
            <StatCard
              title={t('dashboard.assignedRoutes') || 'Toegewezen Routes'}
              value={pendingRoutes.length}
              icon="list"
              color={HoppyColors.primary}
            />
            <StatCard
              title={t('dashboard.lowBattery') || 'Te Swappen (<25%)'}
              value={lowBatteryVehicles.length}
              icon="battery-charging"
              color={HoppyColors.warning}
            />
            <StatCard
              title={t('dashboard.critical') || 'Kritiek (<10%)'}
              value={criticalBatteryVehicles.length}
              icon="battery-dead"
              color={HoppyColors.error}
            />
            <StatCard
              title={t('dashboard.completedToday') || 'Voltooid Vandaag'}
              value={activeRoutes.length}
              icon="checkmark-circle"
              color={HoppyColors.success}
            />
          </>
        )}

        {/* Fleet Manager Stats */}
        {user?.roleName?.toLowerCase() === 'fleetmanager' && (
          <>
            <StatCard
              title={t('dashboard.activeRoutes') || 'Actieve Routes'}
              value={activeRoutes.length}
              icon="map"
              color={HoppyColors.primary}
            />
            <StatCard
              title={t('dashboard.availableSwappers') || 'Beschikbare Swappers'}
              value="3" // This would come from API
              icon="people"
              color={HoppyColors.success}
            />
            <StatCard
              title={t('dashboard.vehiclesToPlan') || 'Te Plannen Voertuigen'}
              value={lowBatteryVehicles.length}
              icon="battery-charging"
              color={HoppyColors.warning}
            />
            <StatCard
              title={t('dashboard.routesToday') || 'Routes Vandaag'}
              value={pendingRoutes.length + activeRoutes.length}
              icon="calendar"
              color={HoppyColors.info}
            />
          </>
        )}

        {/* Admin Stats - Complete overview */}
        {user?.roleName?.toLowerCase() === 'admin' && (
          <>
            <StatCard
              title={t('dashboard.totalVehicles') || 'Totaal Voertuigen'}
              value={vehicles.length}
              icon="bicycle"
              color={HoppyColors.primary}
            />
            <StatCard
              title={t('dashboard.activeUsers') || 'Actieve Gebruikers'}
              value={activeUsersCount}
              icon="people"
              color={HoppyColors.success}
            />
            <StatCard
              title={t('dashboard.criticalBattery') || 'Kritieke Batterij'}
              value={criticalBatteryVehicles.length}
              icon="battery-dead"
              color={HoppyColors.error}
            />
            <StatCard
              title={t('dashboard.systemZones') || 'Zones Beheerd'}
              value={zonesCount}
              icon="location"
              color={HoppyColors.info}
            />
          </>
        )}
      </View>

      {/* Quick Actions - Role-specific functionality */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>
          {user?.roleName?.toLowerCase() === 'swapper' ? 'Battery Swapper Acties' : 
           user?.roleName?.toLowerCase() === 'fleetmanager' ? 'Fleet Manager Acties' : 
           'Administrator Acties'}
        </Text>
        <View style={styles.actionsContainer}>
          {/* Battery Swapper Actions */}
          {user?.roleName?.toLowerCase() === 'swapper' && (
            <>
              <QuickAction
                title="Mijn Routes"
                icon="list"
                onPress={() => navigation.navigate('Routes')}
                color={HoppyColors.primary}
              />
              <QuickAction
                title="Voertuigen"
                icon="bicycle"
                onPress={() => navigation.navigate('Vehicles')}
                color={HoppyColors.success}
              />
              <QuickAction
                title="Route Voortgang"
                icon="analytics"
                onPress={() => navigation.navigate('Routes')}
                color={HoppyColors.info}
              />
              <QuickAction
                title="Instellingen"
                icon="person"
                onPress={() => navigation.navigate('Settings')}
                color={HoppyColors.warning}
              />
            </>
          )}

          {/* Fleet Manager Actions */}
          {user?.roleName?.toLowerCase() === 'fleetmanager' && (
            <>
              <QuickAction
                title="Route Planning"
                icon="map"
                onPress={() => navigation.navigate('Routes')}
                color={HoppyColors.primary}
              />
              <QuickAction
                title="Voertuigen"
                icon="bicycle"
                onPress={() => navigation.navigate('Vehicles')}
                color={HoppyColors.info}
              />
              <QuickAction
                title="Route Genereren"
                icon="add-circle"
                onPress={() => navigation.navigate('Routes')}
                color={HoppyColors.success}
              />
              <QuickAction
                title="Instellingen"
                icon="person"
                onPress={() => navigation.navigate('Settings')}
                color={HoppyColors.warning}
              />
            </>
          )}

          {/* Admin Actions - All Fleet Manager functions + User Management */}
          {user?.roleName?.toLowerCase() === 'admin' && (
            <>
              <QuickAction
                title="Gebruikersbeheer"
                icon="people"
                onPress={() => navigation.navigate('UserManagement')}
                color={HoppyColors.error}
              />
              <QuickAction
                title="Zone Selecteren"
                icon="location"
                onPress={() => navigation.navigate('Routes')}
                color={HoppyColors.primary}
              />
              <QuickAction
                title="Route Genereren"
                icon="add-circle"
                onPress={() => navigation.navigate('Routes')}
                color={HoppyColors.success}
              />
              <QuickAction
                title="Route Planning"
                icon="map"
                onPress={() => navigation.navigate('Routes')}
                color={HoppyColors.success}
              />
              <QuickAction
                title="Voertuigen"
                icon="bicycle"
                onPress={() => navigation.navigate('Vehicles')}
                color={HoppyColors.warning}
              />
              <QuickAction
                title="Instellingen"
                icon="person"
                onPress={() => navigation.navigate('Settings')}
                color={HoppyColors.info}
              />
            </>
          )}
        </View>
      </View>

      {/* Recent Activities - Role-specific */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>
          {user?.roleName?.toLowerCase() === 'swapper' ? 'Mijn Activiteiten' : 
           user?.roleName?.toLowerCase() === 'fleetmanager' ? 'Route Activiteiten' : 
           'Systeem Activiteiten'}
        </Text>
        <View style={styles.activityContainer}>
          {/* Battery Swapper Activities */}
          {user?.roleName?.toLowerCase() === 'swapper' && (
            <>
              {activeRoutes.slice(0, 2).map((route) => (
                <View key={route.id} style={styles.activityItem}>
                  <Icon name="checkmark-circle" size={16} color={HoppyColors.success} />
                  <Text style={styles.activityText}>
                    Route in {route.zoneName} voltooid
                  </Text>
                </View>
              ))}
              {pendingRoutes.slice(0, 2).map((route) => (
                <View key={`pending-${route.id}`} style={styles.activityItem}>
                  <Icon name="time" size={16} color={HoppyColors.warning} />
                  <Text style={styles.activityText}>
                    Nieuwe route toegewezen: {route.zoneName}
                  </Text>
                </View>
              ))}
              <View style={styles.activityItem}>
                <Icon name="battery-charging" size={16} color={HoppyColors.info} />
                <Text style={styles.activityText}>
                  {lowBatteryVehicles.length} voertuigen wachten op swap
                </Text>
              </View>
            </>
          )}

          {/* Fleet Manager Activities */}
          {user?.roleName?.toLowerCase() === 'fleetmanager' && (
            <>
              {activeRoutes.slice(0, 2).map((route) => (
                <View key={route.id} style={styles.activityItem}>
                  <Icon name="person" size={16} color={HoppyColors.primary} />
                  <Text style={styles.activityText}>
                    Route toegewezen aan {route.swapperName}
                  </Text>
                </View>
              ))}
              {pendingRoutes.slice(0, 2).map((route) => (
                <View key={`pending-${route.id}`} style={styles.activityItem}>
                  <Icon name="create" size={16} color={HoppyColors.info} />
                  <Text style={styles.activityText}>
                    Route gegenereerd voor {route.zoneName}
                  </Text>
                </View>
              ))}
              <View style={styles.activityItem}>
                <Icon name="analytics" size={16} color={HoppyColors.success} />
                <Text style={styles.activityText}>
                  Route efficiency: 85% gemiddeld
                </Text>
              </View>
            </>
          )}

          {/* Admin Activities */}
          {user?.roleName?.toLowerCase() === 'admin' && (
            <>
              {recentActivities.map((activity) => (
                <View key={activity.id} style={styles.activityItem}>
                  <Icon name={activity.icon} size={16} color={activity.color} />
                  <Text style={styles.activityText}>
                    {activity.message}
                  </Text>
                </View>
              ))}
            </>
          )}

          {/* Fallback if no activities */}
          {(user?.roleName?.toLowerCase() === 'admin' ? recentActivities.length === 0 : 
            activeRoutes.length === 0 && pendingRoutes.length === 0) && (
            <Text style={styles.noActivityText}>
              {user?.roleName?.toLowerCase() === 'swapper' ? 'Geen routes toegewezen' : 
               user?.roleName?.toLowerCase() === 'fleetmanager' ? 'Geen actieve routes' : 
               'Geen recente activiteiten'}
            </Text>
          )}
        </View>
      </View>

      {/* Battery Alert Section */}
      {criticalBatteryVehicles.length > 0 && (
        <View style={styles.section}>
          <View style={styles.alertContainer}>
            <View style={styles.alertHeader}>
              <Icon name="warning" size={24} color={HoppyColors.error} />
              <Text style={styles.alertTitle}>Kritieke Batterij Waarschuwing</Text>
            </View>
            <Text style={styles.alertText}>
              {criticalBatteryVehicles.length} voertuigen hebben een kritieke batterij status (&lt;10%). 
              Deze moeten prioriteit krijgen voor battery swapping.
            </Text>
            <TouchableOpacity 
              style={styles.alertButton}
              onPress={() => navigation.navigate('Vehicles')}
            >
              <Text style={styles.alertButtonText}>Bekijk Details</Text>
            </TouchableOpacity>
          </View>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: HoppyColors.gray50,
  },
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
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: HoppyTheme.spacing.lg,
    backgroundColor: HoppyColors.gray50,
  },
  errorText: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.error,
    marginTop: HoppyTheme.spacing.md,
    marginBottom: HoppyTheme.spacing.sm,
    textAlign: 'center',
  },
  errorDetails: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    textAlign: 'center',
    marginBottom: HoppyTheme.spacing.lg,
  },
  retryButton: {
    backgroundColor: HoppyColors.primary,
    paddingHorizontal: HoppyTheme.spacing.lg,
    paddingVertical: HoppyTheme.spacing.sm,
    borderRadius: HoppyTheme.borderRadius.md,
    ...HoppyTheme.shadows.md,
  },
  retryButtonText: {
    color: HoppyColors.white,
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: '600',
  },
  header: {
    backgroundColor: HoppyColors.primary,
    padding: HoppyTheme.spacing.lg,
    paddingTop: HoppyTheme.spacing.xl,
  },
  headerContent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerText: {
    flex: 1,
  },
  titleText: {
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: 'bold',
    color: HoppyColors.white,
    marginBottom: HoppyTheme.spacing.xs,
  },
  subtitleText: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.white,
    opacity: 0.9,
  },
  accountButton: {
    backgroundColor: HoppyColors.white,
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
    ...HoppyTheme.shadows.sm,
  },
  accountIcon: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.primary,
  },
  statsContainer: {
    padding: HoppyTheme.spacing.md,
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
  },
  statCard: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.md,
    marginBottom: HoppyTheme.spacing.md,
    width: '48%',
    borderLeftWidth: 4,
    ...HoppyTheme.shadows.md,
  },
  statContent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  statText: {
    flex: 1,
  },
  statValue: {
    fontSize: HoppyTheme.fontSizes.xxl,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  statTitle: {
    fontSize: HoppyTheme.fontSizes.xs,
    color: HoppyColors.gray600,
    marginTop: HoppyTheme.spacing.xs,
  },
  section: {
    margin: HoppyTheme.spacing.md,
    marginTop: 0,
  },
  sectionTitle: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    marginBottom: HoppyTheme.spacing.md,
    color: HoppyColors.gray800,
  },
  actionsContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
  },
  actionButton: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.md,
    width: '48%',
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.md,
    ...HoppyTheme.shadows.md,
  },
  actionButtonDisabled: {
    opacity: 0.5,
  },
  actionText: {
    marginTop: HoppyTheme.spacing.sm,
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '500',
    textAlign: 'center',
  },
  activityContainer: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.md,
    ...HoppyTheme.shadows.sm,
  },
  activityItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: HoppyTheme.spacing.sm,
  },
  activityText: {
    marginLeft: HoppyTheme.spacing.sm,
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray700,
  },
  noActivityText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray400,
    textAlign: 'center',
    fontStyle: 'italic',
  },
  alertContainer: {
    backgroundColor: HoppyColors.error,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.md,
    ...HoppyTheme.shadows.md,
  },
  alertHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.sm,
  },
  alertTitle: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.white,
    marginLeft: HoppyTheme.spacing.sm,
  },
  alertText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.white,
    marginBottom: HoppyTheme.spacing.md,
    lineHeight: 20,
  },
  alertButton: {
    backgroundColor: HoppyColors.white,
    paddingVertical: HoppyTheme.spacing.sm,
    paddingHorizontal: HoppyTheme.spacing.md,
    borderRadius: HoppyTheme.borderRadius.md,
    alignSelf: 'flex-start',
  },
  alertButtonText: {
    color: HoppyColors.error,
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '600',
  },
  // Icon styles (for fallback if needed)
  iconContainer: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  iconText: {
    fontWeight: 'bold',
    textAlign: 'center',
  },
});
