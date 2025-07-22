import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TextInput,
  StyleSheet,
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  TouchableOpacity,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { HoppyButton, HoppyCard, HoppyLogo } from '../components';
import { apiService } from '../services/api';

interface ResetPasswordScreenProps {
  route: {
    params: {
      token?: string;
    };
  };
  navigation: any;
}

export default function ResetPasswordScreen({ route, navigation }: ResetPasswordScreenProps) {
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [token, setToken] = useState<string>('');

  useEffect(() => {
    // Get token from route params or URL
    const tokenFromRoute = route?.params?.token;
    if (tokenFromRoute) {
      setToken(tokenFromRoute);
    }
  }, [route]);

  const validatePassword = (password: string): string | null => {
    if (password.length < 8) {
      return 'Wachtwoord moet minimaal 8 karakters lang zijn';
    }
    if (!/(?=.*[a-z])/.test(password)) {
      return 'Wachtwoord moet minimaal 1 kleine letter bevatten';
    }
    if (!/(?=.*[A-Z])/.test(password)) {
      return 'Wachtwoord moet minimaal 1 hoofdletter bevatten';
    }
    if (!/(?=.*\d)/.test(password)) {
      return 'Wachtwoord moet minimaal 1 cijfer bevatten';
    }
    return null;
  };

  const handleResetPassword = async () => {
    if (!token) {
      Alert.alert('Fout', 'Geen geldige reset token gevonden');
      return;
    }

    if (!newPassword.trim() || !confirmPassword.trim()) {
      Alert.alert('Fout', 'Vul alle velden in');
      return;
    }

    const passwordError = validatePassword(newPassword);
    if (passwordError) {
      Alert.alert('Fout', passwordError);
      return;
    }

    if (newPassword !== confirmPassword) {
      Alert.alert('Fout', 'Wachtwoorden komen niet overeen');
      return;
    }

    try {
      setIsLoading(true);
      const result = await apiService.resetPassword(token, newPassword);
      
      Alert.alert(
        'Succes',
        'Je wachtwoord is succesvol gewijzigd. Je kunt nu inloggen met je nieuwe wachtwoord.',
        [
          {
            text: 'Naar Inloggen',
            onPress: () => navigation.navigate('Login')
          }
        ]
      );
    } catch (error: any) {
      Alert.alert(
        'Fout',
        error.response?.data?.message || 'Er is een fout opgetreden bij het wijzigen van je wachtwoord'
      );
    } finally {
      setIsLoading(false);
    }
  };

  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  const toggleConfirmPasswordVisibility = () => {
    setShowConfirmPassword(!showConfirmPassword);
  };

  return (
    <KeyboardAvoidingView 
      style={styles.container} 
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView 
        contentContainerStyle={styles.scrollContainer}
        keyboardShouldPersistTaps="handled"
      >
        {/* Background Logo */}
        <View style={styles.logoBackground}>
          <HoppyLogo />
        </View>

        <View style={styles.content}>
          <View style={styles.headerContainer}>
            <HoppyLogo />
            <Text style={styles.title}>Nieuw Wachtwoord</Text>
            <Text style={styles.subtitle}>
              Voer je nieuwe wachtwoord in om je account te beveiligen
            </Text>
          </View>

          <HoppyCard padding="large" style={styles.formCard}>
            <View style={styles.inputContainer}>
              <View style={styles.passwordContainer}>
                <TextInput
                  style={styles.passwordInput}
                  value={newPassword}
                  onChangeText={setNewPassword}
                  placeholder="Nieuw wachtwoord"
                  placeholderTextColor={HoppyColors.gray400}
                  secureTextEntry={!showPassword}
                  autoCapitalize="none"
                  autoCorrect={false}
                  editable={!isLoading}
                />
                <TouchableOpacity
                  style={styles.eyeButton}
                  onPress={togglePasswordVisibility}
                  disabled={isLoading}
                >
                  <Ionicons
                    name={showPassword ? 'eye-off' : 'eye'}
                    size={20}
                    color={HoppyColors.gray500}
                  />
                </TouchableOpacity>
              </View>
            </View>

            <View style={styles.inputContainer}>
              <View style={styles.passwordContainer}>
                <TextInput
                  style={styles.passwordInput}
                  value={confirmPassword}
                  onChangeText={setConfirmPassword}
                  placeholder="Bevestig nieuw wachtwoord"
                  placeholderTextColor={HoppyColors.gray400}
                  secureTextEntry={!showConfirmPassword}
                  autoCapitalize="none"
                  autoCorrect={false}
                  editable={!isLoading}
                />
                <TouchableOpacity
                  style={styles.eyeButton}
                  onPress={toggleConfirmPasswordVisibility}
                  disabled={isLoading}
                >
                  <Ionicons
                    name={showConfirmPassword ? 'eye-off' : 'eye'}
                    size={20}
                    color={HoppyColors.gray500}
                  />
                </TouchableOpacity>
              </View>
            </View>

            <View style={styles.passwordRequirements}>
              <Text style={styles.requirementsTitle}>Wachtwoordvereisten:</Text>
              <Text style={styles.requirementItem}>• Minimaal 8 karakters</Text>
              <Text style={styles.requirementItem}>• Minimaal 1 kleine letter</Text>
              <Text style={styles.requirementItem}>• Minimaal 1 hoofdletter</Text>
              <Text style={styles.requirementItem}>• Minimaal 1 cijfer</Text>
            </View>

            <HoppyButton
              title={isLoading ? "Wijzigen..." : "Wachtwoord Wijzigen"}
              onPress={handleResetPassword}
              variant="primary"
              size="large"
              disabled={isLoading}
              style={styles.resetButton}
            />

            <TouchableOpacity
              onPress={() => navigation.navigate('Login')}
              disabled={isLoading}
              style={styles.backToLoginButton}
            >
              <Text style={styles.backToLoginText}>Terug naar inloggen</Text>
            </TouchableOpacity>
          </HoppyCard>
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
    flexGrow: 1,
    justifyContent: 'center',
    minHeight: '100%',
    position: 'relative',
  },
  logoBackground: {
    position: 'absolute',
    top: '30%',
    left: '50%',
    transform: [{ translateX: -100 }, { translateY: -100 }],
    zIndex: 0,
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    paddingHorizontal: HoppyTheme.spacing.lg,
    zIndex: 1,
  },
  headerContainer: {
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.xl,
  },
  title: {
    fontSize: HoppyTheme.fontSizes.xxxl,
    fontWeight: 'bold',
    color: HoppyColors.primary,
    marginTop: HoppyTheme.spacing.lg,
    marginBottom: HoppyTheme.spacing.sm,
    textAlign: 'center',
  },
  subtitle: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray600,
    textAlign: 'center',
    lineHeight: 22,
  },
  formCard: {
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 4,
  },
  inputContainer: {
    marginBottom: HoppyTheme.spacing.lg,
  },
  passwordContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: HoppyTheme.borderRadius.md,
    backgroundColor: HoppyColors.white,
  },
  passwordInput: {
    flex: 1,
    padding: HoppyTheme.spacing.md,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray800,
  },
  eyeButton: {
    padding: HoppyTheme.spacing.md,
    justifyContent: 'center',
    alignItems: 'center',
  },
  passwordRequirements: {
    marginBottom: HoppyTheme.spacing.lg,
    padding: HoppyTheme.spacing.md,
    backgroundColor: HoppyColors.gray100,
    borderRadius: HoppyTheme.borderRadius.md,
  },
  requirementsTitle: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: '600',
    color: HoppyColors.gray700,
    marginBottom: HoppyTheme.spacing.sm,
  },
  requirementItem: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginBottom: 2,
  },
  resetButton: {
    marginTop: HoppyTheme.spacing.lg,
  },
  backToLoginButton: {
    marginTop: HoppyTheme.spacing.md,
    alignItems: 'center',
    paddingVertical: HoppyTheme.spacing.sm,
  },
  backToLoginText: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.primary,
    textDecorationLine: 'underline',
  },
});
