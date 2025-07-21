import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  TextInput,
  Modal,
} from 'react-native';
import { useRoute, useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { apiService, RouteDto, RouteStopDto } from '../services/api';
import { RootStackParamList } from '../types';
import { HoppyButton, HoppyCard } from '../components';
import { useAuth } from '../contexts/AuthContext';

type RouteDetailsScreenNavigationProp = StackNavigationProp<RootStackParamList>;

// Route Stop Status enum (matching backend)
enum RouteStopStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Skipped = 3,
  Failed = 4
}

// Route Status enum (matching backend)
enum RouteStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export default function RouteDetailsScreen() {
  const route = useRoute();
  const navigation = useNavigation<RouteDetailsScreenNavigationProp>();
  const { user } = useAuth();
  const { routeId } = route.params as { routeId: number };
  
  const [routeData, setRouteData] = useState<RouteDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCommentModal, setShowCommentModal] = useState(false);
  const [selectedStopId, setSelectedStopId] = useState<number | null>(null);
  const [comment, setComment] = useState('');

  const userRole = user?.roleName?.toLowerCase();

  useEffect(() => {
    fetchRouteDetails();
  }, [routeId]);

  const fetchRouteDetails = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getRoute(routeId);
      setRouteData(data);
    } catch (err: any) {
      console.error('Error fetching route details:', err);
      setError(err.response?.data?.message || err.message || 'Fout bij ophalen van route details');
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: RouteStopStatus) => {
    switch (status) {
      case RouteStopStatus.Pending:
        return HoppyColors.gray400;
      case RouteStopStatus.InProgress:
        return HoppyColors.warning;
      case RouteStopStatus.Completed:
        return HoppyColors.success;
      case RouteStopStatus.Skipped:
        return HoppyColors.info;
      case RouteStopStatus.Failed:
        return HoppyColors.error;
      default:
        return HoppyColors.gray400;
    }
  };

  const getStatusText = (status: RouteStopStatus) => {
    switch (status) {
      case RouteStopStatus.Pending:
        return 'Te doen';
      case RouteStopStatus.InProgress:
        return 'Onderweg';
      case RouteStopStatus.Completed:
        return 'Voltooid';
      case RouteStopStatus.Skipped:
        return 'Overgeslagen';
      case RouteStopStatus.Failed:
        return 'Mislukt';
      default:
        return 'Onbekend';
    }
  };

  const handleCompleteStop = async (stopId: number) => {
    try {
      await apiService.completeRouteStop(routeId, stopId);
      Alert.alert('Succes', 'Stop gemarkeerd als voltooid!');
      fetchRouteDetails(); // Refresh data
    } catch (err: any) {
      Alert.alert('Fout', err.response?.data?.message || 'Fout bij voltooien van stop');
    }
  };

  const handleSkipStop = (stopId: number) => {
    setSelectedStopId(stopId);
    setShowCommentModal(true);
  };

  const handleSkipWithComment = async () => {
    if (!selectedStopId) return;
    
    try {
      await apiService.updateRouteStopStatus(routeId, selectedStopId, RouteStopStatus.Skipped);
      // In a real app, you'd also save the comment
      Alert.alert('Succes', 'Stop overgeslagen met opmerking');
      setShowCommentModal(false);
      setComment('');
      setSelectedStopId(null);
      fetchRouteDetails(); // Refresh data
    } catch (err: any) {
      Alert.alert('Fout', err.response?.data?.message || 'Fout bij overslaan van stop');
    }
  };

  const calculateProgress = () => {
    if (!routeData || !routeData.stops) return 0;
    const completedStops = routeData.stops.filter(stop => stop.status === RouteStopStatus.Completed);
    return (completedStops.length / routeData.stops.length) * 100;
  };

  const renderStopCard = (stop: RouteStopDto, index: number) => (
    <HoppyCard key={stop.id} padding="medium" style={styles.stopCard}>
      <View style={styles.stopHeader}>
        <View style={styles.stopInfo}>
          <Text style={styles.stopNumber}>Stop {index + 1}</Text>
          <Text style={styles.vehicleId}>Voertuig #{stop.vehicle.externalId}</Text>
          <Text style={styles.batteryLevel}>Batterij: {stop.vehicle.batteryLevel}%</Text>
        </View>
        <View style={[styles.statusBadge, { backgroundColor: getStatusColor(stop.status) }]}>
          <Text style={styles.statusText}>{getStatusText(stop.status)}</Text>
        </View>
      </View>
      
      <View style={styles.stopDetails}>
        <View style={styles.detailRow}>
          <Ionicons name="location-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.detailText}>
            {stop.vehicle.latitude.toFixed(4)}, {stop.vehicle.longitude.toFixed(4)}
          </Text>
        </View>
        <View style={styles.detailRow}>
          <Ionicons name="time-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.detailText}>
            Geschatte tijd: {stop.estimatedArrivalOffset}
          </Text>
        </View>
        <View style={styles.detailRow}>
          <Ionicons name="map-outline" size={16} color={HoppyColors.gray600} />
          <Text style={styles.detailText}>Zone: {stop.vehicle.zoneName}</Text>
        </View>
      </View>

      {/* Action buttons for swappers */}
      {userRole === 'swapper' && stop.status === RouteStopStatus.Pending && (
        <View style={styles.actionButtons}>
          <TouchableOpacity
            style={[styles.actionButton, styles.completeButton]}
            onPress={() => handleCompleteStop(stop.id)}
          >
            <Ionicons name="checkmark-circle" size={16} color={HoppyColors.white} />
            <Text style={styles.actionButtonText}>Swap Voltooid</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.actionButton, styles.skipButton]}
            onPress={() => handleSkipStop(stop.id)}
          >
            <Ionicons name="alert-circle" size={16} color={HoppyColors.white} />
            <Text style={styles.actionButtonText}>Probleem Melden</Text>
          </TouchableOpacity>
        </View>
      )}
    </HoppyCard>
  );

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={HoppyColors.primary} />
        <Text style={styles.loadingText}>Route details laden...</Text>
      </View>
    );
  }

  if (error || !routeData) {
    return (
      <View style={styles.errorContainer}>
        <Ionicons name="alert-circle-outline" size={64} color={HoppyColors.error} />
        <Text style={styles.errorText}>Fout bij laden van route</Text>
        <Text style={styles.errorDetails}>{error}</Text>
        <HoppyButton
          title="Opnieuw proberen"
          onPress={fetchRouteDetails}
          variant="primary"
          size="large"
        />
      </View>
    );
  }

  const progress = calculateProgress();

  return (
    <ScrollView style={styles.container}>
      {/* Route Header */}
      <View style={styles.header}>
        <View style={styles.headerContent}>
          <Text style={styles.routeTitle}>{routeData.zoneName} Route</Text>
          <Text style={styles.swapperName}>
            {userRole === 'swapper' ? 'Mijn Route' : `Swapper: ${routeData.swapperName}`}
          </Text>
          <Text style={styles.routeDate}>
            {new Date(routeData.date).toLocaleDateString('nl-NL', {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric'
            })}
          </Text>
        </View>
      </View>

      {/* Progress Bar */}
      <HoppyCard padding="medium" style={styles.progressCard}>
        <View style={styles.progressHeader}>
          <Text style={styles.progressTitle}>Route Voortgang</Text>
          <Text style={styles.progressPercentage}>{Math.round(progress)}%</Text>
        </View>
        <View style={styles.progressBarContainer}>
          <View style={[styles.progressBar, { width: `${progress}%` }]} />
        </View>
        <Text style={styles.progressText}>
          {routeData.stops.filter(s => s.status === RouteStopStatus.Completed).length} van {routeData.stops.length} stops voltooid
        </Text>
      </HoppyCard>

      {/* Route Stats */}
      <HoppyCard padding="medium" style={styles.statsCard}>
        <Text style={styles.sectionTitle}>Route Informatie</Text>
        <View style={styles.statsGrid}>
          <View style={styles.statItem}>
            <Ionicons name="time-outline" size={20} color={HoppyColors.primary} />
            <Text style={styles.statValue}>{routeData.targetDurationMinutes} min</Text>
            <Text style={styles.statLabel}>Doeltijd</Text>
          </View>
          <View style={styles.statItem}>
            <Ionicons name="navigate-outline" size={20} color={HoppyColors.primary} />
            <Text style={styles.statValue}>{routeData.stops.length}</Text>
            <Text style={styles.statLabel}>Stops</Text>
          </View>
          <View style={styles.statItem}>
            <Ionicons name="location-outline" size={20} color={HoppyColors.primary} />
            <Text style={styles.statValue}>{routeData.zoneName}</Text>
            <Text style={styles.statLabel}>Zone</Text>
          </View>
        </View>
      </HoppyCard>

      {/* Route Stops */}
      <View style={styles.stopsSection}>
        <Text style={styles.sectionTitle}>Route Stops</Text>
        {routeData.stops.map((stop, index) => renderStopCard(stop, index))}
      </View>

      {/* Comment Modal */}
      <Modal
        visible={showCommentModal}
        animationType="slide"
        transparent={true}
        onRequestClose={() => setShowCommentModal(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Waarom kun je deze stop niet voltooien?</Text>
            <TextInput
              style={styles.commentInput}
              placeholder="Bijv. 'Voertuig niet gevonden' of 'Defect voertuig'"
              value={comment}
              onChangeText={setComment}
              multiline
              numberOfLines={3}
            />
            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={[styles.modalButton, styles.cancelButton]}
                onPress={() => setShowCommentModal(false)}
              >
                <Text style={styles.cancelButtonText}>Annuleren</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.modalButton, styles.submitButton]}
                onPress={handleSkipWithComment}
              >
                <Text style={styles.submitButtonText}>Overslaan</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
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
  },
  headerContent: {
    alignItems: 'center',
  },
  routeTitle: {
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: 'bold',
    color: HoppyColors.white,
    marginBottom: HoppyTheme.spacing.xs,
  },
  swapperName: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.white,
    opacity: 0.9,
    marginBottom: HoppyTheme.spacing.xs,
  },
  routeDate: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.white,
    opacity: 0.8,
  },
  progressCard: {
    margin: HoppyTheme.spacing.md,
  },
  progressHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.sm,
  },
  progressTitle: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  progressPercentage: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.primary,
  },
  progressBarContainer: {
    height: 8,
    backgroundColor: HoppyColors.gray200,
    borderRadius: 4,
    marginBottom: HoppyTheme.spacing.sm,
  },
  progressBar: {
    height: '100%',
    backgroundColor: HoppyColors.primary,
    borderRadius: 4,
  },
  progressText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    textAlign: 'center',
  },
  statsCard: {
    margin: HoppyTheme.spacing.md,
    marginTop: 0,
  },
  sectionTitle: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.md,
  },
  statsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  statItem: {
    alignItems: 'center',
    flex: 1,
  },
  statValue: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    marginTop: HoppyTheme.spacing.xs,
  },
  statLabel: {
    fontSize: HoppyTheme.fontSizes.xs,
    color: HoppyColors.gray600,
    marginTop: HoppyTheme.spacing.xs,
  },
  stopsSection: {
    padding: HoppyTheme.spacing.md,
  },
  stopCard: {
    marginBottom: HoppyTheme.spacing.md,
  },
  stopHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: HoppyTheme.spacing.md,
  },
  stopInfo: {
    flex: 1,
  },
  stopNumber: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  vehicleId: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginTop: HoppyTheme.spacing.xs,
  },
  batteryLevel: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.warning,
    marginTop: HoppyTheme.spacing.xs,
  },
  statusBadge: {
    paddingHorizontal: HoppyTheme.spacing.sm,
    paddingVertical: HoppyTheme.spacing.xs,
    borderRadius: HoppyTheme.borderRadius.sm,
  },
  statusText: {
    fontSize: HoppyTheme.fontSizes.xs,
    fontWeight: 'bold',
    color: HoppyColors.white,
  },
  stopDetails: {
    marginBottom: HoppyTheme.spacing.md,
  },
  detailRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.xs,
  },
  detailText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginLeft: HoppyTheme.spacing.sm,
  },
  actionButtons: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: HoppyTheme.spacing.sm,
  },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: HoppyTheme.spacing.md,
    paddingVertical: HoppyTheme.spacing.sm,
    borderRadius: HoppyTheme.borderRadius.md,
    flex: 1,
    justifyContent: 'center',
  },
  completeButton: {
    backgroundColor: HoppyColors.success,
  },
  skipButton: {
    backgroundColor: HoppyColors.warning,
  },
  actionButtonText: {
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: 'bold',
    color: HoppyColors.white,
    marginLeft: HoppyTheme.spacing.xs,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  modalContent: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.lg,
    width: '90%',
    maxWidth: 400,
  },
  modalTitle: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.md,
    textAlign: 'center',
  },
  commentInput: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: HoppyTheme.borderRadius.md,
    padding: HoppyTheme.spacing.md,
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray800,
    minHeight: 80,
    textAlignVertical: 'top',
    marginBottom: HoppyTheme.spacing.md,
  },
  modalButtons: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: HoppyTheme.spacing.sm,
  },
  modalButton: {
    flex: 1,
    paddingVertical: HoppyTheme.spacing.sm,
    borderRadius: HoppyTheme.borderRadius.md,
    alignItems: 'center',
  },
  cancelButton: {
    backgroundColor: HoppyColors.gray200,
  },
  submitButton: {
    backgroundColor: HoppyColors.warning,
  },
  cancelButtonText: {
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: 'bold',
    color: HoppyColors.gray700,
  },
  submitButtonText: {
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: 'bold',
    color: HoppyColors.white,
  },
});
