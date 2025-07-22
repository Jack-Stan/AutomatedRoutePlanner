import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  StyleSheet,
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  Image,
  TouchableOpacity,
  Modal,
  Dimensions,
  StatusBar,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { HoppyButton, HoppyCard, HoppyLogo } from '../components';
import { useAuth } from '../contexts/AuthContext';
import { apiService } from '../services/api';

const { width, height } = Dimensions.get('window');

export default function LoginScreen() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [showForgotPasswordModal, setShowForgotPasswordModal] = useState(false);
  const [forgotPasswordEmail, setForgotPasswordEmail] = useState('');
  const [isForgotPasswordLoading, setIsForgotPasswordLoading] = useState(false);
  const { login } = useAuth();

  const handleLogin = async () => {
    if (!username.trim() || !password.trim()) {
      Alert.alert('Fout', 'Vul alle velden in');
      return;
    }

    try {
      setIsLoading(true);
      await login(username.trim(), password);
    } catch (error: any) {
      Alert.alert(
        'Inloggen mislukt', 
        error.message || 'Er is een fout opgetreden bij het inloggen'
      );
    } finally {
      setIsLoading(false);
    }
  };

  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  const handleForgotPassword = async () => {
    if (!forgotPasswordEmail.trim()) {
      Alert.alert('Fout', 'Vul je emailadres in');
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(forgotPasswordEmail)) {
      Alert.alert('Fout', 'Voer een geldig emailadres in');
      return;
    }

    try {
      setIsForgotPasswordLoading(true);
      const result = await apiService.forgotPassword(forgotPasswordEmail);
      
      Alert.alert(
        'Email Verzonden',
        result.message,
        [
          {
            text: 'OK',
            onPress: () => {
              setShowForgotPasswordModal(false);
              setForgotPasswordEmail('');
            }
          }
        ]
      );
    } catch (error: any) {
      Alert.alert(
        'Fout',
        error.response?.data?.message || 'Er is een fout opgetreden bij het verzenden van de reset email'
      );
    } finally {
      setIsForgotPasswordLoading(false);
    }
  };

  return (
    <>
      <StatusBar barStyle="light-content" backgroundColor={HoppyColors.primaryDark} />
      <KeyboardAvoidingView 
        style={styles.container} 
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      >
        {/* Gradient Background */}
        <View style={styles.gradientBackground}>
          <View style={styles.gradientOverlay} />
        </View>

        {/* Floating Circles for Visual Interest */}
        <View style={styles.decorativeCircle1} />
        <View style={styles.decorativeCircle2} />
        <View style={styles.decorativeCircle3} />

        <ScrollView 
          contentContainerStyle={styles.scrollContainer}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          {/* Header Section */}
          <View style={styles.headerSection}>
            <View style={styles.logoContainer}>
              <Image 
                source={require('../../assets/images/HoppyLogo.png')} 
                style={styles.logo}
                resizeMode="contain"
              />
            </View>
            <Text style={styles.welcomeText}>Welkom bij</Text>
            <Text style={styles.appTitle}>Hoppy Route Manager</Text>
            <Text style={styles.subtitle}>Uw intelligente routeoptimalisatie platform</Text>
          </View>

          {/* Login Form */}
          <View style={styles.loginFormContainer}>
            <View style={styles.loginCard}>
              <Text style={styles.formTitle}>Inloggen</Text>
              
              <View style={styles.inputGroup}>
                <View style={styles.inputWrapper}>
                  <Ionicons 
                    name="person-outline" 
                    size={18} 
                    color={HoppyColors.primary} 
                    style={styles.inputIcon}
                  />
                  <TextInput
                    style={styles.input}
                    value={username}
                    onChangeText={setUsername}
                    placeholder="Gebruikersnaam"
                    placeholderTextColor={HoppyColors.gray400}
                    autoCapitalize="none"
                    autoCorrect={false}
                    editable={!isLoading}
                  />
                </View>

                <View style={styles.inputWrapper}>
                  <Ionicons 
                    name="lock-closed-outline" 
                    size={18} 
                    color={HoppyColors.primary} 
                    style={styles.inputIcon}
                  />
                  <TextInput
                    style={styles.passwordInput}
                    value={password}
                    onChangeText={setPassword}
                    placeholder="Wachtwoord"
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
                      name={showPassword ? 'eye-off-outline' : 'eye-outline'}
                      size={18}
                      color={HoppyColors.primary}
                    />
                  </TouchableOpacity>
                </View>
              </View>

              <HoppyButton
                title={isLoading ? "Inloggen..." : "Inloggen"}
                onPress={handleLogin}
                variant="primary"
                size="large"
                disabled={isLoading}
                style={styles.loginButton}
              />

              <TouchableOpacity
                onPress={() => setShowForgotPasswordModal(true)}
                disabled={isLoading}
                style={styles.forgotPasswordButton}
              >
                <Text style={styles.forgotPasswordText}>Wachtwoord vergeten?</Text>
              </TouchableOpacity>
            </View>
          </View>

          {/* Footer */}
          <View style={styles.footer}>
            <Text style={styles.footerText}>© 2025 Hoppy Route Manager</Text>
            <Text style={styles.footerSubtext}>Powered by Intelligent Route Optimization</Text>
          </View>
        </ScrollView>

        {/* Forgot Password Modal */}
        <Modal
          visible={showForgotPasswordModal}
          animationType="slide"
          presentationStyle="pageSheet"
          onRequestClose={() => setShowForgotPasswordModal(false)}
        >
          <View style={styles.modalContainer}>
            <View style={styles.modalHeader}>
              <TouchableOpacity
                onPress={() => setShowForgotPasswordModal(false)}
                style={styles.modalCloseButton}
              >
                <Ionicons name="close" size={24} color={HoppyColors.gray600} />
              </TouchableOpacity>
              <Text style={styles.modalTitle}>Wachtwoord Vergeten</Text>
              <View style={styles.modalHeaderSpacer} />
            </View>

            <ScrollView style={styles.modalContent}>
              <Text style={styles.modalDescription}>
                Voer je emailadres in en we sturen je een link om je wachtwoord opnieuw in te stellen.
              </Text>

              <View style={styles.modalInputContainer}>
                <Text style={styles.modalInputLabel}>Emailadres</Text>
                <TextInput
                  style={styles.modalInput}
                  value={forgotPasswordEmail}
                  onChangeText={setForgotPasswordEmail}
                  placeholder="je@email.com"
                  placeholderTextColor={HoppyColors.gray400}
                  keyboardType="email-address"
                  autoCapitalize="none"
                  autoCorrect={false}
                  editable={!isForgotPasswordLoading}
                />
              </View>

              <View style={styles.modalButtonContainer}>
                <HoppyButton
                  title="Annuleren"
                  onPress={() => setShowForgotPasswordModal(false)}
                  variant="secondary"
                  size="medium"
                  disabled={isForgotPasswordLoading}
                  style={styles.modalButton}
                />
                <HoppyButton
                  title={isForgotPasswordLoading ? "Verzenden..." : "Reset Link Sturen"}
                  onPress={handleForgotPassword}
                  variant="primary"
                  size="medium"
                  disabled={isForgotPasswordLoading}
                  style={styles.modalButton}
                />
              </View>
            </ScrollView>
          </View>
        </Modal>
      </KeyboardAvoidingView>
    </>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: HoppyColors.primaryDark, // Hoppy donkergroen
  },
  gradientBackground: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: HoppyColors.primaryDark, // Hoppy donkergroen
  },
  gradientOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(57, 197, 163, 0.2)', // Hoppy primary met transparantie
  },
  decorativeCircle1: {
    position: 'absolute',
    top: -50,
    right: -50,
    width: 200,
    height: 200,
    borderRadius: 100,
    backgroundColor: 'rgba(68, 219, 172, 0.1)', // Hoppy light turquoise
  },
  decorativeCircle2: {
    position: 'absolute',
    bottom: -100,
    left: -80,
    width: 300,
    height: 300,
    borderRadius: 150,
    backgroundColor: 'rgba(70, 218, 165, 0.08)', // Hoppy accent
  },
  decorativeCircle3: {
    position: 'absolute',
    top: height * 0.3,
    right: -120,
    width: 150,
    height: 150,
    borderRadius: 75,
    backgroundColor: 'rgba(64, 219, 183, 0.12)', // Hoppy alt color
  },
  scrollContainer: {
    flexGrow: 1,
    paddingHorizontal: HoppyTheme.spacing.lg,
  },
  headerSection: {
    alignItems: 'center',
    paddingTop: Platform.OS === 'ios' ? 60 : 40,
    paddingBottom: HoppyTheme.spacing.xl,
  },
  logoContainer: {
    width: 100,
    height: 100,
    borderRadius: 50, // Perfecte cirkel
    backgroundColor: 'rgba(255, 255, 255, 0.15)',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: HoppyTheme.spacing.lg,
    shadowColor: HoppyColors.primary,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 8,
    borderWidth: 2,
    borderColor: 'rgba(255, 255, 255, 0.2)',
  },
  logo: {
    width: 60,
    height: 60,
  },
  welcomeText: {
    fontSize: HoppyTheme.fontSizes.md,
    color: 'rgba(255, 255, 255, 0.8)',
    marginBottom: 4,
  },
  appTitle: {
    fontSize: HoppyTheme.fontSizes.xxl,
    fontWeight: 'bold',
    color: '#ffffff',
    textAlign: 'center',
    marginBottom: HoppyTheme.spacing.sm,
    letterSpacing: 0.5,
  },
  subtitle: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: 'rgba(255, 255, 255, 0.7)',
    textAlign: 'center',
    paddingHorizontal: HoppyTheme.spacing.md,
    lineHeight: 20,
  },
  loginFormContainer: {
    flex: 1,
    justifyContent: 'center',
    paddingVertical: HoppyTheme.spacing.xl,
    paddingHorizontal: HoppyTheme.spacing.xl, // Extra padding aan zijkanten
  },
  loginCard: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: HoppyTheme.borderRadius.xl,
    padding: HoppyTheme.spacing.lg,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.15,
    shadowRadius: 16,
    elevation: 12,
    maxWidth: 320, // Iets breder gemaakt
    width: '100%',
    alignSelf: 'center', // Centreren
  },
  formTitle: {
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: '600',
    color: HoppyColors.gray800,
    textAlign: 'center',
    marginBottom: HoppyTheme.spacing.lg, // Minder margin
  },
  inputGroup: {
    marginBottom: HoppyTheme.spacing.md,
  },
  inputWrapper: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.md,
    paddingHorizontal: HoppyTheme.spacing.sm,
    paddingVertical: HoppyTheme.spacing.xs,
    marginBottom: HoppyTheme.spacing.sm,
    borderWidth: 1,
    borderColor: HoppyColors.gray200,
    shadowColor: HoppyColors.primary,
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.08,
    shadowRadius: 2,
    elevation: 2,
    minHeight: 44, // Compactere hoogte
  },
  inputIcon: {
    marginRight: HoppyTheme.spacing.xs,
    color: HoppyColors.primary,
  },
  input: {
    flex: 1,
    paddingVertical: HoppyTheme.spacing.xs,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray800,
  },
  passwordInput: {
    flex: 1,
    paddingVertical: HoppyTheme.spacing.xs,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray800,
  },
  eyeButton: {
    padding: HoppyTheme.spacing.xs,
  },
  loginButton: {
    marginTop: HoppyTheme.spacing.md,
    shadowColor: HoppyColors.primary,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 6,
  },
  forgotPasswordButton: {
    marginTop: HoppyTheme.spacing.sm,
    alignItems: 'center',
    paddingVertical: HoppyTheme.spacing.sm,
  },
  forgotPasswordText: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.primary,
    fontWeight: '500',
  },
  footer: {
    alignItems: 'center',
    paddingBottom: Platform.OS === 'ios' ? 30 : 20,
    paddingTop: HoppyTheme.spacing.lg,
  },
  footerText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: 'rgba(255, 255, 255, 0.6)',
    marginBottom: 4,
  },
  footerSubtext: {
    fontSize: HoppyTheme.fontSizes.xs,
    color: 'rgba(255, 255, 255, 0.4)',
    textAlign: 'center',
  },
  // Modal styles remain the same
  modalContainer: {
    flex: 1,
    backgroundColor: HoppyColors.white,
  },
  modalHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: HoppyTheme.spacing.lg,
    paddingTop: 60,
    paddingBottom: HoppyTheme.spacing.lg,
    borderBottomWidth: 1,
    borderBottomColor: HoppyColors.gray200,
  },
  modalCloseButton: {
    padding: HoppyTheme.spacing.xs,
  },
  modalTitle: {
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: '600',
    color: HoppyColors.gray800,
    flex: 1,
    textAlign: 'center',
  },
  modalHeaderSpacer: {
    width: 32,
  },
  modalContent: {
    flex: 1,
    paddingHorizontal: HoppyTheme.spacing.lg,
    paddingVertical: HoppyTheme.spacing.xl,
  },
  modalDescription: {
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray600,
    textAlign: 'center',
    marginBottom: HoppyTheme.spacing.xl,
    lineHeight: 22,
  },
  modalInputContainer: {
    marginBottom: HoppyTheme.spacing.xl,
  },
  modalInputLabel: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: '500',
    color: HoppyColors.gray700,
    marginBottom: HoppyTheme.spacing.sm,
  },
  modalInput: {
    borderWidth: 1,
    borderColor: HoppyColors.gray300,
    borderRadius: HoppyTheme.borderRadius.md,
    paddingHorizontal: HoppyTheme.spacing.md,
    paddingVertical: HoppyTheme.spacing.md,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray800,
    backgroundColor: HoppyColors.white,
  },
  modalButtonContainer: {
    flexDirection: 'row',
    gap: HoppyTheme.spacing.md,
  },
  modalButton: {
    flex: 1,
  },
});
