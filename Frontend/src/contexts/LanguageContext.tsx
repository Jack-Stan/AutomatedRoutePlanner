import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';

export type Language = 'nl' | 'en' | 'es' | 'el';

interface LanguageContextType {
  currentLanguage: Language;
  setLanguage: (language: Language) => Promise<void>;
  t: (key: string, params?: Record<string, any>) => string;
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

interface LanguageProviderProps {
  children: ReactNode;
}

// Translations
const translations: Record<Language, Record<string, string>> = {
  nl: {
    // Dashboard
    'dashboard.title': 'Dashboard',
    'dashboard.welcome': 'Welkom terug',
    'dashboard.activeUsers': 'Actieve Gebruikers',
    'dashboard.totalZones': 'Totaal Zones',
    'dashboard.lowBattery': 'Te Swappen (<25%)',
    'dashboard.pendingRoutes': 'Routes Wachtend',
    'dashboard.assignedRoutes': 'Toegewezen Routes',
    'dashboard.critical': 'Kritiek (<10%)',
    'dashboard.completedToday': 'Voltooid Vandaag',
    'dashboard.activeRoutes': 'Actieve Routes',
    'dashboard.availableSwappers': 'Beschikbare Swappers',
    'dashboard.vehiclesToPlan': 'Te Plannen Voertuigen',
    'dashboard.routesToday': 'Routes Vandaag',
    'dashboard.totalVehicles': 'Totaal Voertuigen',
    'dashboard.criticalBattery': 'Kritieke Batterij',
    'dashboard.systemZones': 'Systeem Zones',
    
    // Navigation
    'nav.dashboard': 'Dashboard',
    'nav.routes': 'Routes',
    'nav.vehicles': 'Voertuigen',
    'nav.map': 'Kaart',
    'nav.settings': 'Instellingen',
    
    // Settings
    'settings.title': 'Instellingen',
    'settings.userManagement': 'Gebruikersbeheer',
    'settings.systemSettings': 'Systeem Instellingen',
    'settings.language': 'Taal',
    'settings.logout': 'Uitloggen',
    'settings.about': 'Over Hoppy',
    
    // User Management
    'users.title': 'Gebruikersbeheer',
    'users.create': 'Nieuwe Gebruiker',
    'users.edit': 'Gebruiker Bewerken',
    'users.delete': 'Verwijderen',
    'users.cantDeleteAdmin': 'Administrator accounts kunnen niet worden verwijderd',
    'users.created.title': 'Gebruiker Aangemaakt',
    'users.created.message': 'Gebruiker succesvol aangemaakt! Er is een e-mail verzonden naar {email} met login gegevens.',
    
    // Password Reset
    'forgotPassword.title': 'Wachtwoord Vergeten',
    'forgotPassword.description': 'Voer je emailadres in en we sturen je een link om je wachtwoord opnieuw in te stellen.',
    'forgotPassword.button': 'Wachtwoord vergeten?',
    'forgotPassword.sendReset': 'Reset Link Sturen',
    'forgotPassword.emailSent': 'Email Verzonden',
    'resetPassword.title': 'Nieuw Wachtwoord',
    'resetPassword.subtitle': 'Voer je nieuwe wachtwoord in om je account te beveiligen',
    'resetPassword.button': 'Wachtwoord Wijzigen',
    'resetPassword.success': 'Je wachtwoord is succesvol gewijzigd. Je kunt nu inloggen met je nieuwe wachtwoord.',
    'resetPassword.requirements': 'Wachtwoordvereisten:',
    'resetPassword.requirement1': '• Minimaal 8 karakters',
    'resetPassword.requirement2': '• Minimaal 1 kleine letter',
    'resetPassword.requirement3': '• Minimaal 1 hoofdletter',
    'resetPassword.requirement4': '• Minimaal 1 cijfer',
    
    // Common
    'common.cancel': 'Annuleren',
    'common.save': 'Opslaan',
    'common.delete': 'Verwijderen',
    'common.edit': 'Bewerken',
    'common.loading': 'Laden...',
    'common.error': 'Fout',
    'common.success': 'Succes',
    
    // Languages
    'language.dutch': 'Nederlands',
    'language.english': 'Engels',
    'language.spanish': 'Spaans',
    'language.greek': 'Grieks',
  },
  en: {
    // Dashboard
    'dashboard.title': 'Dashboard',
    'dashboard.welcome': 'Welcome back',
    'dashboard.activeUsers': 'Active Users',
    'dashboard.totalZones': 'Total Zones',
    'dashboard.lowBattery': 'Low Batteries',
    'dashboard.pendingRoutes': 'Pending Routes',
    
    // Navigation
    'nav.dashboard': 'Dashboard',
    'nav.routes': 'Routes',
    'nav.vehicles': 'Vehicles',
    'nav.map': 'Map',
    'nav.settings': 'Settings',
    
    // Settings
    'settings.title': 'Settings',
    'settings.userManagement': 'User Management',
    'settings.systemSettings': 'System Settings',
    'settings.language': 'Language',
    'settings.logout': 'Logout',
    'settings.about': 'About Hoppy',
    
    // User Management
    'users.title': 'User Management',
    'users.create': 'New User',
    'users.edit': 'Edit User',
    'users.delete': 'Delete',
    'users.cantDeleteAdmin': 'Administrator accounts cannot be deleted',
    'users.created.title': 'User Created',
    'users.created.message': 'User successfully created! An email has been sent to {email} with login credentials.',
    
    // Password Reset
    'forgotPassword.title': 'Forgot Password',
    'forgotPassword.description': 'Enter your email address and we\'ll send you a link to reset your password.',
    'forgotPassword.button': 'Forgot password?',
    'forgotPassword.sendReset': 'Send Reset Link',
    'forgotPassword.emailSent': 'Email Sent',
    'resetPassword.title': 'New Password',
    'resetPassword.subtitle': 'Enter your new password to secure your account',
    'resetPassword.button': 'Change Password',
    'resetPassword.success': 'Your password has been successfully changed. You can now log in with your new password.',
    'resetPassword.requirements': 'Password requirements:',
    'resetPassword.requirement1': '• At least 8 characters',
    'resetPassword.requirement2': '• At least 1 lowercase letter',
    'resetPassword.requirement3': '• At least 1 uppercase letter',
    'resetPassword.requirement4': '• At least 1 number',
    
    // Common
    'common.cancel': 'Cancel',
    'common.save': 'Save',
    'common.delete': 'Delete',
    'common.edit': 'Edit',
    'common.loading': 'Loading...',
    'common.error': 'Error',
    'common.success': 'Success',
    
    // Languages
    'language.dutch': 'Dutch',
    'language.english': 'English',
    'language.spanish': 'Spanish',
    'language.greek': 'Greek',
  },
  es: {
    // Dashboard
    'dashboard.title': 'Panel',
    'dashboard.welcome': 'Bienvenido de vuelta',
    'dashboard.activeUsers': 'Usuarios Activos',
    'dashboard.totalZones': 'Zonas Totales',
    'dashboard.lowBattery': 'Baterías Bajas',
    'dashboard.pendingRoutes': 'Rutas Pendientes',
    
    // Navigation
    'nav.dashboard': 'Panel',
    'nav.routes': 'Rutas',
    'nav.vehicles': 'Vehículos',
    'nav.map': 'Mapa',
    'nav.settings': 'Configuración',
    
    // Settings
    'settings.title': 'Configuración',
    'settings.userManagement': 'Gestión de Usuarios',
    'settings.systemSettings': 'Configuración del Sistema',
    'settings.language': 'Idioma',
    'settings.logout': 'Cerrar Sesión',
    'settings.about': 'Acerca de Hoppy',
    
    // User Management
    'users.title': 'Gestión de Usuarios',
    'users.create': 'Nuevo Usuario',
    'users.edit': 'Editar Usuario',
    'users.delete': 'Eliminar',
    'users.cantDeleteAdmin': 'Las cuentas de administrador no se pueden eliminar',
    
    // Common
    'common.cancel': 'Cancelar',
    'common.save': 'Guardar',
    'common.delete': 'Eliminar',
    'common.edit': 'Editar',
    'common.loading': 'Cargando...',
    'common.error': 'Error',
    'common.success': 'Éxito',
    
    // Languages
    'language.dutch': 'Holandés',
    'language.english': 'Inglés',
    'language.spanish': 'Español',
    'language.greek': 'Griego',
  },
  el: {
    // Dashboard
    'dashboard.title': 'Πίνακας Ελέγχου',
    'dashboard.welcome': 'Καλώς ήρθατε πίσω',
    'dashboard.activeUsers': 'Ενεργοί Χρήστες',
    'dashboard.totalZones': 'Συνολικές Ζώνες',
    'dashboard.lowBattery': 'Χαμηλή Μπαταρία',
    'dashboard.pendingRoutes': 'Διαδρομές σε Αναμονή',
    
    // Navigation
    'nav.dashboard': 'Πίνακας',
    'nav.routes': 'Διαδρομές',
    'nav.vehicles': 'Οχήματα',
    'nav.map': 'Χάρτης',
    'nav.settings': 'Ρυθμίσεις',
    
    // Settings
    'settings.title': 'Ρυθμίσεις',
    'settings.userManagement': 'Διαχείριση Χρηστών',
    'settings.systemSettings': 'Ρυθμίσεις Συστήματος',
    'settings.language': 'Γλώσσα',
    'settings.logout': 'Αποσύνδεση',
    'settings.about': 'Σχετικά με το Hoppy',
    
    // User Management
    'users.title': 'Διαχείριση Χρηστών',
    'users.create': 'Νέος Χρήστης',
    'users.edit': 'Επεξεργασία Χρήστη',
    'users.delete': 'Διαγραφή',
    'users.cantDeleteAdmin': 'Οι λογαριασμοί διαχειριστή δεν μπορούν να διαγραφούν',
    
    // Common
    'common.cancel': 'Ακύρωση',
    'common.save': 'Αποθήκευση',
    'common.delete': 'Διαγραφή',
    'common.edit': 'Επεξεργασία',
    'common.loading': 'Φόρτωση...',
    'common.error': 'Σφάλμα',
    'common.success': 'Επιτυχία',
    
    // Languages
    'language.dutch': 'Ολλανδικά',
    'language.english': 'Αγγλικά',
    'language.spanish': 'Ισπανικά',
    'language.greek': 'Ελληνικά',
  },
};

export function LanguageProvider({ children }: LanguageProviderProps) {
  const [currentLanguage, setCurrentLanguage] = useState<Language>('nl');

  useEffect(() => {
    loadSavedLanguage();
  }, []);

  const loadSavedLanguage = async () => {
    try {
      const savedLanguage = await AsyncStorage.getItem('selectedLanguage');
      if (savedLanguage && Object.keys(translations).includes(savedLanguage)) {
        setCurrentLanguage(savedLanguage as Language);
      }
    } catch (error) {
      console.error('Error loading saved language:', error);
    }
  };

  const setLanguage = async (language: Language) => {
    try {
      await AsyncStorage.setItem('selectedLanguage', language);
      setCurrentLanguage(language);
    } catch (error) {
      console.error('Error saving language:', error);
    }
  };

  const t = (key: string, params?: Record<string, any>): string => {
    let translation = translations[currentLanguage][key] || translations.nl[key] || key;
    
    // Simple parameter replacement
    if (params) {
      Object.keys(params).forEach(param => {
        translation = translation.replace(`{{${param}}}`, params[param]);
      });
    }
    
    return translation;
  };

  return (
    <LanguageContext.Provider value={{ currentLanguage, setLanguage, t }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage(): LanguageContextType {
  const context = useContext(LanguageContext);
  if (context === undefined) {
    throw new Error('useLanguage must be used within a LanguageProvider');
  }
  return context;
}

export const getLanguageFlag = (language: Language): string => {
  switch (language) {
    case 'nl': return '🇳🇱';
    case 'en': return '🇬🇧';
    case 'es': return '🇪🇸';
    case 'el': return '🇬🇷';
    default: return '🌐';
  }
};

export const getLanguageName = (language: Language): string => {
  switch (language) {
    case 'nl': return 'Nederlands';
    case 'en': return 'English';
    case 'es': return 'Español';
    case 'el': return 'Ελληνικά';
    default: return 'Unknown';
  }
};
