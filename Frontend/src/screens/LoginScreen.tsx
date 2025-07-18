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
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { HoppyButton, HoppyCard, HoppyLogo } from '../components';
import { useAuth } from '../contexts/AuthContext';

export default function LoginScreen() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
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

  return (
    <KeyboardAvoidingView 
      style={styles.container} 
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView 
        contentContainerStyle={styles.scrollContainer}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        {/* Logo Background */}
        <View style={styles.logoBackground}>
          <Image 
            source={require('../../assets/images/HoppyLogo.png')} 
            style={styles.backgroundLogo}
            resizeMode="contain"
          />
        </View>

        {/* Compact Login Form */}
        <View style={styles.loginFormContainer}>
          <HoppyCard style={styles.loginCard}>
            <Text style={styles.formTitle}>Hoppy Route Manager</Text>
            
            <View style={styles.inputContainer}>
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

            <View style={styles.inputContainer}>
              <View style={styles.passwordContainer}>
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
                    name={showPassword ? 'eye-off' : 'eye'}
                    size={20}
                    color={HoppyColors.gray500}
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
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'center',
    alignItems: 'center',
    opacity: 0.1,
    zIndex: 0,
  },
  backgroundLogo: {
    width: '80%',
    height: '80%',
  },
  loginFormContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: HoppyTheme.spacing.lg,
    zIndex: 1,
  },
  loginCard: {
    width: '100%',
    maxWidth: 350,
    padding: HoppyTheme.spacing.xl,
  },
  formTitle: {
    fontSize: HoppyTheme.fontSizes.xl,
    fontWeight: 'bold',
    color: HoppyColors.primary,
    marginBottom: HoppyTheme.spacing.xl,
    textAlign: 'center',
  },
  inputContainer: {
    marginBottom: HoppyTheme.spacing.lg,
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
  loginButton: {
    marginTop: HoppyTheme.spacing.lg,
  },
});
