import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { HoppyColors } from '../theme';

interface HoppyLogoProps {
  size?: 'small' | 'medium' | 'large';
  showText?: boolean;
}

export default function HoppyLogo({ size = 'medium', showText = true }: HoppyLogoProps) {
  const logoSize = {
    small: 40,
    medium: 80,
    large: 120,
  }[size];

  const textSize = {
    small: 16,
    medium: 24,
    large: 32,
  }[size];

  return (
    <View style={styles.container}>
      {/* Tijdelijke logo met tekst - later vervangen door echte logo */}
      <View style={[styles.logoContainer, { width: logoSize, height: logoSize }]}>
        <Text style={[styles.logoText, { fontSize: textSize * 0.6 }]}>H</Text>
      </View>
      
      {showText && (
        <Text style={[styles.brandText, { fontSize: textSize }]}>
          Hoppy
        </Text>
      )}
      
      {showText && size !== 'small' && (
        <Text style={styles.tagline}>Don't worry, be Hoppy!</Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  logoContainer: {
    backgroundColor: HoppyColors.primary,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 8,
    boxShadow: '0px 4px 8px rgba(0, 0, 0, 0.2)',
    elevation: 5,
  },
  logoText: {
    color: '#FFFFFF',
    fontWeight: 'bold',
  },
  brandText: {
    color: HoppyColors.primary,
    fontWeight: 'bold',
    marginBottom: 4,
  },
  tagline: {
    color: HoppyColors.secondary,
    fontSize: 14,
    fontStyle: 'italic',
    textAlign: 'center',
  },
});
