import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  ScrollView,
  Modal,
  TextInput,
  FlatList,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { apiService, RouteDto, RouteStopDto, CountryDto, RegionDto, ZoneDto, RouteGenerationRequest, UserDto } from '../services/api';
import { HoppyButton } from '../components';
import { useAuth } from '../contexts/AuthContext';

// Route Approval Status enum
enum RouteApprovalStatus {
  PendingApproval = 0,
  Approved = 1,
  Rejected = 2,
  AutoApproved = 3
}

// Route Type definitions
const RouteTypes = {
  battery: {
    key: 'battery',
    label: 'Batterij Vervangen',
    description: 'Vervang lege batterijen in voertuigen',
    icon: 'battery-charging',
    color: '#FF6B35'
  },
  rebalancing: {
    key: 'rebalancing',
    label: 'Rebalancing',
    description: 'Verdeel voertuigen over zones',
    icon: 'shuffle',
    color: '#4ECDC4'
  },
  misparked: {
    key: 'misparked',
    label: 'Verkeerd Geparkeerd',
    description: 'Verplaats verkeerd geparkeerde voertuigen',
    icon: 'location',
    color: '#45B7D1'
  },
  maintenance: {
    key: 'maintenance',
    label: 'Onderhoud',
    description: 'Verzamel voertuigen voor onderhoud',
    icon: 'construct',
    color: '#F39C12'
  },
  collection: {
    key: 'collection',
    label: 'Ophalen',
    description: 'Verzamel defecte voertuigen',
    icon: 'car',
    color: '#E74C3C'
  }
};

