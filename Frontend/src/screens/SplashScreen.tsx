import React, { useEffect } from 'react';
import { View, StyleSheet, Dimensions } from 'react-native';
import { HoppyLogo } from '../components';
import { HoppyColors } from '../theme';

interface SplashScreenProps {
  onFinish: () => void;
}

export default function SplashScreen({ onFinish }: SplashScreenProps) {
  useEffect(() => {
    const timer = setTimeout(() => {
      onFinish();
    }, 2000); // Show splash for 2 seconds

    return () => clearTimeout(timer);
  }, [onFinish]);

  return (
    <View style={styles.container}>
      <HoppyLogo size="large" showText={true} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: HoppyColors.white,
    justifyContent: 'center',
    alignItems: 'center',
  },
});
