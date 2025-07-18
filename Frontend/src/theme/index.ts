// Hoppy Brand Colors & Theme (officiële kleuren van behoppy.eu)
export const HoppyColors = {
  // Primary brand colors - Hoppy gebruikt groen/turquoise kleuren
  primary: '#39C5A3', // Hoppy turquoise/groen
  primaryDark: '#2FA888', // Donkerder groen
  primaryLight: '#44DBAC', // Lichter turquoise
  primaryAccent: '#46DAA5', // Hoppy groen accent
  primaryAlt: '#40DBB7', // Alternatieve Hoppy kleur
  
  // Secondary colors
  secondary: '#2C3E50', // Donkergrijs/blauw
  secondaryLight: '#34495E',
  
  // Supporting colors
  success: '#38A169', // Groen voor voltooide routes
  warning: '#D69E2E', // Geel voor waarschuwingen
  error: '#E53E3E', // Rood voor fouten
  info: '#3182CE', // Blauw voor info
  
  // Neutral colors
  white: '#FFFFFF',
  black: '#1A202C',
  gray50: '#F7FAFC',
  gray100: '#EDF2F7',
  gray200: '#E2E8F0',
  gray300: '#CBD5E0',
  gray400: '#A0AEC0',
  gray500: '#718096',
  gray600: '#4A5568',
  gray700: '#2D3748',
  gray800: '#1A202C',
  gray900: '#171923',
  
  // Status colors for routes/vehicles
  pending: '#44DBAC', // Licht turquoise voor pending
  active: '#39C5A3', // Hoppy primary voor actief
  completed: '#46DAA5', // Hoppy groen voor voltooid
  cancelled: '#E53E3E', // Rood voor geannuleerd
  
  // Battery level colors
  batteryHigh: '#38A169', // Groen >60%
  batteryMedium: '#D69E2E', // Geel 20-60%
  batteryLow: '#E53E3E', // Rood <20%
  batteryCritical: '#C53030', // Donkerrood <10%
};

export const HoppyTheme = {
  colors: HoppyColors,
  
  // Typography
  fonts: {
    regular: 'System',
    medium: 'System',
    bold: 'System',
    light: 'System',
  },
  
  fontSizes: {
    xs: 12,
    sm: 14,
    md: 16,
    lg: 18,
    xl: 20,
    xxl: 24,
    xxxl: 32,
  },
  
  // Spacing
  spacing: {
    xs: 4,
    sm: 8,
    md: 16,
    lg: 24,
    xl: 32,
    xxl: 48,
  },
  
  // Border radius
  borderRadius: {
    sm: 4,
    md: 8,
    lg: 12,
    xl: 16,
    full: 999,
  },
  
  // Shadows - Web compatible (using boxShadow)
  shadows: {
    sm: {
      boxShadow: '0px 1px 2px rgba(0, 0, 0, 0.1)',
    },
    md: {
      boxShadow: '0px 2px 4px rgba(0, 0, 0, 0.1)',
    },
    lg: {
      boxShadow: '0px 4px 8px rgba(0, 0, 0, 0.15)',
    },
  },
};

// Helper functions
export const getStatusColor = (status: string) => {
  switch (status.toLowerCase()) {
    case 'pending':
    case 'in afwachting':
      return HoppyColors.pending;
    case 'active':
    case 'inprogress':
    case 'onderweg':
      return HoppyColors.active;
    case 'completed':
    case 'voltooid':
      return HoppyColors.completed;
    case 'cancelled':
    case 'geannuleerd':
      return HoppyColors.cancelled;
    default:
      return HoppyColors.gray400;
  }
};

export const getBatteryColor = (batteryLevel: number) => {
  if (batteryLevel >= 60) return HoppyColors.batteryHigh;
  if (batteryLevel >= 20) return HoppyColors.batteryMedium;
  if (batteryLevel >= 10) return HoppyColors.batteryLow;
  return HoppyColors.batteryCritical;
};

export default HoppyTheme;