export default function RouteGenerationScreen() {
  const navigation = useNavigation();
  const { user } = useAuth();

  // Step tracking
  const [currentStep, setCurrentStep] = useState(1); // 1: Country, 2: Zone, 3: Parameters, 4: Preview

  // Data states
  const [countries, setCountries] = useState<CountryDto[]>([]);
  const [regions, setRegions] = useState<RegionDto[]>([]);
  const [zones, setZones] = useState<ZoneDto[]>([]);
  const [swappers, setSwappers] = useState<UserDto[]>([]);
  const [routePreview, setRoutePreview] = useState<RouteDto | null>(null);

  // Selection states
  const [selectedCountry, setSelectedCountry] = useState<CountryDto | null>(null);
  const [selectedRegion, setSelectedRegion] = useState<RegionDto | null>(null);
  const [selectedZone, setSelectedZone] = useState<ZoneDto | null>(null);
  const [selectedSwapper, setSelectedSwapper] = useState<UserDto | null>(null);

  // Route parameters
  const [routeDuration, setRouteDuration] = useState<string>('4'); // 4 hours
  const [batteryThreshold, setBatteryThreshold] = useState<string>('25');
  const [routeType, setRouteType] = useState<string>('battery'); // Default to battery replacement

  // UI states
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [showStopInstructionsModal, setShowStopInstructionsModal] = useState(false);
  const [selectedStop, setSelectedStop] = useState<RouteStopDto | null>(null);
  const [stopInstructions, setStopInstructions] = useState('');
  const [batterySwapNotes, setBatterySwapNotes] = useState('');

  useEffect(() => {
    loadCountries();
    loadSwappers();
  }, []);

  const loadCountries = async () => {
    try {
      setLoading(true);
      const countriesData = await apiService.getCountries();
      setCountries(countriesData);
    } catch (error) {
      Alert.alert('Fout', 'Kon landen niet laden');
    } finally {
      setLoading(false);
    }
  };

  const loadSwappers = async (forZoneId?: number) => {
    try {
      const swappersData = await apiService.getSwappers(forZoneId);
      setSwappers(swappersData);
      
      // Auto-select first swapper if available
      if (swappersData.length > 0) {
        setSelectedSwapper(swappersData[0]);
      } else {
        setSelectedSwapper(null);
      }
    } catch (error) {
      console.error('Error loading swappers:', error);
      Alert.alert('Fout', 'Kon swappers niet laden');
    }
  };

  const handleCountrySelect = async (country: CountryDto) => {
    setSelectedCountry(country);
    setSelectedRegion(null);
    setSelectedZone(null);
    setRoutePreview(null);
    
    try {
      setLoading(true);
      const regionsData = await apiService.getRegionsByCountry(country.countryCode);
      setRegions(regionsData);
      
      // Collect all zones from all regions in this country
      const allZones: ZoneDto[] = [];
      regionsData.forEach(region => {
        allZones.push(...region.zones);
      });
      setZones(allZones);
      setCurrentStep(2); // Go directly to zone selection
    } catch (error) {
      Alert.alert('Fout', 'Kon zones niet laden');
    } finally {
      setLoading(false);
    }
  };

  const handleRegionSelect = async (region: RegionDto) => {
    setSelectedRegion(region);
    setSelectedZone(null);
    setRoutePreview(null);
    
    console.log('Selected region:', region.name, 'Zones count:', region.zones.length);
    setZones(region.zones);
    setCurrentStep(3);
  };

  const handleZoneSelect = async (zone: ZoneDto) => {
    setSelectedZone(zone);
    setRoutePreview(null);
    setCurrentStep(3); // Go to parameters (step 3 instead of 4)
    
    // Load swappers assigned to this zone
    await loadSwappers(zone.id);
  };

  const handleGenerateRoute = async () => {
    if (!selectedZone || !selectedSwapper) {
      Alert.alert('Fout', 'Selecteer eerst een zone en swapper');
      return;
    }

    const durationMinutes = parseInt(routeDuration) * 60;
    const threshold = parseInt(batteryThreshold);

    // Validate ranges
    if (durationMinutes < 60 || durationMinutes > 1440) {
      Alert.alert('Fout', 'Route duur moet tussen 1 en 24 uur zijn');
      return;
    }

    if (threshold < 1 || threshold > 100) {
      Alert.alert('Fout', 'Batterij drempel moet tussen 1% en 100% zijn');
      return;
    }

    try {
      setGenerating(true);
      
      const selectedRouteType = RouteTypes[routeType as keyof typeof RouteTypes];
      
      const request: RouteGenerationRequest = {
        assignedSwapperId: selectedSwapper.id,
        zoneId: selectedZone.id,
        targetDurationMinutes: durationMinutes,
        batteryThreshold: threshold,
        name: `${selectedRouteType.label} - ${selectedZone.name} - ${new Date().toLocaleDateString('nl-NL')}`,
        description: `${selectedRouteType.description} in ${selectedZone.name}`,
        routeType: routeType
      };

      console.log('Sending route generation request:', JSON.stringify(request, null, 2));

      const response = await apiService.generateRoute(request);
      
      if (response.success && response.route) {
        setRoutePreview(response.route);
        setCurrentStep(4); // Preview is step 4 instead of 5
      } else {
        Alert.alert('Fout', response.message || 'Kon route niet genereren');
      }
    } catch (error: any) {
      Alert.alert('Fout', error.message || 'Er is een fout opgetreden bij het genereren van de route');
    } finally {
      setGenerating(false);
    }
  };

  const handleStopInstructionsEdit = (stop: RouteStopDto) => {
    setSelectedStop(stop);
    setStopInstructions(stop.specialInstructions || '');
    setBatterySwapNotes(stop.batterySwapNotes || '');
    setShowStopInstructionsModal(true);
  };

  const handleSaveStopInstructions = async () => {
    if (!selectedStop || !routePreview) return;

    try {
      setLoading(true);
      const updatedStop = await apiService.updateRouteStopInstructions(
        routePreview.id,
        selectedStop.id,
        stopInstructions,
        batterySwapNotes
      );

      // Update local state
      const updatedStops = routePreview.stops.map(stop => 
        stop.id === selectedStop.id ? updatedStop : stop
      );
      setRoutePreview({ ...routePreview, stops: updatedStops });

      setShowStopInstructionsModal(false);
      setSelectedStop(null);
    } catch (error) {
      Alert.alert('Fout', 'Kon instructies niet opslaan');
    } finally {
      setLoading(false);
    }
  };

  const handleFinalizeRoute = async () => {
    if (!routePreview) return;

    Alert.alert(
      'Route Finalizen',
      'Deze route wordt doorgestuurd naar de FleetManager voor goedkeuring. Doorgaan?',
      [
        { text: 'Annuleren', style: 'cancel' },
        { 
          text: 'Doorsturen', 
          onPress: async () => {
            try {
              setLoading(true);
              
              // In real implementation, this would save the route with PendingApproval status
              Alert.alert(
                'Route Doorgestuurd',
                'De route is doorgestuurd naar de FleetManager voor goedkeuring.',
                [
                  { text: 'OK', onPress: () => navigation.goBack() }
                ]
              );
            } catch (error) {
              Alert.alert('Fout', 'Kon route niet doorsturen');
            } finally {
              setLoading(false);
            }
          }
        }
      ]
    );
  };

  const renderStepIndicator = () => (
    <View style={styles.stepIndicator}>
      {[1, 2, 3, 4].map((step) => (
        <View key={step} style={styles.stepContainer}>
          <View style={[
            styles.stepCircle,
            step <= currentStep ? styles.stepCircleActive : styles.stepCircleInactive
          ]}>
            <Text style={[
              styles.stepNumber,
              step <= currentStep ? styles.stepNumberActive : styles.stepNumberInactive
            ]}>
              {step}
            </Text>
          </View>
          {step < 4 && (
            <View style={[
              styles.stepLine,
              step < currentStep ? styles.stepLineActive : styles.stepLineInactive
            ]} />
          )}
        </View>
      ))}
    </View>
  );

  const renderCountrySelection = () => (
    <View style={styles.selectionContainer}>
      <Text style={styles.sectionTitle}>Selecteer Land</Text>
      <FlatList
        data={countries}
        keyExtractor={(item) => item.countryCode}
        scrollEnabled={false}
        nestedScrollEnabled={true}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={styles.selectionItem}
            onPress={() => handleCountrySelect(item)}
          >
            <Text style={styles.selectionItemText}>{item.countryName}</Text>
            <Text style={styles.selectionItemSubtext}>
              {item.regions.length} regio{item.regions.length !== 1 ? '\'s' : ''}
            </Text>
            <Ionicons name="chevron-forward" size={20} color={HoppyColors.gray400} />
          </TouchableOpacity>
        )}
        showsVerticalScrollIndicator={false}
      />
    </View>
  );

  const renderRegionSelection = () => (
    <View style={styles.selectionContainer}>
      <TouchableOpacity
        style={styles.backButton}
        onPress={() => setCurrentStep(1)}
      >
        <Ionicons name="chevron-back" size={20} color={HoppyColors.primary} />
        <Text style={styles.backButtonText}>Terug naar landen</Text>
      </TouchableOpacity>
      
      <Text style={styles.sectionTitle}>
        Selecteer Regio in {selectedCountry?.countryName}
      </Text>
      <FlatList
        data={regions}
        keyExtractor={(item) => item.id.toString()}
        scrollEnabled={false}
        nestedScrollEnabled={true}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={styles.selectionItem}
            onPress={() => handleRegionSelect(item)}
          >
            <Text style={styles.selectionItemText}>{item.name}</Text>
            <Text style={styles.selectionItemSubtext}>
              {item.zones.length} zone{item.zones.length !== 1 ? 's' : ''}
            </Text>
            <Ionicons name="chevron-forward" size={20} color={HoppyColors.gray400} />
          </TouchableOpacity>
        )}
        showsVerticalScrollIndicator={false}
      />
    </View>
  );

  const renderZoneSelection = () => (
    <View style={styles.selectionContainer}>
      <TouchableOpacity
        style={styles.backButton}
        onPress={() => setCurrentStep(1)}
      >
        <Ionicons name="chevron-back" size={20} color={HoppyColors.primary} />
        <Text style={styles.backButtonText}>Terug naar landen</Text>
      </TouchableOpacity>
      
      <Text style={styles.sectionTitle}>
        Selecteer Zone in {selectedCountry?.countryName}
      </Text>
      {zones.length === 0 ? (
        <View style={styles.emptyStateContainer}>
          <Ionicons name="location-outline" size={48} color={HoppyColors.gray400} />
          <Text style={styles.emptyStateText}>Geen zones beschikbaar</Text>
          <Text style={styles.emptyStateSubtext}>
            Er zijn geen zones beschikbaar in {selectedCountry?.countryName}
          </Text>
        </View>
      ) : (
        <FlatList
          data={zones}
          keyExtractor={(item) => item.id.toString()}
          scrollEnabled={false}
          nestedScrollEnabled={true}
          renderItem={({ item }) => (
            <TouchableOpacity
              style={styles.selectionItem}
              onPress={() => handleZoneSelect(item)}
            >
              <Text style={styles.selectionItemText}>{item.name}</Text>
              <Text style={styles.selectionItemSubtext}>{item.regionName}</Text>
              <Ionicons name="chevron-forward" size={20} color={HoppyColors.gray400} />
            </TouchableOpacity>
          )}
          showsVerticalScrollIndicator={false}
        />
      )}
    </View>
  );

  const renderParameterInput = () => (
    <View style={styles.parameterContainer}>
      <TouchableOpacity
        style={styles.backButton}
        onPress={() => setCurrentStep(2)}
      >
        <Ionicons name="chevron-back" size={20} color={HoppyColors.primary} />
        <Text style={styles.backButtonText}>Terug naar zones</Text>
      </TouchableOpacity>

      <Text style={styles.sectionTitle}>Route Parameters</Text>
      <Text style={styles.selectedInfo}>
        Zone: {selectedZone?.name} ({selectedCountry?.countryName})
      </Text>

      <View style={styles.inputGroup}>
        <Text style={styles.inputLabel}>Toegewezen Swapper</Text>
        <View style={styles.buttonRow}>
          {swappers.map((swapper) => (
            <TouchableOpacity
              key={swapper.id}
              style={[
                styles.parameterButton,
                selectedSwapper?.id === swapper.id && styles.parameterButtonActive
              ]}
              onPress={() => setSelectedSwapper(swapper)}
            >
              <Text style={[
                styles.parameterButtonText,
                selectedSwapper?.id === swapper.id && styles.parameterButtonTextActive
              ]}>
                {swapper.firstName} {swapper.lastName}
              </Text>
            </TouchableOpacity>
          ))}
        </View>
        {swappers.length === 0 && (
          <Text style={styles.noDataText}>Geen swappers beschikbaar</Text>
        )}
      </View>

      <View style={styles.inputGroup}>
        <Text style={styles.inputLabel}>Route Type</Text>
        <View style={styles.routeTypeContainer}>
          {Object.values(RouteTypes).map((type) => (
            <TouchableOpacity
              key={type.key}
              style={[
                styles.routeTypeCard,
                routeType === type.key && styles.routeTypeCardActive
              ]}
              onPress={() => setRouteType(type.key)}
            >
              <View style={styles.routeTypeIconContainer}>
                <Ionicons 
                  name={type.icon as any} 
                  size={24} 
                  color={routeType === type.key ? HoppyColors.white : type.color} 
                />
              </View>
              <Text style={[
                styles.routeTypeLabel,
                routeType === type.key && styles.routeTypeLabelActive
              ]}>
                {type.label}
              </Text>
              <Text style={[
                styles.routeTypeDescription,
                routeType === type.key && styles.routeTypeDescriptionActive
              ]}>
                {type.description}
              </Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>

      <View style={styles.inputGroup}>
        <Text style={styles.inputLabel}>Route Duur (uren)</Text>
        <View style={styles.buttonRow}>
          {[2, 3, 4, 6, 8].map((hours) => (
            <TouchableOpacity
              key={hours}
              style={[
                styles.parameterButton,
                routeDuration === hours.toString() && styles.parameterButtonActive
              ]}
              onPress={() => setRouteDuration(hours.toString())}
            >
              <Text style={[
                styles.parameterButtonText,
                routeDuration === hours.toString() && styles.parameterButtonTextActive
              ]}>
                {hours}u
              </Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>

      <View style={styles.inputGroup}>
        <Text style={styles.inputLabel}>Batterij Drempel (%)</Text>
        <View style={styles.buttonRow}>
          {[10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100].map((percentage) => (
            <TouchableOpacity
              key={percentage}
              style={[
                styles.parameterButton,
                batteryThreshold === percentage.toString() && styles.parameterButtonActive
              ]}
              onPress={() => setBatteryThreshold(percentage.toString())}
            >
              <Text style={[
                styles.parameterButtonText,
                batteryThreshold === percentage.toString() && styles.parameterButtonTextActive
              ]}>
                {percentage}%
              </Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>

      <HoppyButton
        title={generating ? "Route Genereren..." : "Route Genereren"}
        onPress={handleGenerateRoute}
        loading={generating}
        style={styles.generateButton}
      />
    </View>
  );

  const renderRoutePreview = () => {
    if (!routePreview) return null;

    return (
      <View style={styles.previewContainer}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => setCurrentStep(4)}
        >
          <Ionicons name="chevron-back" size={20} color={HoppyColors.primary} />
          <Text style={styles.backButtonText}>Wijzig parameters</Text>
        </TouchableOpacity>

        <Text style={styles.sectionTitle}>Route Preview</Text>
        
        <View style={styles.routeInfo}>
          <Text style={styles.routeInfoTitle}>Route Informatie</Text>
          <Text style={styles.routeInfoText}>Zone: {routePreview.zoneName}</Text>
          <Text style={styles.routeInfoText}>Duur: {Math.floor(routePreview.targetDurationMinutes / 60)}u {routePreview.targetDurationMinutes % 60}m</Text>
          <Text style={styles.routeInfoText}>Aantal stops: {routePreview.stops.length}</Text>
          <Text style={styles.routeInfoText}>Status: Wacht op goedkeuring</Text>
        </View>

        <Text style={styles.stopsTitle}>Route Stops</Text>
        <FlatList
          data={routePreview.stops}
          keyExtractor={(item) => item.id.toString()}
          scrollEnabled={false}
          nestedScrollEnabled={true}
          renderItem={({ item, index }) => (
            <View style={styles.stopItem}>
              <View style={styles.stopHeader}>
                <Text style={styles.stopNumber}>Stop {index + 1}</Text>
                <TouchableOpacity
                  style={styles.editButton}
                  onPress={() => handleStopInstructionsEdit(item)}
                >
                  <Ionicons name="create-outline" size={16} color={HoppyColors.primary} />
                  <Text style={styles.editButtonText}>Bewerk</Text>
                </TouchableOpacity>
              </View>
              
              <Text style={styles.stopVehicle}>
                Voertuig: {item.vehicle.registrationNumber} ({item.vehicle.vehicleType})
              </Text>
              <Text style={styles.stopBattery}>
                Batterij: {item.vehicle.batteryLevel}%
              </Text>
              
              {item.specialInstructions && (
                <Text style={styles.stopInstructions}>
                  Instructies: {item.specialInstructions}
                </Text>
              )}
              
              {item.batterySwapNotes && (
                <Text style={styles.stopNotes}>
                  Batterij opmerkingen: {item.batterySwapNotes}
                </Text>
              )}
            </View>
          )}
          showsVerticalScrollIndicator={false}
        />

        <HoppyButton
          title="Route Doorsturen voor Goedkeuring"
          onPress={handleFinalizeRoute}
          loading={loading}
          style={styles.finalizeButton}
        />
      </View>
    );
  };

  if (loading && countries.length === 0) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={HoppyColors.primary} />
        <Text style={styles.loadingText}>Landen laden...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.headerBackButton}
          onPress={() => navigation.goBack()}
        >
          <Ionicons name="chevron-back" size={24} color={HoppyColors.primary} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Route Genereren</Text>
      </View>

      {renderStepIndicator()}

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        {currentStep === 1 && renderCountrySelection()}
        {currentStep === 2 && renderZoneSelection()}
        {currentStep === 3 && renderParameterInput()}
        {currentStep === 4 && renderRoutePreview()}
      </ScrollView>

      {/* Stop Instructions Modal */}
      <Modal
        visible={showStopInstructionsModal}
        transparent={true}
        animationType="slide"
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Stop Instructies</Text>
              <TouchableOpacity
                onPress={() => setShowStopInstructionsModal(false)}
              >
                <Ionicons name="close" size={24} color={HoppyColors.gray600} />
              </TouchableOpacity>
            </View>

            <Text style={styles.modalVehicleInfo}>
              Voertuig: {selectedStop?.vehicle.registrationNumber} ({selectedStop?.vehicle.batteryLevel}%)
            </Text>

            <View style={styles.modalInputGroup}>
              <Text style={styles.modalInputLabel}>Speciale Instructies</Text>
              <TextInput
                style={styles.modalTextInput}
                value={stopInstructions}
                onChangeText={setStopInstructions}
                placeholder="Bijv: Voertuig staat in parkeervak 15"
                multiline
                numberOfLines={3}
              />
            </View>

            <View style={styles.modalInputGroup}>
              <Text style={styles.modalInputLabel}>Batterij Wissel Opmerkingen</Text>
              <TextInput
                style={styles.modalTextInput}
                value={batterySwapNotes}
                onChangeText={setBatterySwapNotes}
                placeholder="Bijv: Batterij is zwaar beschadigd, extra voorzichtigheid"
                multiline
                numberOfLines={3}
              />
            </View>

            <View style={styles.modalActions}>
              <TouchableOpacity
                style={styles.modalCancelButton}
                onPress={() => setShowStopInstructionsModal(false)}
              >
                <Text style={styles.modalCancelButtonText}>Annuleren</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={styles.modalSaveButton}
                onPress={handleSaveStopInstructions}
              >
                <Text style={styles.modalSaveButtonText}>Opslaan</Text>
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
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingTop: 60,
    paddingBottom: 20,
    backgroundColor: HoppyColors.white,
    borderBottomWidth: 1,
    borderBottomColor: HoppyColors.gray200,
  },
  headerBackButton: {
    marginRight: 16,
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: HoppyColors.gray900,
  },
  stepIndicator: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 20,
    backgroundColor: HoppyColors.white,
  },
  stepContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  stepCircle: {
    width: 32,
    height: 32,
    borderRadius: 16,
    justifyContent: 'center',
    alignItems: 'center',
  },
  stepCircleActive: {
    backgroundColor: HoppyColors.primary,
  },
  stepCircleInactive: {
    backgroundColor: HoppyColors.gray300,
  },
  stepNumber: {
    fontSize: 14,
    fontWeight: '600',
  },
  stepNumberActive: {
    color: HoppyColors.white,
  },
  stepNumberInactive: {
    color: HoppyColors.gray600,
  },
  stepLine: {
    width: 40,
    height: 2,
  },
  stepLineActive: {
    backgroundColor: HoppyColors.primary,
  },
  stepLineInactive: {
    backgroundColor: HoppyColors.gray300,
  },
  content: {
    flex: 1,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HoppyColors.gray50,
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: HoppyColors.gray600,
  },
  selectionContainer: {
    padding: 16,
  },
  sectionTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: HoppyColors.gray900,
    marginBottom: 20,
  },
  backButton: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  backButtonText: {
    fontSize: 16,
    color: HoppyColors.primary,
    marginLeft: 4,
  },
  selectionItem: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    backgroundColor: HoppyColors.white,
    borderRadius: 12,
    marginBottom: 8,
    ...HoppyTheme.shadows.sm,
  },
  selectionItemText: {
    flex: 1,
    fontSize: 16,
    fontWeight: '600',
    color: HoppyColors.gray900,
  },
  selectionItemSubtext: {
    fontSize: 14,
    color: HoppyColors.gray600,
    marginRight: 8,
  },
  parameterContainer: {
    padding: 16,
  },
  selectedInfo: {
    fontSize: 14,
    color: HoppyColors.gray600,
    marginBottom: 24,
    padding: 12,
    backgroundColor: HoppyColors.gray50,
    borderRadius: 8,
  },
  inputGroup: {
    marginBottom: 20,
  },
  inputLabel: {
    fontSize: 16,
    fontWeight: '600',
    color: HoppyColors.gray900,
    marginBottom: 8,
  },
  textInput: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    backgroundColor: HoppyColors.white,
  },
  generateButton: {
    marginTop: 20,
  },
  previewContainer: {
    padding: 16,
  },
  routeInfo: {
    backgroundColor: HoppyColors.white,
    borderRadius: 12,
    padding: 16,
    marginBottom: 20,
    ...HoppyTheme.shadows.sm,
  },
  routeInfoTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: HoppyColors.gray900,
    marginBottom: 12,
  },
  routeInfoText: {
    fontSize: 14,
    color: HoppyColors.gray600,
    marginBottom: 4,
  },
  stopsTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: HoppyColors.gray900,
    marginBottom: 12,
  },
  stopItem: {
    backgroundColor: HoppyColors.white,
    borderRadius: 12,
    padding: 16,
    marginBottom: 8,
    ...HoppyTheme.shadows.sm,
  },
  stopHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  stopNumber: {
    fontSize: 16,
    fontWeight: '600',
    color: HoppyColors.gray900,
  },
  editButton: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 4,
  },
  editButtonText: {
    fontSize: 14,
    color: HoppyColors.primary,
    marginLeft: 4,
  },
  stopVehicle: {
    fontSize: 14,
    color: HoppyColors.gray700,
    marginBottom: 4,
  },
  stopBattery: {
    fontSize: 14,
    color: HoppyColors.gray600,
    marginBottom: 4,
  },
  stopInstructions: {
    fontSize: 14,
    color: HoppyColors.primary,
    fontStyle: 'italic',
    marginTop: 8,
  },
  stopNotes: {
    fontSize: 14,
    color: HoppyColors.warning,
    fontStyle: 'italic',
    marginTop: 4,
  },
  finalizeButton: {
    marginTop: 20,
    marginBottom: 40,
  },
  // Modal styles
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  modalContent: {
    backgroundColor: HoppyColors.white,
    borderRadius: 16,
    padding: 20,
    width: '100%',
    maxWidth: 400,
    maxHeight: '80%',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: HoppyColors.gray900,
  },
  modalVehicleInfo: {
    fontSize: 14,
    color: HoppyColors.gray600,
    marginBottom: 20,
    padding: 12,
    backgroundColor: HoppyColors.gray50,
    borderRadius: 8,
  },
  modalInputGroup: {
    marginBottom: 16,
  },
  modalInputLabel: {
    fontSize: 16,
    fontWeight: '600',
    color: HoppyColors.gray900,
    marginBottom: 8,
  },
  modalTextInput: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    backgroundColor: HoppyColors.white,
    minHeight: 80,
    textAlignVertical: 'top',
  },
  modalActions: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 20,
  },
  modalCancelButton: {
    flex: 1,
    padding: 12,
    borderRadius: 8,
    backgroundColor: HoppyColors.gray200,
    marginRight: 8,
    alignItems: 'center',
  },
  modalCancelButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: HoppyColors.gray700,
  },
  modalSaveButton: {
    flex: 1,
    padding: 12,
    borderRadius: 8,
    backgroundColor: HoppyColors.primary,
    marginLeft: 8,
    alignItems: 'center',
  },
  modalSaveButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: HoppyColors.white,
  },
  emptyStateContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 60,
  },
  emptyStateText: {
    fontSize: 18,
    fontWeight: '600',
    color: HoppyColors.gray600,
    marginTop: 16,
    textAlign: 'center',
  },
  emptyStateSubtext: {
    fontSize: 14,
    color: HoppyColors.gray400,
    marginTop: 8,
    textAlign: 'center',
    paddingHorizontal: 20,
  },
  buttonRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    marginTop: 8,
  },
  parameterButton: {
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    backgroundColor: HoppyColors.white,
    minWidth: 60,
    alignItems: 'center',
  },
  parameterButtonActive: {
    backgroundColor: HoppyColors.primary,
    borderColor: HoppyColors.primary,
  },
  parameterButtonText: {
    fontSize: 14,
    fontWeight: '500',
    color: HoppyColors.gray700,
  },
  parameterButtonTextActive: {
    color: HoppyColors.white,
  },
  routeTypeContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
    marginTop: 12,
  },
  routeTypeCard: {
    flex: 1,
    minWidth: 150,
    padding: 16,
    borderRadius: 12,
    borderWidth: 2,
    borderColor: HoppyColors.gray200,
    backgroundColor: HoppyColors.white,
    alignItems: 'center',
    marginBottom: 8,
  },
  routeTypeCardActive: {
    borderColor: HoppyColors.primary,
    backgroundColor: HoppyColors.primary,
  },
  routeTypeIconContainer: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: HoppyColors.gray100,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 8,
  },
  routeTypeLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: HoppyColors.gray800,
    textAlign: 'center',
    marginBottom: 4,
  },
  routeTypeLabelActive: {
    color: HoppyColors.white,
  },
  routeTypeDescription: {
    fontSize: 12,
    color: HoppyColors.gray600,
    textAlign: 'center',
    lineHeight: 16,
  },
  routeTypeDescriptionActive: {
    color: HoppyColors.white,
  },
  noDataText: {
    fontSize: 14,
    color: HoppyColors.gray600,
    textAlign: 'center',
    fontStyle: 'italic',
    marginTop: 8,
  },
});
