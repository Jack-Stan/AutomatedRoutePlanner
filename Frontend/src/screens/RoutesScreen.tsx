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
  Modal,
  TextInput,
  ScrollView,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { CompositeNavigationProp } from '@react-navigation/native';
import { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import { StackNavigationProp } from '@react-navigation/stack';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { apiService, RouteDto } from '../services/api';
import { RootStackParamList, BottomTabParamList } from '../types';
import { HoppyButton, HoppyLogo } from '../components';
import { useAuth } from '../contexts/AuthContext';

type RoutesScreenNavigationProp = CompositeNavigationProp<
  BottomTabNavigationProp<BottomTabParamList, 'Routes'>,
  StackNavigationProp<RootStackParamList>
>;

// Route Status enum (matching backend)
enum RouteStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export default function RoutesScreen() {
  const navigation = useNavigation<RoutesScreenNavigationProp>();
  const { user } = useAuth();
  const [routes, setRoutes] = useState<RouteDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedZoneId, setSelectedZoneId] = useState<number>(1); // Default zone
  const [showGenerateModal, setShowGenerateModal] = useState(false);
  const [routeDuration, setRouteDuration] = useState<string>('240'); // 4 hours in minutes
  const [batteryThreshold, setBatteryThreshold] = useState<string>('25');
  const [generating, setGenerating] = useState(false);

  const userRole = user?.roleName?.toLowerCase();

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

  const handleGenerateRoute = async () => {
    try {
      setGenerating(true);
      
      // In a real app, this would call an API endpoint to generate an optimized route
      // For now, we'll simulate the process
      await new Promise(resolve => setTimeout(resolve, 2000)); // Simulate API call
      
      Alert.alert(
        'Route Gegenereerd',
        `Nieuwe route gegenereerd voor zone ${selectedZoneId}\n` +
        `Duur: ${Math.floor(parseInt(routeDuration) / 60)}u ${parseInt(routeDuration) % 60}m\n` +
        `Batterij drempel: ${batteryThreshold}%\n\n` +
        'Route is toegevoegd aan de lijst.',
        [
          { text: 'OK', onPress: () => {
            setShowGenerateModal(false);
            fetchRoutes(); // Refresh the route list
          }}
        ]
      );
    } catch (err: any) {
      Alert.alert('Fout', 'Er is een fout opgetreden bij het genereren van de route');
    } finally {
      setGenerating(false);
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
              {(userRole === 'admin' || userRole === 'fleetmanager') 
                ? 'Genereer een nieuwe route met de + knop' 
                : 'Vraag nieuwe route suggesties aan voor deze zone'}
            </Text>
          </View>
        }
        refreshing={loading}
        onRefresh={fetchRoutes}
      />

      {/* Floating Action Button for Managers and Admins */}
      {(userRole === 'admin' || userRole === 'fleetmanager') && (
        <TouchableOpacity
          style={styles.fab}
          onPress={() => setShowGenerateModal(true)}
        >
          <Ionicons name="add" size={24} color={HoppyColors.white} />
        </TouchableOpacity>
      )}

      {/* Route Generation Modal */}
      <Modal
        visible={showGenerateModal}
        animationType="slide"
        transparent={true}
        onRequestClose={() => setShowGenerateModal(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Nieuwe Route Genereren</Text>
              <TouchableOpacity
                onPress={() => setShowGenerateModal(false)}
                style={styles.closeButton}
              >
                <Ionicons name="close" size={24} color={HoppyColors.gray600} />
              </TouchableOpacity>
            </View>

            <ScrollView style={styles.modalBody}>
              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>Zone</Text>
                <View style={styles.zoneSelector}>
                  <TouchableOpacity
                    style={[styles.zoneButton, selectedZoneId === 1 && styles.zoneButtonActive]}
                    onPress={() => setSelectedZoneId(1)}
                  >
                    <Text style={[styles.zoneButtonText, selectedZoneId === 1 && styles.zoneButtonTextActive]}>
                      Gent
                    </Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={[styles.zoneButton, selectedZoneId === 2 && styles.zoneButtonActive]}
                    onPress={() => setSelectedZoneId(2)}
                  >
                    <Text style={[styles.zoneButtonText, selectedZoneId === 2 && styles.zoneButtonTextActive]}>
                      Brussel
                    </Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={[styles.zoneButton, selectedZoneId === 3 && styles.zoneButtonActive]}
                    onPress={() => setSelectedZoneId(3)}
                  >
                    <Text style={[styles.zoneButtonText, selectedZoneId === 3 && styles.zoneButtonTextActive]}>
                      Antwerpen
                    </Text>
                  </TouchableOpacity>
                </View>
              </View>

              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>Route Duur (minuten)</Text>
                <View style={styles.durationSelector}>
                  <TouchableOpacity
                    style={[styles.durationButton, routeDuration === '240' && styles.durationButtonActive]}
                    onPress={() => setRouteDuration('240')}
                  >
                    <Text style={[styles.durationButtonText, routeDuration === '240' && styles.durationButtonTextActive]}>
                      4u
                    </Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={[styles.durationButton, routeDuration === '360' && styles.durationButtonActive]}
                    onPress={() => setRouteDuration('360')}
                  >
                    <Text style={[styles.durationButtonText, routeDuration === '360' && styles.durationButtonTextActive]}>
                      6u
                    </Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={[styles.durationButton, routeDuration === '480' && styles.durationButtonActive]}
                    onPress={() => setRouteDuration('480')}
                  >
                    <Text style={[styles.durationButtonText, routeDuration === '480' && styles.durationButtonTextActive]}>
                      8u
                    </Text>
                  </TouchableOpacity>
                </View>
              </View>

              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>Batterij Drempel (%)</Text>
                <TextInput
                  style={styles.textInput}
                  value={batteryThreshold}
                  onChangeText={setBatteryThreshold}
                  keyboardType="numeric"
                  placeholder="25"
                />
                <Text style={styles.inputHint}>
                  Voertuigen onder dit percentage worden opgenomen in de route
                </Text>
              </View>
            </ScrollView>

            <View style={styles.modalFooter}>
              <TouchableOpacity
                style={[styles.modalButton, styles.cancelButton]}
                onPress={() => setShowGenerateModal(false)}
              >
                <Text style={styles.cancelButtonText}>Annuleren</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.modalButton, styles.generateButton]}
                onPress={handleGenerateRoute}
                disabled={generating}
              >
                {generating ? (
                  <ActivityIndicator size="small" color={HoppyColors.white} />
                ) : (
                  <Text style={styles.generateButtonText}>Route Genereren</Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
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
  
  // Floating Action Button
  fab: {
    position: 'absolute',
    bottom: 20,
    right: 20,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: HoppyColors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    ...HoppyTheme.shadows.lg,
  },
  
  // Modal styles
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  modalContent: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    width: '90%',
    maxWidth: 400,
    maxHeight: '80%',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: HoppyTheme.spacing.lg,
    borderBottomWidth: 1,
    borderBottomColor: HoppyColors.gray200,
  },
  modalTitle: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  closeButton: {
    padding: HoppyTheme.spacing.sm,
  },
  modalBody: {
    padding: HoppyTheme.spacing.lg,
  },
  inputGroup: {
    marginBottom: HoppyTheme.spacing.lg,
  },
  inputLabel: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.sm,
  },
  zoneSelector: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: HoppyTheme.spacing.sm,
  },
  zoneButton: {
    flex: 1,
    paddingVertical: HoppyTheme.spacing.sm,
    paddingHorizontal: HoppyTheme.spacing.md,
    borderRadius: HoppyTheme.borderRadius.md,
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    backgroundColor: HoppyColors.white,
    alignItems: 'center',
  },
  zoneButtonActive: {
    backgroundColor: HoppyColors.primary,
    borderColor: HoppyColors.primary,
  },
  zoneButtonText: {
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '600',
    color: HoppyColors.gray700,
  },
  zoneButtonTextActive: {
    color: HoppyColors.white,
  },
  durationSelector: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: HoppyTheme.spacing.sm,
  },
  durationButton: {
    flex: 1,
    paddingVertical: HoppyTheme.spacing.sm,
    paddingHorizontal: HoppyTheme.spacing.md,
    borderRadius: HoppyTheme.borderRadius.md,
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    backgroundColor: HoppyColors.white,
    alignItems: 'center',
  },
  durationButtonActive: {
    backgroundColor: HoppyColors.primary,
    borderColor: HoppyColors.primary,
  },
  durationButtonText: {
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '600',
    color: HoppyColors.gray700,
  },
  durationButtonTextActive: {
    color: HoppyColors.white,
  },
  textInput: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: HoppyTheme.borderRadius.md,
    paddingHorizontal: HoppyTheme.spacing.md,
    paddingVertical: HoppyTheme.spacing.sm,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray800,
  },
  inputHint: {
    fontSize: HoppyTheme.fontSizes.xs,
    color: HoppyColors.gray500,
    marginTop: HoppyTheme.spacing.xs,
  },
  modalFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: HoppyTheme.spacing.sm,
    padding: HoppyTheme.spacing.lg,
    borderTopWidth: 1,
    borderTopColor: HoppyColors.gray200,
  },
  modalButton: {
    flex: 1,
    paddingVertical: HoppyTheme.spacing.sm,
    borderRadius: HoppyTheme.borderRadius.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cancelButton: {
    backgroundColor: HoppyColors.gray200,
  },
  generateButton: {
    backgroundColor: HoppyColors.primary,
  },
  cancelButtonText: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: '600',
    color: HoppyColors.gray700,
  },
  generateButtonText: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: '600',
    color: HoppyColors.white,
  },
});
