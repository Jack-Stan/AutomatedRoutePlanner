import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  RefreshControl,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { apiService, RouteDto } from '../services/api';
import { RootStackParamList } from '../types';
import { HoppyButton, HoppyLogo } from '../components';

type RoutesScreenNavigationProp = StackNavigationProp<RootStackParamList>;

// Route Status enum (matching backend)
enum RouteStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export default function RoutesScreen() {
  const navigation = useNavigation<RoutesScreenNavigationProp>();
  const [routes, setRoutes] = useState<RouteDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedZoneId] = useState<number>(1); // Default zone - later from user selection

  useEffect(() => {
    fetchRoutes();
  }, []);

  const fetchRoutes = async () => {
    try {
      setLoading(true);
      setError(null);
      
      // Fetch route suggestions for the selected zone
      const routeData = await apiService.getRouteSuggestions(selectedZoneId);
      setRoutes(routeData);
    } catch (err: any) {
      console.error('Error fetching routes:', err);
      setError(err.response?.data?.message || err.message || 'Fout bij ophalen van routes');
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: RouteStatus) => {
    switch (status) {
      case RouteStatus.Pending:
        return HoppyColors.pending;
      case RouteStatus.InProgress:
        return HoppyColors.active;
      case RouteStatus.Completed:
        return HoppyColors.completed;
      case RouteStatus.Cancelled:
        return HoppyColors.cancelled;
      default:
        return HoppyColors.gray400;
    }
  };

  const getStatusText = (status: RouteStatus) => {
    switch (status) {
      case RouteStatus.Pending:
        return 'In afwachting';
      case RouteStatus.InProgress:
        return 'Onderweg';
      case RouteStatus.Completed:
        return 'Voltooid';
      case RouteStatus.Cancelled:
        return 'Geannuleerd';
      default:
        return 'Onbekend';
    }
  };

  const handleConfirmRoute = async (routeId: number) => {
    try {
      await apiService.confirmRoute(routeId);
      Alert.alert('Succes', 'Route bevestigd!', [
        { text: 'OK', onPress: fetchRoutes }
      ]);
    } catch (err: any) {
      Alert.alert('Fout', err.response?.data?.message || 'Fout bij bevestigen van route');
    }
  };

  const renderRouteItem = ({ item }: { item: RouteDto }) => (
    <TouchableOpacity
      style={styles.routeCard}
      onPress={() => navigation.navigate('RouteDetails', { routeId: item.id })}
    >
      <View style={styles.routeHeader}>
        <View style={styles.routeTitle}>
          <Text style={styles.routeName}>{item.zoneName} Route</Text>
          <Text style={styles.swapperName}>Swapper: {item.swapperName}</Text>
        </View>
        <View style={[styles.statusBadge, { backgroundColor: getStatusColor(item.status) }]}>
          <Text style={styles.statusText}>{getStatusText(item.status)}</Text>
        </View>
      </View>
      
      <View style={styles.routeDetails}>
        <View style={styles.infoRow}>
          <Ionicons name="calendar-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.infoText}>
            Datum: {new Date(item.date).toLocaleDateString('nl-NL')}
          </Text>
        </View>
        <View style={styles.infoRow}>
          <Ionicons name="time-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.infoText}>
            Doel: {item.targetDurationMinutes} minuten
          </Text>
        </View>
        <View style={styles.infoRow}>
          <Ionicons name="navigate-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.infoText}>Stops: {item.stops?.length || 0}</Text>
        </View>
        <View style={styles.infoRow}>
          <Ionicons name="location-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.infoText}>Zone: {item.zoneName}</Text>
        </View>
      </View>

      {item.status === RouteStatus.Pending && (
        <View style={{ marginTop: HoppyTheme.spacing.md }}>
          <HoppyButton
            title="Route Bevestigen"
            onPress={() => handleConfirmRoute(item.id)}
            variant="primary"
            size="medium"
          />
        </View>
      )}
    </TouchableOpacity>
  );

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={HoppyColors.primary} />
        <Text style={styles.loadingText}>Routes laden...</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.errorContainer}>
        <Ionicons name="alert-circle-outline" size={48} color={HoppyColors.error} />
        <Text style={styles.errorText}>Fout bij laden van routes</Text>
        <Text style={styles.errorDetails}>{error}</Text>
        <HoppyButton
          title="Opnieuw proberen"
          onPress={fetchRoutes}
          variant="primary"
          size="large"
        />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View style={styles.headerContent}>
          <View style={styles.headerText}>
            <Text style={styles.headerTitle}>Route Suggesties</Text>
            <Text style={styles.headerSubtitle}>Don't worry, be Hoppy! 🛴</Text>
          </View>
          <HoppyLogo size="small" showText={false} />
        </View>
      </View>
      
      <FlatList
        data={routes}
        renderItem={renderRouteItem}
        keyExtractor={(item) => item.id.toString()}
        contentContainerStyle={styles.listContainer}
        showsVerticalScrollIndicator={false}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Ionicons name="map-outline" size={64} color={HoppyColors.gray400} />
            <Text style={styles.emptyText}>Geen routes gevonden</Text>
            <Text style={styles.emptySubtext}>
              Vraag nieuwe route suggesties aan voor deze zone
            </Text>
          </View>
        }
        refreshing={loading}
        onRefresh={fetchRoutes}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: HoppyColors.gray50,
  },
  header: {
    backgroundColor: HoppyColors.primary,
    paddingHorizontal: HoppyTheme.spacing.md,
    paddingVertical: HoppyTheme.spacing.lg,
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
  headerTitle: {
    fontSize: HoppyTheme.fontSizes.xxl,
    fontWeight: 'bold',
    color: HoppyColors.white,
    marginBottom: HoppyTheme.spacing.xs,
  },
  headerSubtitle: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.white,
    opacity: 0.9,
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
  listContainer: {
    padding: HoppyTheme.spacing.md,
  },
  routeCard: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.md,
    marginBottom: HoppyTheme.spacing.md,
    ...HoppyTheme.shadows.md,
  },
  routeHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: HoppyTheme.spacing.md,
  },
  routeTitle: {
    flex: 1,
    marginRight: HoppyTheme.spacing.sm,
  },
  routeName: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.xs,
  },
  swapperName: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
  },
  statusBadge: {
    paddingHorizontal: HoppyTheme.spacing.sm,
    paddingVertical: HoppyTheme.spacing.xs,
    borderRadius: HoppyTheme.borderRadius.full,
  },
  statusText: {
    color: HoppyColors.white,
    fontSize: HoppyTheme.fontSizes.xs,
    fontWeight: '600',
  },
  routeDetails: {
    gap: HoppyTheme.spacing.sm,
  },
  infoRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  infoText: {
    marginLeft: HoppyTheme.spacing.sm,
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray700,
  },
  confirmButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: HoppyColors.success,
    marginTop: HoppyTheme.spacing.md,
    paddingVertical: HoppyTheme.spacing.sm,
    borderRadius: HoppyTheme.borderRadius.md,
    ...HoppyTheme.shadows.sm,
  },
  confirmButtonText: {
    color: HoppyColors.white,
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: '600',
    marginLeft: HoppyTheme.spacing.xs,
  },
  emptyContainer: {
    alignItems: 'center',
    marginTop: 100,
    paddingHorizontal: HoppyTheme.spacing.lg,
  },
  emptyText: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray400,
    marginTop: HoppyTheme.spacing.md,
    textAlign: 'center',
  },
  emptySubtext: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray400,
    marginTop: HoppyTheme.spacing.sm,
    textAlign: 'center',
  },
});
