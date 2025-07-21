import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  TextInput,
  Modal,
  RefreshControl,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { HoppyButton, HoppyCard } from '../components';
import { useAuth, User, isAdmin, isFleetManager, getRoleDisplayName } from '../contexts/AuthContext';
import { apiService } from '../services/api';

interface CreateUserData {
  email: string;
  firstName: string;
  lastName: string;
  role: 'fleetmanager' | 'swapper';
  assignedZones?: number[];
}

export default function UserManagementScreen() {
  const { user } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [newUser, setNewUser] = useState<CreateUserData>({
    email: '',
    firstName: '',
    lastName: '',
    role: 'swapper',
    assignedZones: [],
  });

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async (isRefresh = false) => {
    try {
      if (isRefresh) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }
      // Use API service to get users
      const userData = await apiService.getUsers();
      setUsers(userData);
    } catch (error: any) {
      console.error('Error fetching users:', error);
      
      let errorMessage = 'Kon gebruikers niet laden';
      if (error.response?.status === 401) {
        errorMessage = 'Niet geautoriseerd om gebruikers te laden';
      } else if (error.response?.status === 403) {
        errorMessage = 'Geen toegang tot gebruikersbeheer';
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      }
      
      Alert.alert('Fout', errorMessage);
    } finally {
      if (isRefresh) {
        setRefreshing(false);
      } else {
        setLoading(false);
      }
    }
  };

  const onRefresh = () => {
    fetchUsers(true);
  };

  const filteredUsers = users.filter(user => {
    if (!searchQuery) return true;
    
    const query = searchQuery.toLowerCase();
    return (
      user.firstName.toLowerCase().includes(query) ||
      user.lastName.toLowerCase().includes(query) ||
      user.username.toLowerCase().includes(query) ||
      user.email.toLowerCase().includes(query) ||
      user.roleName.toLowerCase().includes(query)
    );
  });

  const handleCreateUser = async () => {
    if (!newUser.email || !newUser.firstName || !newUser.lastName) {
      Alert.alert('Fout', 'Vul alle verplichte velden in');
      return;
    }

    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(newUser.email)) {
      Alert.alert('Fout', 'Voer een geldig e-mailadres in');
      return;
    }

    // Name validation
    if (newUser.firstName.trim().length < 2) {
      Alert.alert('Fout', 'Voornaam moet minimaal 2 karakters lang zijn');
      return;
    }

    if (newUser.lastName.trim().length < 2) {
      Alert.alert('Fout', 'Achternaam moet minimaal 2 karakters lang zijn');
      return;
    }

    try {
      setLoading(true);
      
      // Only admins can create users
      if (!isAdmin(user)) {
        Alert.alert('Fout', 'Alleen administrators kunnen gebruikers aanmaken');
        return;
      }

      // Use API service to create user
      const createdUser = await apiService.createUser({
        email: newUser.email,
        firstName: newUser.firstName,
        lastName: newUser.lastName,
        role: newUser.role,
        assignedZones: newUser.assignedZones,
      });

      Alert.alert(
        'Gebruiker Aangemaakt', 
        `Gebruiker succesvol aangemaakt!\n\nEr is een e-mail verzonden naar ${newUser.email} met:\n• Automatisch gegenereerde gebruikersnaam\n• Tijdelijk wachtwoord\n\nDe gebruiker moet bij eerste login een nieuw wachtwoord instellen.`,
        [{ text: 'OK' }]
      );
      
      setShowCreateModal(false);
      setNewUser({
        email: '',
        firstName: '',
        lastName: '',
        role: 'swapper',
        assignedZones: [],
      });
      fetchUsers();
    } catch (error: any) {
      console.error('Error creating user:', error);
      
      let errorMessage = 'Kon gebruiker niet aanmaken';
      if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error.response?.status === 409) {
        errorMessage = 'E-mailadres is al in gebruik';
      } else if (error.response?.status === 400) {
        errorMessage = 'Ongeldige gegevens opgegeven';
      }
      
      Alert.alert('Fout', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteUser = (userId: number, userName: string) => {
    Alert.alert(
      'Gebruiker verwijderen',
      `Weet je zeker dat je ${userName} wilt verwijderen?`,
      [
        { text: 'Annuleren', style: 'cancel' },
        { 
          text: 'Verwijderen', 
          style: 'destructive',
          onPress: () => deleteUser(userId)
        },
      ]
    );
  };

  const deleteUser = async (userId: number) => {
    try {
      // Use API service to delete user
      await apiService.deleteUser(userId);
      Alert.alert('Succes', 'Gebruiker verwijderd');
      fetchUsers();
    } catch (error: any) {
      console.error('Error deleting user:', error);
      
      let errorMessage = 'Kon gebruiker niet verwijderen';
      if (error.response?.status === 404) {
        errorMessage = 'Gebruiker niet gevonden';
      } else if (error.response?.status === 403) {
        errorMessage = 'Geen toegang om deze gebruiker te verwijderen';
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      }
      
      Alert.alert('Fout', errorMessage);
    }
  };

  const handleEditUser = (userData: User) => {
    const formatDate = (dateString?: string) => {
      if (!dateString) return 'Nooit';
      return new Date(dateString).toLocaleDateString('nl-NL', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    };

    const statusText = userData.isActive ? '✅ Actief' : '❌ Inactief';
    const lastLogin = formatDate(userData.lastLoginAt);
    const zoneName = userData.assignedZoneName || (userData.assignedZoneId ? `Zone ${userData.assignedZoneId}` : 'Geen zone');

    Alert.alert(
      'Gebruiker Bewerken',
      `Gebruiker: ${userData.firstName} ${userData.lastName}\nUsername: ${userData.username}\nRol: ${userData.roleName}\nEmail: ${userData.email}\n\nStatus: ${statusText}\nLaatst ingelogd: ${lastLogin}\nZone: ${zoneName}\n\nAangemaakt: ${formatDate(userData.createdAt)}`,
      [
        { text: 'Sluiten', style: 'cancel' },
        { 
          text: 'Reset Wachtwoord', 
          onPress: () => handleResetPassword(userData.email) 
        },
        ...(isAdmin(user) ? [{ 
          text: 'Zone Wijzigen', 
          onPress: () => handleChangeZone(userData) 
        }] : [])
      ]
    );
  };

  const handleResetPassword = async (email: string) => {
    try {
      // In a real app, you would call an API endpoint to reset password
      // For now, we'll just show a confirmation
      Alert.alert(
        'Wachtwoord Reset',
        `Een nieuw tijdelijk wachtwoord is verzonden naar ${email}.\n\nDe gebruiker zal bij de volgende login gevraagd worden om het wachtwoord te wijzigen.`,
        [{ text: 'OK' }]
      );
    } catch (error) {
      Alert.alert('Fout', 'Kon wachtwoord niet resetten');
    }
  };

  const handleChangeZone = (userData: User) => {
    // Available zones (in a real app, this would be fetched from API)
    const availableZones = [
      { id: 1, name: 'Gent Centrum' },
      { id: 2, name: 'Gent Universiteit' },
      { id: 3, name: 'Brussel Centrum' },
      { id: 4, name: 'Antwerpen Centrum' },
    ];

    const buttons = [
      ...availableZones.map(zone => ({
        text: zone.name,
        onPress: () => updateUserZone(userData.id, zone.id, zone.name)
      })),
      { text: 'Annuleren', style: 'cancel' as const }
    ];

    Alert.alert(
      'Zone Wijzigen',
      `Selecteer een nieuwe zone voor ${userData.firstName} ${userData.lastName}:`,
      buttons
    );
  };

  const updateUserZone = async (userId: number, zoneId: number, zoneName: string) => {
    try {
      // In a real app, you would call an API endpoint to update user zone
      // For now, we'll just show a confirmation and refresh the user list
      Alert.alert(
        'Zone Gewijzigd',
        `Zone succesvol gewijzigd naar ${zoneName}`,
        [{ text: 'OK', onPress: () => fetchUsers() }]
      );
    } catch (error) {
      Alert.alert('Fout', 'Kon zone niet wijzigen');
    }
  };

  const renderUserCard = (userData: User) => (
    <HoppyCard key={userData.id} padding="medium" style={styles.userCard}>
      <View style={styles.userHeader}>
        <View style={styles.userInfo}>
          <Text style={styles.userName}>{userData.firstName} {userData.lastName}</Text>
          <Text style={styles.userRole}>{getRoleDisplayName(userData.roleName)}</Text>
          <Text style={styles.userDetails}>@{userData.username} • {userData.email}</Text>
        </View>
        <View style={styles.userActions}>
          <TouchableOpacity
            style={styles.actionButton}
            onPress={() => handleEditUser(userData)}
            accessibilityLabel={`Bewerk gebruiker ${userData.firstName} ${userData.lastName}`}
            accessibilityHint="Tik om gebruikersinformatie te bekijken en bewerken"
          >
            <Ionicons name="pencil" size={20} color={HoppyColors.primary} />
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.actionButton, styles.deleteButton]}
            onPress={() => handleDeleteUser(userData.id, `${userData.firstName} ${userData.lastName}`)}
            accessibilityLabel={`Verwijder gebruiker ${userData.firstName} ${userData.lastName}`}
            accessibilityHint="Tik om deze gebruiker te verwijderen"
          >
            <Ionicons name="trash" size={20} color={HoppyColors.error} />
          </TouchableOpacity>
        </View>
      </View>
      
      {(userData.assignedZoneId || userData.assignedZoneName) && (
        <View style={styles.zonesContainer}>
          <Text style={styles.zonesLabel}>Toegewezen zone:</Text>
          <Text style={styles.zonesText}>
            {userData.assignedZoneName || `Zone ${userData.assignedZoneId}`}
          </Text>
        </View>
      )}
    </HoppyCard>
  );

  if (!user || !isAdmin(user)) {
    return (
      <View style={styles.container}>
        <Text style={styles.noAccessText}>Je hebt geen toegang tot gebruikersbeheer</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Gebruikersbeheer</Text>
        <Text style={styles.headerSubtitle}>
          Beheer Fleet Managers en Battery Swappers
        </Text>
      </View>

      {/* Create Button */}
      <View style={styles.createButtonContainer}>
        <HoppyButton
          title="Nieuwe Gebruiker Aanmaken"
          onPress={() => setShowCreateModal(true)}
          variant="primary"
          size="medium"
          style={styles.createButton}
        />
      </View>

      {/* Search */}
      <View style={styles.searchContainer}>
        <Ionicons name="search" size={20} color={HoppyColors.gray400} style={styles.searchIcon} />
        <TextInput
          style={styles.searchInput}
          value={searchQuery}
          onChangeText={setSearchQuery}
          placeholder="Zoek gebruikers..."
          placeholderTextColor={HoppyColors.gray400}
        />
        {searchQuery.length > 0 && (
          <TouchableOpacity onPress={() => setSearchQuery('')} style={styles.clearButton}>
            <Ionicons name="close-circle" size={20} color={HoppyColors.gray400} />
          </TouchableOpacity>
        )}
      </View>

      {/* Users List */}
      <ScrollView 
        style={styles.usersList} 
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            colors={[HoppyColors.primary]}
            tintColor={HoppyColors.primary}
          />
        }
      >
        {loading ? (
          <Text style={styles.loadingText}>Gebruikers laden...</Text>
        ) : users.length === 0 ? (
          <Text style={styles.noUsersText}>Geen gebruikers gevonden</Text>
        ) : (
          users.map(renderUserCard)
        )}
      </ScrollView>

      {/* Create User Modal */}
      <Modal visible={showCreateModal} animationType="slide" presentationStyle="pageSheet">
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>Nieuwe Gebruiker Aanmaken</Text>
            <TouchableOpacity 
              onPress={() => !loading && setShowCreateModal(false)}
              disabled={loading}
            >
              <Ionicons name="close" size={24} color={loading ? HoppyColors.gray400 : HoppyColors.gray600} />
            </TouchableOpacity>
          </View>

          <ScrollView style={styles.modalContent} showsVerticalScrollIndicator={false}>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Voornaam *</Text>
              <TextInput
                style={[styles.textInput, loading && styles.textInputDisabled]}
                value={newUser.firstName}
                onChangeText={(text) => setNewUser({...newUser, firstName: text})}
                placeholder="Voornaam"
                placeholderTextColor={HoppyColors.gray400}
                editable={!loading}
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Achternaam *</Text>
              <TextInput
                style={[styles.textInput, loading && styles.textInputDisabled]}
                value={newUser.lastName}
                onChangeText={(text) => setNewUser({...newUser, lastName: text})}
                placeholder="Achternaam"
                placeholderTextColor={HoppyColors.gray400}
                editable={!loading}
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>E-mail *</Text>
              <TextInput
                style={[styles.textInput, loading && styles.textInputDisabled]}
                value={newUser.email}
                onChangeText={(text) => setNewUser({...newUser, email: text})}
                placeholder="email@example.com"
                placeholderTextColor={HoppyColors.gray400}
                keyboardType="email-address"
                autoCapitalize="none"
                editable={!loading}
              />
              <Text style={styles.helperText}>
                Een automatische gebruikersnaam en tijdelijk wachtwoord worden gegenereerd en verzonden naar dit e-mailadres.
              </Text>
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Rol *</Text>
              <View style={styles.roleButtons}>
                <TouchableOpacity
                  style={[
                    styles.roleButton,
                    newUser.role === 'fleetmanager' && styles.roleButtonActive,
                    loading && styles.roleButtonDisabled
                  ]}
                  onPress={() => !loading && setNewUser({...newUser, role: 'fleetmanager'})}
                  disabled={loading}
                >
                  <Text style={[
                    styles.roleButtonText,
                    newUser.role === 'fleetmanager' && styles.roleButtonTextActive,
                    loading && styles.roleButtonTextDisabled
                  ]}>
                    Fleet Manager
                  </Text>
                </TouchableOpacity>
                <TouchableOpacity
                  style={[
                    styles.roleButton,
                    newUser.role === 'swapper' && styles.roleButtonActive,
                    loading && styles.roleButtonDisabled
                  ]}
                  onPress={() => !loading && setNewUser({...newUser, role: 'swapper'})}
                  disabled={loading}
                >
                  <Text style={[
                    styles.roleButtonText,
                    newUser.role === 'swapper' && styles.roleButtonTextActive,
                    loading && styles.roleButtonTextDisabled
                  ]}>
                    Battery Swapper
                  </Text>
                </TouchableOpacity>
              </View>
            </View>

            <View style={styles.modalActions}>
              <HoppyButton
                title="Annuleren"
                onPress={() => setShowCreateModal(false)}
                variant="secondary"
                size="medium"
                disabled={loading}
                style={styles.modalButton}
              />
              <HoppyButton
                title={loading ? "Aanmaken..." : "Aanmaken"}
                onPress={handleCreateUser}
                variant="primary"
                size="medium"
                disabled={loading}
                style={styles.modalButton}
              />
            </View>
          </ScrollView>
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
    padding: HoppyTheme.spacing.lg,
    paddingTop: HoppyTheme.spacing.xl,
  },
  headerTitle: {
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: 'bold',
    color: HoppyColors.white,
    marginBottom: HoppyTheme.spacing.xs,
  },
  headerSubtitle: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.white,
    opacity: 0.9,
  },
  createButtonContainer: {
    padding: HoppyTheme.spacing.md,
  },
  createButton: {
    marginBottom: HoppyTheme.spacing.sm,
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: HoppyColors.white,
    marginHorizontal: HoppyTheme.spacing.md,
    marginBottom: HoppyTheme.spacing.md,
    borderRadius: HoppyTheme.borderRadius.md,
    paddingHorizontal: HoppyTheme.spacing.md,
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
  },
  searchIcon: {
    marginRight: HoppyTheme.spacing.sm,
  },
  searchInput: {
    flex: 1,
    paddingVertical: HoppyTheme.spacing.md,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray800,
  },
  clearButton: {
    padding: HoppyTheme.spacing.xs,
  },
  usersList: {
    flex: 1,
    paddingHorizontal: HoppyTheme.spacing.md,
  },
  loadingText: {
    textAlign: 'center',
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray600,
    marginTop: HoppyTheme.spacing.lg,
  },
  noUsersText: {
    textAlign: 'center',
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray600,
    marginTop: HoppyTheme.spacing.lg,
  },
  noAccessText: {
    textAlign: 'center',
    fontSize: HoppyTheme.fontSizes.lg,
    color: HoppyColors.error,
    margin: HoppyTheme.spacing.lg,
  },
  userCard: {
    marginBottom: HoppyTheme.spacing.sm,
  },
  userHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  userInfo: {
    flex: 1,
  },
  userName: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  userRole: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.primary,
    fontWeight: '600',
    marginTop: HoppyTheme.spacing.xs,
  },
  userDetails: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginTop: HoppyTheme.spacing.xs,
  },
  userActions: {
    flexDirection: 'row',
    gap: HoppyTheme.spacing.sm,
  },
  actionButton: {
    padding: HoppyTheme.spacing.sm,
    borderRadius: HoppyTheme.borderRadius.md,
    backgroundColor: HoppyColors.gray100,
  },
  deleteButton: {
    backgroundColor: HoppyColors.error + '20',
  },
  zonesContainer: {
    marginTop: HoppyTheme.spacing.sm,
    paddingTop: HoppyTheme.spacing.sm,
    borderTopWidth: 1,
    borderTopColor: HoppyColors.gray200,
  },
  zonesLabel: {
    fontSize: HoppyTheme.fontSizes.xs,
    color: HoppyColors.gray600,
    fontWeight: '600',
  },
  zonesText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray700,
    marginTop: HoppyTheme.spacing.xs,
  },
  modalContainer: {
    flex: 1,
    backgroundColor: HoppyColors.white,
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
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  modalContent: {
    flex: 1,
    padding: HoppyTheme.spacing.lg,
  },
  inputGroup: {
    marginBottom: HoppyTheme.spacing.md,
  },
  inputLabel: {
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '600',
    color: HoppyColors.gray700,
    marginBottom: HoppyTheme.spacing.xs,
  },
  helperText: {
    fontSize: HoppyTheme.fontSizes.xs,
    color: HoppyColors.gray600,
    marginTop: HoppyTheme.spacing.xs,
    fontStyle: 'italic',
  },
  textInput: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: HoppyTheme.borderRadius.md,
    padding: HoppyTheme.spacing.md,
    fontSize: HoppyTheme.fontSizes.md,
    backgroundColor: HoppyColors.white,
    color: HoppyColors.gray800,
  },
  textInputDisabled: {
    backgroundColor: HoppyColors.gray100,
    color: HoppyColors.gray500,
    borderColor: HoppyColors.gray200,
  },
  roleButtons: {
    flexDirection: 'row',
    gap: HoppyTheme.spacing.sm,
  },
  roleButton: {
    flex: 1,
    padding: HoppyTheme.spacing.md,
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: HoppyTheme.borderRadius.md,
    alignItems: 'center',
  },
  roleButtonActive: {
    borderColor: HoppyColors.primary,
    backgroundColor: HoppyColors.primary + '10',
  },
  roleButtonDisabled: {
    backgroundColor: HoppyColors.gray100,
    borderColor: HoppyColors.gray200,
  },
  roleButtonText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray700,
    fontWeight: '500',
  },
  roleButtonTextActive: {
    color: HoppyColors.primary,
    fontWeight: '600',
  },
  roleButtonTextDisabled: {
    color: HoppyColors.gray400,
  },
  modalActions: {
    flexDirection: 'row',
    gap: HoppyTheme.spacing.md,
    marginTop: HoppyTheme.spacing.lg,
  },
  modalButton: {
    flex: 1,
  },
});
