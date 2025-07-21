import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  Alert,
  TouchableOpacity,
} from 'react-native';
import { useRoute, useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme, getBatteryColor } from '../theme';
import { apiService, VehicleDto } from '../services/api';
import { RootStackParamList } from '../types';
import { HoppyButton, HoppyCard } from '../components';
import { useAuth } from '../contexts/AuthContext';

type VehicleDetailsScreenNavigationProp = StackNavigationProp<RootStackParamList>;

export default function VehicleDetailsScreen() {
  const route = useRoute();
  const navigation = useNavigation<VehicleDetailsScreenNavigationProp>();
  const { user } = useAuth();
  const { vehicleId } = route.params as { vehicleId: number };
  
  const [vehicle, setVehicle] = useState<VehicleDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const userRole = user?.roleName?.toLowerCase();

  useEffect(() => {
    fetchVehicleDetails();
  }, [vehicleId]);

  const fetchVehicleDetails = async () => {
    try {
      setLoading(true);
      setError(null);
      // In a real app, this would fetch individual vehicle details
      // For now, we'll simulate with the vehicles by zone API
      const vehicles = await apiService.getVehiclesByZone(1);
      const foundVehicle = vehicles.find(v => v.id === vehicleId);
      setVehicle(foundVehicle || null);
    } catch (err: any) {
      console.error('Error fetching vehicle details:', err);
      setError(err.response?.data?.message || err.message || 'Fout bij ophalen van voertuig details');
    } finally {
      setLoading(false);
    }
  };

  const handleSwapVehicle = () => {
    Alert.alert(
      'Batterij Swap',
      'Markeer deze batterij als vervangen?',
      [
        { text: 'Annuleren', style: 'cancel' },
        { 
          text: 'Swap Voltooid', 
          onPress: () => {
            Alert.alert('Succes', 'Batterij swap gemarkeerd als voltooid!');
            navigation.goBack();
          }
        }
      ]
    );
  };

  const getBatteryIcon = (level: number) => {
    if (level > 50) return 'battery-full';
    if (level > 25) return 'battery-half';
    return 'battery-dead';
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={HoppyColors.primary} />
        <Text style={styles.loadingText}>Voertuig details laden...</Text>
      </View>
    );
  }

  if (error || !vehicle) {
    return (
      <View style={styles.errorContainer}>
        <Ionicons name="alert-circle-outline" size={64} color={HoppyColors.error} />
        <Text style={styles.errorText}>Fout bij laden van voertuig</Text>
        <Text style={styles.errorDetails}>{error || 'Voertuig niet gevonden'}</Text>
        <HoppyButton
          title="Opnieuw proberen"
          onPress={fetchVehicleDetails}
          variant="primary"
          size="large"
        />
      </View>
    );
  }

  return (
    <ScrollView style={styles.container}>
      {/* Vehicle Header */}
      <View style={styles.header}>
        <View style={styles.headerContent}>
          <Text style={styles.vehicleTitle}>Voertuig #{vehicle.externalId}</Text>
          <Text style={styles.zoneName}>{vehicle.zoneName}</Text>
        </View>
        <View style={[styles.batteryContainer, { backgroundColor: getBatteryColor(vehicle.batteryLevel) }]}>
          <Ionicons 
            name={getBatteryIcon(vehicle.batteryLevel)} 
            size={32} 
            color={HoppyColors.white} 
          />
          <Text style={styles.batteryText}>{vehicle.batteryLevel}%</Text>
        </View>
      </View>

      {/* Vehicle Details */}
      <HoppyCard padding="medium" style={styles.detailsCard}>
        <Text style={styles.sectionTitle}>Voertuig Informatie</Text>
        
        <View style={styles.detailRow}>
          <Ionicons name="location-outline" size={20} color={HoppyColors.primary} />
          <View style={styles.detailContent}>
            <Text style={styles.detailLabel}>Locatie</Text>
            <Text style={styles.detailValue}>
              {vehicle.latitude.toFixed(6)}, {vehicle.longitude.toFixed(6)}
            </Text>
          </View>
        </View>

        <View style={styles.detailRow}>
          <Ionicons name="map-outline" size={20} color={HoppyColors.primary} />
          <View style={styles.detailContent}>
            <Text style={styles.detailLabel}>Zone</Text>
            <Text style={styles.detailValue}>{vehicle.zoneName}</Text>
          </View>
        </View>

        <View style={styles.detailRow}>
          <Ionicons name="battery-charging-outline" size={20} color={HoppyColors.primary} />
          <View style={styles.detailContent}>
            <Text style={styles.detailLabel}>Batterij Status</Text>
            <Text style={[styles.detailValue, { color: getBatteryColor(vehicle.batteryLevel) }]}>
              {vehicle.batteryLevel}% - {vehicle.batteryLevel > 50 ? 'Goed' : 
                                       vehicle.batteryLevel > 25 ? 'Gemiddeld' : 'Kritiek'}
            </Text>
          </View>
        </View>

        <View style={styles.detailRow}>
          <Ionicons name="time-outline" size={20} color={HoppyColors.primary} />
          <View style={styles.detailContent}>
            <Text style={styles.detailLabel}>Laatste Update</Text>
            <Text style={styles.detailValue}>
              {new Date(vehicle.lastUpdated).toLocaleString('nl-NL')}
            </Text>
          </View>
        </View>
      </HoppyCard>

      {/* Battery Status Card */}
      <HoppyCard padding="medium" style={styles.batteryCard}>
        <Text style={styles.sectionTitle}>Batterij Status</Text>
        <View style={styles.batteryProgress}>
          <View style={styles.batteryProgressBar}>
            <View 
              style={[
                styles.batteryProgressFill, 
                { 
                  width: `${vehicle.batteryLevel}%`,
                  backgroundColor: getBatteryColor(vehicle.batteryLevel)
                }
              ]} 
            />
          </View>
          <Text style={styles.batteryPercentage}>{vehicle.batteryLevel}%</Text>
        </View>
        
        {vehicle.batteryLevel <= 25 && (
          <View style={styles.warningContainer}>
            <Ionicons name="warning-outline" size={20} color={HoppyColors.warning} />
            <Text style={styles.warningText}>
              Dit voertuig heeft een lage batterij en moet worden opgeladen
            </Text>
          </View>
        )}
      </HoppyCard>

      {/* Action Buttons */}
      {userRole === 'swapper' && vehicle.batteryLevel <= 25 && (
        <HoppyCard padding="medium" style={styles.actionCard}>
          <Text style={styles.sectionTitle}>Acties</Text>
          <TouchableOpacity
            style={styles.swapButton}
            onPress={handleSwapVehicle}
          >
            <Ionicons name="battery-charging-outline" size={20} color={HoppyColors.white} />
            <Text style={styles.swapButtonText}>Batterij Swap Voltooien</Text>
          </TouchableOpacity>
        </HoppyCard>
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
  header: {
    backgroundColor: HoppyColors.primary,
    padding: HoppyTheme.spacing.lg,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerContent: {
    flex: 1,
  },
  vehicleTitle: {
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: 'bold',
    color: HoppyColors.white,
    marginBottom: HoppyTheme.spacing.xs,
  },
  zoneName: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.white,
    opacity: 0.9,
  },
  batteryContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: HoppyTheme.spacing.md,
    paddingVertical: HoppyTheme.spacing.sm,
    borderRadius: HoppyTheme.borderRadius.lg,
    ...HoppyTheme.shadows.sm,
  },
  batteryText: {
    color: HoppyColors.white,
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    marginLeft: HoppyTheme.spacing.sm,
  },
  detailsCard: {
    margin: HoppyTheme.spacing.md,
  },
  sectionTitle: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.md,
  },
  detailRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.md,
  },
  detailContent: {
    flex: 1,
    marginLeft: HoppyTheme.spacing.md,
  },
  detailLabel: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginBottom: HoppyTheme.spacing.xs,
  },
  detailValue: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray800,
    fontWeight: '500',
  },
  batteryCard: {
    margin: HoppyTheme.spacing.md,
    marginTop: 0,
  },
  batteryProgress: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.md,
  },
  batteryProgressBar: {
    flex: 1,
    height: 12,
    backgroundColor: HoppyColors.gray200,
    borderRadius: 6,
    overflow: 'hidden',
    marginRight: HoppyTheme.spacing.md,
  },
  batteryProgressFill: {
    height: '100%',
    borderRadius: 6,
  },
  batteryPercentage: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    minWidth: 40,
  },
  warningContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: HoppyColors.warning + '20',
    padding: HoppyTheme.spacing.md,
    borderRadius: HoppyTheme.borderRadius.md,
    borderLeftWidth: 4,
    borderLeftColor: HoppyColors.warning,
  },
  warningText: {
    flex: 1,
    marginLeft: HoppyTheme.spacing.sm,
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.warning,
    fontWeight: '500',
  },
  actionCard: {
    margin: HoppyTheme.spacing.md,
    marginTop: 0,
  },
  swapButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: HoppyColors.success,
    paddingVertical: HoppyTheme.spacing.md,
    borderRadius: HoppyTheme.borderRadius.md,
    ...HoppyTheme.shadows.md,
  },
  swapButtonText: {
    color: HoppyColors.white,
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    marginLeft: HoppyTheme.spacing.sm,
  },
});
