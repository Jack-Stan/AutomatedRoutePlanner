import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { HoppyColors, HoppyTheme } from '../theme';

interface HoppyCardProps {
  children: React.ReactNode;
  style?: ViewStyle;
  padding?: 'none' | 'small' | 'medium' | 'large';
  elevation?: 'low' | 'medium' | 'high';
  borderColor?: string;
}

export default function HoppyCard({ 
  children, 
  style, 
  padding = 'medium',
  elevation = 'medium',
  borderColor
}: HoppyCardProps) {
  const getPadding = () => {
    switch (padding) {
      case 'none': return 0;
      case 'small': return HoppyTheme.spacing.sm;
      case 'medium': return HoppyTheme.spacing.md;
      case 'large': return HoppyTheme.spacing.lg;
      default: return HoppyTheme.spacing.md;
    }
  };

  const getElevation = () => {
    switch (elevation) {
      case 'low': return HoppyTheme.shadows.sm;
      case 'medium': return HoppyTheme.shadows.md;
      case 'high': return HoppyTheme.shadows.lg;
      default: return HoppyTheme.shadows.md;
    }
  };

  const cardStyle = [
    styles.card,
    {
      padding: getPadding(),
      borderLeftColor: borderColor || HoppyColors.primary,
      borderLeftWidth: borderColor ? 4 : 0,
    },
    getElevation(),
    style,
  ];

  return (
    <View style={cardStyle}>
      {children}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: HoppyColors.white,
    borderRadius: HoppyTheme.borderRadius.lg,
    marginBottom: HoppyTheme.spacing.md,
  },
});
