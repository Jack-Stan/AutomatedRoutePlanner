import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { HoppyColors, HoppyTheme } from '../theme';

export default function VehicleDetailsScreen() {
  return (
    <View style={styles.container}>
      <Text style={styles.text}>Voertuig Details Screen</Text>
      <Text style={styles.subtext}>Hier komen de details van het geselecteerde voertuig</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: HoppyColors.gray50,
  },
  text: {
    fontSize: HoppyTheme.fontSizes.lg,
    fontWeight: 'bold',
    color: HoppyColors.gray800,
  },
  subtext: {
    fontSize: HoppyTheme.fontSizes.sm,
    color: HoppyColors.gray600,
    marginTop: HoppyTheme.spacing.sm,
    textAlign: 'center',
  },
});
