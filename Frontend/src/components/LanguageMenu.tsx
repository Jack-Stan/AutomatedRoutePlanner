import React, { useState } from 'react';
import { View, Text, TouchableOpacity, Modal, StyleSheet } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors, HoppyTheme } from '../theme';
import { useLanguage, Language, getLanguageFlag, getLanguageName } from '../contexts/LanguageContext';

interface LanguageMenuProps {
  userInitial?: string;
  userName?: string;
  userRole?: string;
  onLogout?: () => void;
  onSettingsPress?: () => void;
}

export default function LanguageMenu({ userInitial, userName, userRole, onLogout, onSettingsPress }: LanguageMenuProps) {
  const [isMenuVisible, setIsMenuVisible] = useState(false);
  const { currentLanguage, setLanguage, t } = useLanguage();

  const languages: { code: Language; name: string; flag: string }[] = [
    { code: 'nl', name: getLanguageName('nl'), flag: getLanguageFlag('nl') },
    { code: 'en', name: getLanguageName('en'), flag: getLanguageFlag('en') },
    { code: 'es', name: getLanguageName('es'), flag: getLanguageFlag('es') },
    { code: 'el', name: getLanguageName('el'), flag: getLanguageFlag('el') },
  ];

  const handleLanguageSelect = async (language: Language) => {
    await setLanguage(language);
    setIsMenuVisible(false);
  };

  return (
    <>
      <TouchableOpacity 
        style={styles.accountButton}
        onPress={() => setIsMenuVisible(true)}
      >
        <Text style={styles.accountIcon}>
          {userInitial || 'U'}
        </Text>
      </TouchableOpacity>

      <Modal
        visible={isMenuVisible}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setIsMenuVisible(false)}
      >
        <TouchableOpacity 
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setIsMenuVisible(false)}
        >
          <View style={styles.menuContainer}>
            {/* User Info Section */}
            <View style={styles.userSection}>
              <View style={styles.userAvatar}>
                <Text style={styles.userAvatarText}>{userInitial || 'U'}</Text>
              </View>
              <View style={styles.userInfo}>
                <Text style={styles.userNameText}>{userName || 'Unknown User'}</Text>
                <Text style={styles.userRoleText}>{userRole || 'User'}</Text>
              </View>
            </View>

            <View style={styles.divider} />

            {/* Language Section */}
            <View style={styles.section}>
              <Text style={styles.sectionTitle}>{t('settings.language')}</Text>
              {languages.map((lang) => (
                <TouchableOpacity
                  key={lang.code}
                  style={[
                    styles.languageItem,
                    currentLanguage === lang.code && styles.selectedLanguageItem
                  ]}
                  onPress={() => handleLanguageSelect(lang.code)}
                >
                  <Text style={styles.languageFlag}>{lang.flag}</Text>
                  <Text style={[
                    styles.languageText,
                    currentLanguage === lang.code && styles.selectedLanguageText
                  ]}>
                    {lang.name}
                  </Text>
                  {currentLanguage === lang.code && (
                    <Ionicons name="checkmark" size={20} color={HoppyColors.primary} />
                  )}
                </TouchableOpacity>
              ))}
            </View>

            <View style={styles.divider} />

            {/* Quick Actions Section */}
            <View style={styles.section}>
              <TouchableOpacity
                style={styles.actionItem}
                onPress={() => {
                  setIsMenuVisible(false);
                  onSettingsPress?.();
                }}
              >
                <Ionicons name="settings-outline" size={20} color={HoppyColors.gray600} />
                <Text style={styles.actionText}>{t('nav.settings')}</Text>
                <Ionicons name="chevron-forward" size={16} color={HoppyColors.gray400} />
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.actionItem, styles.logoutItem]}
                onPress={() => {
                  setIsMenuVisible(false);
                  onLogout?.();
                }}
              >
                <Ionicons name="log-out-outline" size={20} color={HoppyColors.error} />
                <Text style={[styles.actionText, styles.logoutText]}>{t('settings.logout')}</Text>
              </TouchableOpacity>
            </View>
          </View>
        </TouchableOpacity>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  accountButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: HoppyColors.white,
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: HoppyColors.black,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  accountIcon: {
    fontSize: 16,
    fontWeight: 'bold',
    color: HoppyColors.primary,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-start',
    alignItems: 'flex-end',
    paddingTop: 100, // Adjust based on header height
    paddingRight: 20,
  },
  menuContainer: {
    backgroundColor: HoppyColors.white,
    borderRadius: 12,
    minWidth: 280,
    maxWidth: 320,
    shadowColor: HoppyColors.black,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.25,
    shadowRadius: 12,
    elevation: 8,
  },
  userSection: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: HoppyTheme.spacing.lg,
    backgroundColor: HoppyColors.gray50,
    borderTopLeftRadius: 12,
    borderTopRightRadius: 12,
  },
  userAvatar: {
    width: 50,
    height: 50,
    borderRadius: 25,
    backgroundColor: HoppyColors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: HoppyTheme.spacing.md,
  },
  userAvatarText: {
    fontSize: 20,
    fontWeight: 'bold',
    color: HoppyColors.white,
  },
  userInfo: {
    flex: 1,
  },
  userNameText: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: '600',
    color: HoppyColors.gray800,
    marginBottom: 2,
  },
  userRoleText: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
  },
  divider: {
    height: 1,
    backgroundColor: HoppyColors.gray200,
  },
  section: {
    padding: HoppyTheme.spacing.md,
  },
  sectionTitle: {
    fontSize: HoppyTheme.fontSizes.md,
    fontWeight: '600',
    color: HoppyColors.gray800,
    marginBottom: HoppyTheme.spacing.sm,
  },
  languageItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: HoppyTheme.spacing.sm,
    paddingHorizontal: HoppyTheme.spacing.sm,
    borderRadius: 8,
    marginBottom: 4,
  },
  selectedLanguageItem: {
    backgroundColor: HoppyColors.primary + '10',
  },
  languageFlag: {
    fontSize: 20,
    marginRight: HoppyTheme.spacing.sm,
  },
  languageText: {
    flex: 1,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray700,
  },
  selectedLanguageText: {
    color: HoppyColors.primary,
    fontWeight: '500',
  },
  actionItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: HoppyTheme.spacing.sm,
    paddingHorizontal: HoppyTheme.spacing.sm,
    borderRadius: 8,
    marginBottom: 4,
  },
  logoutItem: {
    marginTop: HoppyTheme.spacing.sm,
  },
  actionText: {
    flex: 1,
    fontSize: HoppyTheme.fontSizes.md,
    color: HoppyColors.gray700,
    marginLeft: HoppyTheme.spacing.sm,
  },
  logoutText: {
    color: HoppyColors.error,
  },
});
