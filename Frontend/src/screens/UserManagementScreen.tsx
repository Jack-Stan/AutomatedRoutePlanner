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
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { HoppyButton, HoppyCard } from '../components';
import { useAuth, User, isAdmin, isFleetManager, getRoleDisplayName } from '../contexts/AuthContext';
import { apiService } from '../services/api';

interface CreateUserData {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  role: 'fleetmanager' | 'swapper';
  assignedZones?: number[];
}

export default function UserManagementScreen() {
  const { user } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newUser, setNewUser] = useState<CreateUserData>({
    username: '',
    email: '',
    firstName: '',
    lastName: '',
    password: '',
    role: 'swapper',
    assignedZones: [],
  });

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    try {
      setLoading(true);
      // Use API service to get users
      const userData = await apiService.getUsers();
      setUsers(userData);
    } catch (error) {
      console.error('Error fetching users:', error);
      Alert.alert('Fout', 'Kon gebruikers niet laden');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateUser = async () => {
    if (!newUser.username || !newUser.email || !newUser.firstName || !newUser.lastName || !newUser.password) {
      Alert.alert('Fout', 'Vul alle verplichte velden in');
      return;
    }

    try {
      setLoading(true);
      
      // Validate role permissions
      if (isFleetManager(user) && newUser.role !== 'swapper') {
        Alert.alert('Fout', 'Fleet Managers kunnen alleen Battery Swappers aanmaken');
        return;
      }

      // Use API service to create user
      const createdUser = await apiService.createUser({
        username: newUser.username,
        email: newUser.email,
        password: newUser.password,
        role: newUser.role,
        firstName: newUser.firstName,
        lastName: newUser.lastName,
      });

      Alert.alert('Succes', 'Gebruiker succesvol aangemaakt');
      setShowCreateModal(false);
      setNewUser({
        username: '',
        email: '',
        firstName: '',
        lastName: '',
        password: '',
        role: 'swapper',
        assignedZones: [],
      });
      fetchUsers();
    } catch (error) {
      console.error('Error creating user:', error);
      Alert.alert('Fout', 'Kon gebruiker niet aanmaken');
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
    } catch (error) {
      console.error('Error deleting user:', error);
      Alert.alert('Fout', 'Kon gebruiker niet verwijderen');
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
            onPress={() => Alert.alert('Info', 'Bewerken functie komt binnenkort')}
          >
            <Ionicons name="pencil" size={20} color={HoppyColors.primary} />
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.actionButton, styles.deleteButton]}
            onPress={() => handleDeleteUser(userData.id, `${userData.firstName} ${userData.lastName}`)}
          >
            <Ionicons name="trash" size={20} color={HoppyColors.error} />
          </TouchableOpacity>
        </View>
      </View>
      
      {userData.assignedZoneId && (
        <View style={styles.zonesContainer}>
          <Text style={styles.zonesLabel}>Toegewezen zone:</Text>
          <Text style={styles.zonesText}>Zone {userData.assignedZoneId}</Text>
        </View>
      )}
    </HoppyCard>
  );

  if (!user || (!isAdmin(user) && !isFleetManager(user))) {
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
          {isAdmin(user) ? 'Beheer Fleet Managers en Swappers' : 'Beheer Battery Swappers'}
        </Text>
      </View>

      {/* Create Button */}
      <View style={styles.createButtonContainer}>
        <HoppyButton
          title={`Nieuwe ${isAdmin(user) ? 'Gebruiker' : 'Swapper'} Aanmaken`}
          onPress={() => setShowCreateModal(true)}
          variant="primary"
          size="medium"
          style={styles.createButton}
        />
      </View>

      {/* Users List */}
      <ScrollView style={styles.usersList} showsVerticalScrollIndicator={false}>
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
            <TouchableOpacity onPress={() => setShowCreateModal(false)}>
              <Ionicons name="close" size={24} color={HoppyColors.gray600} />
            </TouchableOpacity>
          </View>

          <ScrollView style={styles.modalContent} showsVerticalScrollIndicator={false}>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Voornaam *</Text>
              <TextInput
                style={styles.textInput}
                value={newUser.firstName}
                onChangeText={(text) => setNewUser({...newUser, firstName: text})}
                placeholder="Voornaam"
                placeholderTextColor={HoppyColors.gray400}
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Achternaam *</Text>
              <TextInput
                style={styles.textInput}
                value={newUser.lastName}
                onChangeText={(text) => setNewUser({...newUser, lastName: text})}
                placeholder="Achternaam"
                placeholderTextColor={HoppyColors.gray400}
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Gebruikersnaam *</Text>
              <TextInput
                style={styles.textInput}
                value={newUser.username}
                onChangeText={(text) => setNewUser({...newUser, username: text})}
                placeholder="Gebruikersnaam"
                placeholderTextColor={HoppyColors.gray400}
                autoCapitalize="none"
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>E-mail *</Text>
              <TextInput
                style={styles.textInput}
                value={newUser.email}
                onChangeText={(text) => setNewUser({...newUser, email: text})}
                placeholder="email@example.com"
                placeholderTextColor={HoppyColors.gray400}
                keyboardType="email-address"
                autoCapitalize="none"
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Wachtwoord *</Text>
              <TextInput
                style={styles.textInput}
                value={newUser.password}
                onChangeText={(text) => setNewUser({...newUser, password: text})}
                placeholder="Tijdelijk wachtwoord"
                placeholderTextColor={HoppyColors.gray400}
                secureTextEntry
              />
            </View>

            {isAdmin(user) && (
              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>Rol *</Text>
                <View style={styles.roleButtons}>
                  <TouchableOpacity
                    style={[
                      styles.roleButton,
                      newUser.role === 'fleetmanager' && styles.roleButtonActive
                    ]}
                    onPress={() => setNewUser({...newUser, role: 'fleetmanager'})}
                  >
                    <Text style={[
                      styles.roleButtonText,
                      newUser.role === 'fleetmanager' && styles.roleButtonTextActive
                    ]}>
                      Fleet Manager
                    </Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={[
                      styles.roleButton,
                      newUser.role === 'swapper' && styles.roleButtonActive
                    ]}
                    onPress={() => setNewUser({...newUser, role: 'swapper'})}
                  >
                    <Text style={[
                      styles.roleButtonText,
                      newUser.role === 'swapper' && styles.roleButtonTextActive
                    ]}>
                      Battery Swapper
                    </Text>
                  </TouchableOpacity>
                </View>
              </View>
            )}

            <View style={styles.modalActions}>
              <HoppyButton
                title="Annuleren"
                onPress={() => setShowCreateModal(false)}
                variant="secondary"
                size="medium"
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
  textInput: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: HoppyTheme.borderRadius.md,
    padding: HoppyTheme.spacing.md,
    fontSize: HoppyTheme.fontSizes.md,
    backgroundColor: HoppyColors.white,
    color: HoppyColors.gray800,
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
  roleButtonText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray700,
    fontWeight: '500',
  },
  roleButtonTextActive: {
    color: HoppyColors.primary,
    fontWeight: '600',
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
