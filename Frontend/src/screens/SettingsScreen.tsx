import React from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { HoppyColors, HoppyTheme } from '../theme';
import { useAuth } from '../contexts/AuthContext';
import { RootStackParamList } from '../types';

type SettingsScreenNavigationProp = StackNavigationProp<RootStackParamList>;

export default function SettingsScreen() {
  const { user, logout } = useAuth();
  const navigation = useNavigation<SettingsScreenNavigationProp>();

  const getRoleDisplayName = (role: string): string => {
    switch (role.toLowerCase()) {
      case 'admin': return 'Administrator';
      case 'fleetmanager': return 'Fleet Manager';
      case 'swapper': return 'Battery Swapper';
      default: return 'Gebruiker';
    }
  };

  const SettingItem = ({ 
    title, 
    subtitle, 
    icon, 
    onPress, 
    showArrow = true,
    color = HoppyColors.gray800,
    iconColor = HoppyColors.primary 
  }: {
    title: string;
    subtitle?: string;
    icon: keyof typeof Ionicons.glyphMap;
    onPress: () => void;
    showArrow?: boolean;
    color?: string;
    iconColor?: string;
  }) => (
    <TouchableOpacity style={styles.settingItem} onPress={onPress}>
      <View style={styles.settingLeft}>
        <View style={[styles.iconContainer, { backgroundColor: iconColor + '20' }]}>
          <Ionicons name={icon} size={24} color={iconColor} />
        </View>
        <View style={styles.settingText}>
          <Text style={[styles.settingTitle, { color }]}>{title}</Text>
          {subtitle && <Text style={styles.settingSubtitle}>{subtitle}</Text>}
        </View>
      </View>
      {showArrow && (
        <Ionicons name="chevron-forward" size={20} color={HoppyColors.gray400} />
      )}
    </TouchableOpacity>
  );

  const handleLogout = () => {
    Alert.alert(
      'Uitloggen',
      'Weet je zeker dat je wilt uitloggen?',
      [
        { text: 'Annuleren', style: 'cancel' },
        { 
          text: 'Uitloggen', 
          style: 'destructive', 
          onPress: logout 
        }
      ]
    );
  };

  return (
    <ScrollView style={styles.container}>
      {/* User Info Section */}
      <View style={styles.section}>
        <View style={styles.userCard}>
          <View style={styles.userAvatar}>
            <Text style={styles.userAvatarText}>
              {user?.firstName?.charAt(0)?.toUpperCase() || user?.username?.charAt(0)?.toUpperCase() || 'U'}
            </Text>
          </View>
          <View style={styles.userInfo}>
            <Text style={styles.userName}>
              {user?.firstName && user?.lastName 
                ? `${user.firstName} ${user.lastName}` 
                : user?.username || 'Onbekende gebruiker'}
            </Text>
            <Text style={styles.userRole}>{getRoleDisplayName(user?.roleName || '')}</Text>
            <Text style={styles.userEmail}>{user?.email}</Text>
          </View>
        </View>
      </View>

      {/* Admin Only Section */}
      {user?.roleName?.toLowerCase() === 'admin' && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Beheer (Administrator)</Text>
          <View style={styles.settingsGroup}>
            <SettingItem
              title="Gebruikersbeheer"
              subtitle="Accounts aanmaken, bewerken en verwijderen"
              icon="people"
              onPress={() => navigation.navigate('UserManagement')}
              iconColor={HoppyColors.primary}
            />
            <SettingItem
              title="Systeem Instellingen"
              subtitle="Geavanceerde configuratie opties"
              icon="settings"
              onPress={() => Alert.alert('Systeem Instellingen', 'Functie komt binnenkort!')}
              iconColor={HoppyColors.info}
            />
          </View>
        </View>
      )}

      {/* Fleet Manager Section */}
      {(user?.roleName?.toLowerCase() === 'admin' || user?.roleName?.toLowerCase() === 'fleetmanager') && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>
            {user?.roleName?.toLowerCase() === 'admin' ? 'Management Tools' : 'Fleet Management'}
          </Text>
          <View style={styles.settingsGroup}>
            <SettingItem
              title="Zone Beheer"
              subtitle="Geografische gebieden configureren"
              icon="map"
              onPress={() => Alert.alert('Zone Beheer', 'Functie komt binnenkort!')}
              iconColor={HoppyColors.success}
            />
            <SettingItem
              title="Voertuig Configuratie"
              subtitle="E-scooter en e-bike instellingen"
              icon="bicycle"
              onPress={() => Alert.alert('Voertuig Configuratie', 'Functie komt binnenkort!')}
              iconColor={HoppyColors.warning}
            />
          </View>
        </View>
      )}

      {/* General Settings */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Algemeen</Text>
        <View style={styles.settingsGroup}>
          <SettingItem
            title="Profiel Bewerken"
            subtitle="Persoonlijke gegevens wijzigen"
            icon="person"
            onPress={() => Alert.alert('Profiel Bewerken', 'Functie komt binnenkort!')}
            iconColor={HoppyColors.info}
          />
          <SettingItem
            title="Notificaties"
            subtitle="Push notificatie instellingen"
            icon="notifications"
            onPress={() => Alert.alert('Notificaties', 'Functie komt binnenkort!')}
            iconColor={HoppyColors.success}
          />
          <SettingItem
            title="Over Hoppy"
            subtitle="Versie info en licenties"
            icon="information-circle"
            onPress={() => Alert.alert('Over Hoppy', 'Hoppy Route Manager v1.0.0\n\nEen batterij swap management systeem voor e-mobility.')}
            iconColor={HoppyColors.gray600}
          />
        </View>
      </View>

      {/* Logout Section */}
      <View style={styles.section}>
        <View style={styles.settingsGroup}>
          <SettingItem
            title="Uitloggen"
            subtitle="Veilig uitloggen van je account"
            icon="log-out"
            onPress={handleLogout}
            showArrow={false}
            color={HoppyColors.error}
            iconColor={HoppyColors.error}
          />
        </View>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: HoppyColors.gray50,
  },
  section: {
    marginTop: HoppyTheme.spacing.lg,
    paddingHorizontal: HoppyTheme.spacing.md,
  },
  sectionTitle: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.sm,
    paddingHorizontal: HoppyTheme.spacing.sm,
  },
  userCard: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.lg,
    flexDirection: 'row',
    alignItems: 'center',
    ...HoppyTheme.shadows.md,
  },
  userAvatar: {
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: HoppyColors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: HoppyTheme.spacing.md,
  },
  userAvatarText: {
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: 'bold',
    color: HoppyColors.white,
  },
  userInfo: {
    flex: 1,
  },
  userName: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.xs,
  },
  userRole: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.primary,
    fontWeight: '600',
    marginBottom: HoppyTheme.spacing.xs,
  },
  userEmail: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
  },
  settingsGroup: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    ...HoppyTheme.shadows.sm,
  },
  settingItem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: HoppyTheme.spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: HoppyColors.gray100,
  },
  settingLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: HoppyTheme.spacing.md,
  },
  settingText: {
    flex: 1,
  },
  settingTitle: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: '600',
    marginBottom: HoppyTheme.spacing.xs,
  },
  settingSubtitle: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
  },
});
