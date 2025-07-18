import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme, getBatteryColor } from '../theme';
import { apiService, VehicleDto } from '../services/api';
import { RootStackParamList } from '../types';
import { HoppyButton, HoppyLogo } from '../components';

type VehiclesScreenNavigationProp = StackNavigationProp<RootStackParamList>;

export default function VehiclesScreen() {
  const navigation = useNavigation<VehiclesScreenNavigationProp>();
  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedZoneId] = useState<number>(1); // Default zone - later from user selection
  const [showLowBatteryOnly, setShowLowBatteryOnly] = useState(false);

  useEffect(() => {
    fetchVehicles();
  }, [showLowBatteryOnly]);

  const fetchVehicles = async () => {
    try {
      setLoading(true);
      setError(null);
      
      let vehicleData: VehicleDto[];
      if (showLowBatteryOnly) {
        vehicleData = await apiService.getLowBatteryVehicles(selectedZoneId, 25);
      } else {
        vehicleData = await apiService.getVehiclesByZone(selectedZoneId);
      }
      
      setVehicles(vehicleData);
    } catch (err: any) {
      console.error('Error fetching vehicles:', err);
      setError(err.response?.data?.message || err.message || 'Fout bij ophalen van voertuigen');
    } finally {
      setLoading(false);
    }
  };

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchVehicles();
    setRefreshing(false);
  };

  const getBatteryIcon = (batteryLevel: number) => {
    if (batteryLevel >= 75) return 'battery-full';
    if (batteryLevel >= 50) return 'battery-half';
    if (batteryLevel >= 25) return 'battery-charging';
    return 'battery-dead';
  };

  const formatLastUpdated = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));
    
    if (diffInMinutes < 1) return 'Nu';
    if (diffInMinutes < 60) return `${diffInMinutes}m geleden`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)}u geleden`;
    return date.toLocaleDateString('nl-NL');
  };

  const renderVehicleItem = ({ item }: { item: VehicleDto }) => (
    <TouchableOpacity
      style={styles.vehicleCard}
      onPress={() => navigation.navigate('VehicleDetails', { vehicleId: item.id })}
    >
      <View style={styles.vehicleHeader}>
        <View style={styles.vehicleInfo}>
          <Text style={styles.vehicleId}>#{item.externalId}</Text>
          <Text style={styles.zoneName}>{item.zoneName}</Text>
        </View>
        <View style={[styles.batteryContainer, { backgroundColor: getBatteryColor(item.batteryLevel) }]}>
          <Ionicons 
            name={getBatteryIcon(item.batteryLevel)} 
            size={20} 
            color={HoppyColors.white} 
          />
          <Text style={styles.batteryText}>{item.batteryLevel}%</Text>
        </View>
      </View>
      
      <View style={styles.vehicleDetails}>
        <View style={styles.detailRow}>
          <Ionicons name="location-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.detailText}>
            {item.latitude.toFixed(4)}, {item.longitude.toFixed(4)}
          </Text>
        </View>
        <View style={styles.detailRow}>
          <Ionicons name="time-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.detailText}>
            Laatste update: {formatLastUpdated(item.lastUpdated)}
          </Text>
        </View>
      </View>
    </TouchableOpacity>
  );

  const renderFilterButton = () => (
    <TouchableOpacity
      style={[
        styles.filterButton,
        { backgroundColor: showLowBatteryOnly ? HoppyColors.warning : HoppyColors.gray200 }
      ]}
      onPress={() => setShowLowBatteryOnly(!showLowBatteryOnly)}
    >
      <Ionicons 
        name="battery-charging-outline" 
        size={18} 
        color={showLowBatteryOnly ? HoppyColors.white : HoppyColors.gray700} 
      />
      <Text style={[
        styles.filterButtonText,
        { color: showLowBatteryOnly ? HoppyColors.white : HoppyColors.gray700 }
      ]}>
        {showLowBatteryOnly ? 'Alle voertuigen' : 'Lage batterij'}
      </Text>
    </TouchableOpacity>
  );

  if (loading && !refreshing) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={HoppyColors.primary} />
        <Text style={styles.loadingText}>E-scooters & E-bikes laden...</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.errorContainer}>
        <Ionicons name="bicycle-outline" size={64} color={HoppyColors.error} />
        <Text style={styles.errorText}>Fout bij laden van voertuigen</Text>
        <Text style={styles.errorDetails}>{error}</Text>
        <TouchableOpacity 
          style={styles.retryButton}
          onPress={fetchVehicles}
        >
          <Text style={styles.retryButtonText}>Opnieuw proberen</Text>
        </TouchableOpacity>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View>
          <Text style={styles.statsText}>
            {vehicles.length} voertuigen gevonden
          </Text>
          <Text style={styles.statsSubtext}>
            {vehicles.filter(v => v.batteryLevel < 25).length} met lage batterij
          </Text>
        </View>
        {renderFilterButton()}
      </View>
      
      <FlatList
        data={vehicles}
        renderItem={renderVehicleItem}
        keyExtractor={(item) => item.id.toString()}
        contentContainerStyle={styles.listContainer}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            colors={[HoppyColors.primary]}
            tintColor={HoppyColors.primary}
          />
        }
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Ionicons name="bicycle-outline" size={64} color={HoppyColors.gray400} />
            <Text style={styles.emptyText}>Geen voertuigen gevonden</Text>
            <Text style={styles.emptySubtext}>
              {showLowBatteryOnly 
                ? 'Geen voertuigen met lage batterij in deze zone'
                : 'Geen voertuigen beschikbaar in deze zone'
              }
            </Text>
          </View>
        }
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
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: HoppyTheme.spacing.md,
    paddingVertical: HoppyTheme.spacing.md,
    backgroundColor: HoppyColors.white,
    borderBottomWidth: 1,
    borderBottomColor: HoppyColors.gray200,
  },
  statsText: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  statsSubtext: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginTop: 2,
  },
  filterButton: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: HoppyTheme.spacing.md,
    paddingVertical: HoppyTheme.spacing.sm,
    borderRadius: HoppyTheme.borderRadius.md,
    ...HoppyTheme.shadows.sm,
  },
  filterButtonText: {
    marginLeft: HoppyTheme.spacing.xs,
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '600',
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
  vehicleCard: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.md,
    marginBottom: HoppyTheme.spacing.md,
    ...HoppyTheme.shadows.md,
  },
  vehicleHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.md,
  },
  vehicleInfo: {
    flex: 1,
  },
  vehicleId: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  zoneName: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginTop: 2,
  },
  batteryContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: HoppyTheme.spacing.sm,
    paddingVertical: HoppyTheme.spacing.xs,
    borderRadius: HoppyTheme.borderRadius.md,
  },
  batteryText: {
    color: HoppyColors.white,
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '600',
    marginLeft: HoppyTheme.spacing.xs,
  },
  vehicleDetails: {
    gap: HoppyTheme.spacing.sm,
  },
  detailRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  detailText: {
    marginLeft: HoppyTheme.spacing.sm,
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray700,
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
