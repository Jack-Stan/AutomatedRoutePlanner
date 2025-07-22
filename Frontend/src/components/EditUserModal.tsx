import React, { useState } from 'react';
import {
  View,
  Text,
  Modal,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Alert,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors } from '../theme';
import { HoppyButton } from '../components';

interface User {
  id: number;
  firstName?: string;
  lastName?: string;
  username: string;
  email: string;
  roleName: string;
  assignedZoneId?: number;
  assignedZoneName?: string;
  isActive: boolean;
}

interface EditUserModalProps {
  visible: boolean;
  user: User | null;
  onClose: () => void;
  onSave: (userData: Partial<User>) => void;
  onResetPassword: (userId: number) => void;
  isAdmin: boolean;
}

const EditUserModal: React.FC<EditUserModalProps> = ({
  visible,
  user,
  onClose,
  onSave,
  onResetPassword,
  isAdmin,
}) => {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [selectedZone, setSelectedZone] = useState<number | undefined>();

  React.useEffect(() => {
    if (user) {
      setFirstName(user.firstName || '');
      setLastName(user.lastName || '');
      setEmail(user.email);
      setSelectedZone(user.assignedZoneId);
    }
  }, [user]);

  const handleSave = () => {
    if (!user) return;

    // Validate required fields
    if (!email.trim()) {
      Alert.alert('Fout', 'Email is verplicht');
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      Alert.alert('Fout', 'Voer een geldig emailadres in');
      return;
    }

    onSave({
      id: user.id,
      firstName: firstName.trim() || undefined,
      lastName: lastName.trim() || undefined,
      email: email.trim(),
      assignedZoneId: selectedZone,
    });
  };

  const handleResetPassword = () => {
    if (!user) return;

    Alert.alert(
      'Wachtwoord Resetten',
      `Weet je zeker dat je het wachtwoord wilt resetten voor ${user.firstName} ${user.lastName}?\n\nEr wordt een nieuw tijdelijk wachtwoord verzonden naar ${user.email}.`,
      [
        { text: 'Annuleren', style: 'cancel' },
        {
          text: 'Resetten',
          style: 'destructive',
          onPress: () => onResetPassword(user.id),
        },
      ]
    );
  };

  const availableZones = [
    { id: 1, name: 'Gent Centrum' },
    { id: 2, name: 'Gent Universiteit' },
    { id: 3, name: 'Brussel Centrum' },
    { id: 4, name: 'Antwerpen Centrum' },
  ];

  if (!user) return null;

  return (
    <Modal
      visible={visible}
      animationType="slide"
      presentationStyle="pageSheet"
      onRequestClose={onClose}
    >
      <View style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={onClose} style={styles.closeButton}>
            <Ionicons name="close" size={24} color={HoppyColors.gray600} />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Gebruiker Bewerken</Text>
          <View style={styles.headerSpacer} />
        </View>

        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          <View style={styles.userInfo}>
            <Text style={styles.username}>@{user.username}</Text>
            <Text style={styles.role}>{user.roleName}</Text>
          </View>

          <View style={styles.formSection}>
            <Text style={styles.sectionTitle}>Persoonlijke Gegevens</Text>
            
            <View style={styles.inputGroup}>
              <Text style={styles.label}>Voornaam</Text>
              <TextInput
                style={styles.input}
                value={firstName}
                onChangeText={setFirstName}
                placeholder="Voornaam"
                placeholderTextColor={HoppyColors.gray400}
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.label}>Achternaam</Text>
              <TextInput
                style={styles.input}
                value={lastName}
                onChangeText={setLastName}
                placeholder="Achternaam"
                placeholderTextColor={HoppyColors.gray400}
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.label}>Email *</Text>
              <TextInput
                style={styles.input}
                value={email}
                onChangeText={setEmail}
                placeholder="email@example.com"
                placeholderTextColor={HoppyColors.gray400}
                keyboardType="email-address"
                autoCapitalize="none"
              />
            </View>
          </View>

          {isAdmin && user.roleName?.toLowerCase() !== 'admin' && (
            <View style={styles.formSection}>
              <Text style={styles.sectionTitle}>Zone Toewijzing</Text>
              
              {availableZones.map((zone) => (
                <TouchableOpacity
                  key={zone.id}
                  style={[
                    styles.zoneOption,
                    selectedZone === zone.id && styles.zoneOptionSelected,
                  ]}
                  onPress={() => setSelectedZone(selectedZone === zone.id ? undefined : zone.id)}
                >
                  <Text
                    style={[
                      styles.zoneOptionText,
                      selectedZone === zone.id && styles.zoneOptionTextSelected,
                    ]}
                  >
                    {zone.name}
                  </Text>
                  {selectedZone === zone.id && (
                    <Ionicons name="checkmark" size={20} color={HoppyColors.primary} />
                  )}
                </TouchableOpacity>
              ))}
              
              {selectedZone && (
                <TouchableOpacity
                  style={styles.clearZoneButton}
                  onPress={() => setSelectedZone(undefined)}
                >
                  <Text style={styles.clearZoneText}>Geen zone</Text>
                </TouchableOpacity>
              )}
            </View>
          )}

          <View style={styles.actionsSection}>
            <HoppyButton
              title="Wachtwoord Resetten"
              onPress={handleResetPassword}
              variant="secondary"
              size="medium"
              style={styles.actionButton}
            />
          </View>
        </ScrollView>

        <View style={styles.footer}>
          <HoppyButton
            title="Annuleren"
            onPress={onClose}
            variant="secondary"
            size="medium"
            style={styles.footerButton}
          />
          <HoppyButton
            title="Opslaan"
            onPress={handleSave}
            variant="primary"
            size="medium"
            style={styles.footerButton}
          />
        </View>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: HoppyColors.white,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 20,
    paddingTop: 60,
    paddingBottom: 20,
    borderBottomWidth: 1,
    borderBottomColor: HoppyColors.gray200,
  },
  closeButton: {
    padding: 4,
  },
  headerTitle: {
    flex: 1,
    fontSize: 18,
    fontWeight: '600',
    color: HoppyColors.gray800,
    textAlign: 'center',
  },
  headerSpacer: {
    width: 32,
  },
  content: {
    flex: 1,
    paddingHorizontal: 20,
  },
  userInfo: {
    alignItems: 'center',
    paddingVertical: 24,
  },
  username: {
    fontSize: 20,
    fontWeight: '600',
    color: HoppyColors.gray800,
    marginBottom: 4,
  },
  role: {
    fontSize: 16,
    color: HoppyColors.gray600,
  },
  formSection: {
    marginBottom: 32,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: HoppyColors.gray800,
    marginBottom: 16,
  },
  inputGroup: {
    marginBottom: 20,
  },
  label: {
    fontSize: 16,
    fontWeight: '500',
    color: HoppyColors.gray700,
    marginBottom: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 12,
    fontSize: 16,
    color: HoppyColors.gray800,
    backgroundColor: HoppyColors.white,
  },
  zoneOption: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 16,
    paddingHorizontal: 16,
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: 8,
    marginBottom: 8,
    backgroundColor: HoppyColors.white,
  },
  zoneOptionSelected: {
    borderColor: HoppyColors.primary,
    backgroundColor: HoppyColors.primaryLight + '20',
  },
  zoneOptionText: {
    fontSize: 16,
    color: HoppyColors.gray700,
  },
  zoneOptionTextSelected: {
    color: HoppyColors.primary,
    fontWeight: '500',
  },
  clearZoneButton: {
    paddingVertical: 12,
    alignItems: 'center',
  },
  clearZoneText: {
    fontSize: 16,
    color: HoppyColors.error,
  },
  actionsSection: {
    marginBottom: 32,
  },
  actionButton: {
    marginBottom: 12,
  },
  footer: {
    flexDirection: 'row',
    paddingHorizontal: 20,
    paddingVertical: 20,
    borderTopWidth: 1,
    borderTopColor: HoppyColors.gray200,
    gap: 12,
  },
  footerButton: {
    flex: 1,
  },
});

export default EditUserModal;
