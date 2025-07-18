import React from 'react';
import { TouchableOpacity, Text, StyleSheet, ActivityIndicator, ViewStyle, TextStyle } from 'react-native';
import { HoppyColors, HoppyTheme } from '../theme';

interface HoppyButtonProps {
  title: string;
  onPress: () => void;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
  size?: 'small' | 'medium' | 'large';
  disabled?: boolean;
  loading?: boolean;
  style?: ViewStyle;
  textStyle?: TextStyle;
}

export default function HoppyButton({
  title,
  onPress,
  variant = 'primary',
  size = 'medium',
  disabled = false,
  loading = false,
  style,
  textStyle,
}: HoppyButtonProps) {
  const getButtonStyle = () => {
    const baseStyle: ViewStyle = {
      borderRadius: HoppyTheme.borderRadius.md,
      alignItems: 'center',
      justifyContent: 'center',
      flexDirection: 'row',
    };

    // Size styles
    const sizeStyles = {
      small: {
        paddingHorizontal: 16,
        paddingVertical: 8,
        minHeight: 36,
      },
      medium: {
        paddingHorizontal: 24,
        paddingVertical: 12,
        minHeight: 44,
      },
      large: {
        paddingHorizontal: 32,
        paddingVertical: 16,
        minHeight: 52,
      },
    };

    // Variant styles
    const variantStyles = {
      primary: {
        backgroundColor: disabled ? HoppyColors.gray300 : HoppyColors.primary,
        ...HoppyTheme.shadows.md,
      },
      secondary: {
        backgroundColor: disabled ? HoppyColors.gray300 : HoppyColors.secondary,
        ...HoppyTheme.shadows.md,
      },
      outline: {
        backgroundColor: 'transparent',
        borderWidth: 2,
        borderColor: disabled ? HoppyColors.gray300 : HoppyColors.primary,
      },
      ghost: {
        backgroundColor: 'transparent',
      },
    };

    return [baseStyle, sizeStyles[size], variantStyles[variant], style];
  };

  const getTextStyle = () => {
    const baseTextStyle: TextStyle = {
      fontWeight: '600',
    };

    // Size text styles
    const sizeTextStyles = {
      small: {
        fontSize: HoppyTheme.fontSizes.sm,
      },
      medium: {
        fontSize: HoppyTheme.fontSizes.md,
      },
      large: {
        fontSize: HoppyTheme.fontSizes.lg,
      },
    };

    // Variant text styles
    const variantTextStyles = {
      primary: {
        color: disabled ? HoppyColors.gray500 : HoppyColors.white,
      },
      secondary: {
        color: disabled ? HoppyColors.gray500 : HoppyColors.white,
      },
      outline: {
        color: disabled ? HoppyColors.gray500 : HoppyColors.primary,
      },
      ghost: {
        color: disabled ? HoppyColors.gray500 : HoppyColors.primary,
      },
    };

    return [baseTextStyle, sizeTextStyles[size], variantTextStyles[variant], textStyle];
  };

  return (
    <TouchableOpacity
      style={getButtonStyle()}
      onPress={onPress}
      disabled={disabled || loading}
      activeOpacity={0.8}
    >
      {loading && (
        <ActivityIndicator
          size="small"
          color={variant === 'primary' || variant === 'secondary' ? HoppyColors.white : HoppyColors.primary}
          style={{ marginRight: 8 }}
        />
      )}
      <Text style={getTextStyle()}>{title}</Text>
    </TouchableOpacity>
  );
}
