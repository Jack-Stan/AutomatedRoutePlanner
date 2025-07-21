import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TextInput,
  Alert,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { useAuth } from '../contexts/AuthContext';
import { HoppyColors, HoppyTheme } from '../theme';
import { HoppyButton } from '../components';

export default function FirstLoginScreen() {
  const { user, changePassword } = useAuth();
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handleChangePassword = async () => {
    if (!oldPassword || !newPassword || !confirmPassword) {
      Alert.alert('Fout', 'Vul alle velden in');
      return;
    }

    if (newPassword !== confirmPassword) {
      Alert.alert('Fout', 'Nieuwe wachtwoorden komen niet overeen');
      return;
    }

    if (newPassword.length < 8) {
      Alert.alert('Fout', 'Nieuw wachtwoord moet minimaal 8 karakters lang zijn');
      return;
    }

    // Password strength validation
    const hasUpperCase = /[A-Z]/.test(newPassword);
    const hasLowerCase = /[a-z]/.test(newPassword);
    const hasNumber = /\d/.test(newPassword);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(newPassword);

    if (!hasUpperCase || !hasLowerCase || !hasNumber || !hasSpecialChar) {
      Alert.alert(
        'Zwak wachtwoord',
        'Het wachtwoord moet bevatten:\n• Minimaal 1 hoofdletter\n• Minimaal 1 kleine letter\n• Minimaal 1 cijfer\n• Minimaal 1 speciaal teken'
      );
      return;
    }

    if (oldPassword === newPassword) {
      Alert.alert('Fout', 'Het nieuwe wachtwoord moet verschillen van het huidige wachtwoord');
      return;
    }

    try {
      setLoading(true);
      await changePassword(oldPassword, newPassword);
      
      Alert.alert(
        'Succes',
        'Uw wachtwoord is succesvol gewijzigd. U kunt nu de app gebruiken.',
        [{ text: 'OK' }]
      );
    } catch (error: any) {
      Alert.alert('Fout', error.message || 'Kon wachtwoord niet wijzigen');
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView 
      style={styles.container} 
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView 
        style={styles.scrollContainer}
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
      >
        <View style={styles.header}>
          <Text style={styles.title}>Welkom bij Hoppy!</Text>
          <Text style={styles.subtitle}>
            Hallo {user?.firstName} {user?.lastName}
          </Text>
          <Text style={styles.description}>
            Dit is uw eerste login. Voor uw veiligheid moet u uw tijdelijke wachtwoord wijzigen voordat u de app kunt gebruiken.
          </Text>
        </View>

        <View style={styles.form}>
          <View style={styles.inputGroup}>
            <Text style={styles.label}>Huidig tijdelijk wachtwoord *</Text>
            <TextInput
              style={[styles.input, loading && styles.inputDisabled]}
              value={oldPassword}
              onChangeText={setOldPassword}
              secureTextEntry
              placeholder="Voer uw tijdelijke wachtwoord in"
              placeholderTextColor={HoppyColors.gray400}
              editable={!loading}
              autoCapitalize="none"
              autoCorrect={false}
            />
          </View>

          <View style={styles.inputGroup}>
            <Text style={styles.label}>Nieuw wachtwoord *</Text>
            <TextInput
              style={[styles.input, loading && styles.inputDisabled]}
              value={newPassword}
              onChangeText={setNewPassword}
              secureTextEntry
              placeholder="Voer uw nieuwe wachtwoord in"
              placeholderTextColor={HoppyColors.gray400}
              editable={!loading}
              autoCapitalize="none"
              autoCorrect={false}
            />
          </View>

          <View style={styles.inputGroup}>
            <Text style={styles.label}>Bevestig nieuw wachtwoord *</Text>
            <TextInput
              style={[styles.input, loading && styles.inputDisabled]}
              value={confirmPassword}
              onChangeText={setConfirmPassword}
              secureTextEntry
              placeholder="Bevestig uw nieuwe wachtwoord"
              placeholderTextColor={HoppyColors.gray400}
              editable={!loading}
              autoCapitalize="none"
              autoCorrect={false}
            />
          </View>

          <View style={styles.passwordRequirements}>
            <Text style={styles.requirementsTitle}>Wachtwoord vereisten:</Text>
            <Text style={styles.requirement}>• Minimaal 8 karakters lang</Text>
            <Text style={styles.requirement}>• Bevat hoofdletters (A-Z)</Text>
            <Text style={styles.requirement}>• Bevat kleine letters (a-z)</Text>
            <Text style={styles.requirement}>• Bevat cijfers (0-9)</Text>
            <Text style={styles.requirement}>• Bevat speciale tekens (!@#$%^&*)</Text>
          </View>

          <HoppyButton
            title={loading ? 'Wachtwoord wijzigen...' : 'Wachtwoord wijzigen'}
            onPress={handleChangePassword}
            disabled={loading}
            style={styles.submitButton}
          />
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: HoppyColors.gray50,
  },
  scrollContainer: {
    flex: 1,
  },
  scrollContent: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: HoppyTheme.spacing.lg,
  },
  header: {
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.xl,
  },
  title: {
    fontSize: HoppyTheme.fontSizes.xxl,
    fontWeight: 'bold',
    color: HoppyColors.primary,
    marginBottom: HoppyTheme.spacing.md,
  },
  subtitle: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: '600',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.sm,
  },
  description: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray600,
    textAlign: 'center',
    lineHeight: 24,
  },
  form: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    padding: HoppyTheme.spacing.lg,
    shadowColor: HoppyColors.black,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  inputGroup: {
    marginBottom: HoppyTheme.spacing.md,
  },
  label: {
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '600',
    color: HoppyColors.gray700,
    marginBottom: HoppyTheme.spacing.xs,
  },
  input: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: HoppyTheme.borderRadius.md,
    padding: HoppyTheme.spacing.md,
    fontSize: HoppyTheme.fontSizes.md,
    backgroundColor: HoppyColors.white,
    color: HoppyColors.gray800,
  },
  inputDisabled: {
    backgroundColor: HoppyColors.gray100,
    color: HoppyColors.gray500,
    borderColor: HoppyColors.gray200,
  },
  passwordRequirements: {
    backgroundColor: HoppyColors.info + '10',
    borderRadius: HoppyTheme.borderRadius.md,
    padding: HoppyTheme.spacing.md,
    marginBottom: HoppyTheme.spacing.lg,
    borderLeftWidth: 4,
    borderLeftColor: HoppyColors.info,
  },
  requirementsTitle: {
    fontSize: HoppyTheme.fontSizes.sm,
    fontWeight: '600',
    color: HoppyColors.gray700,
    marginBottom: HoppyTheme.spacing.xs,
  },
  requirement: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginBottom: HoppyTheme.spacing.xs / 2,
  },
  submitButton: {
    marginTop: HoppyTheme.spacing.md,
  },
});
