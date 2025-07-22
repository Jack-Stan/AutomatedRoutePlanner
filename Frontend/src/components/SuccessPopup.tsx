import React, { useEffect } from 'react';
import {
  View,
  Text,
  Modal,
  StyleSheet,
  TouchableWithoutFeedback,
  Animated,
  Dimensions,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { HoppyColors } from '../theme';

interface SuccessPopupProps {
  visible: boolean;
  title: string;
  message: string;
  onClose: () => void;
  autoCloseDelay?: number; // Auto close after X milliseconds (optional)
}

const SuccessPopup: React.FC<SuccessPopupProps> = ({
  visible,
  title,
  message,
  onClose,
  autoCloseDelay = 3000,
}) => {
  const opacity = new Animated.Value(0);
  const scale = new Animated.Value(0.8);

  useEffect(() => {
    if (visible) {
      // Animate in
      Animated.parallel([
        Animated.timing(opacity, {
          toValue: 1,
          duration: 250,
          useNativeDriver: true,
        }),
        Animated.spring(scale, {
          toValue: 1,
          useNativeDriver: true,
          tension: 100,
          friction: 8,
        }),
      ]).start();

      // Auto close if autoCloseDelay is set
      if (autoCloseDelay > 0) {
        const timeout = setTimeout(() => {
          handleClose();
        }, autoCloseDelay);

        return () => clearTimeout(timeout);
      }
    }
  }, [visible]);

  const handleClose = () => {
    Animated.parallel([
      Animated.timing(opacity, {
        toValue: 0,
        duration: 200,
        useNativeDriver: true,
      }),
      Animated.timing(scale, {
        toValue: 0.8,
        duration: 200,
        useNativeDriver: true,
      }),
    ]).start(() => {
      onClose();
    });
  };

  if (!visible) return null;

  return (
    <Modal
      visible={visible}
      transparent
      animationType="none"
      statusBarTranslucent
      onRequestClose={handleClose}
    >
      <TouchableWithoutFeedback onPress={handleClose}>
        <View style={styles.overlay}>
          <TouchableWithoutFeedback>
            <Animated.View
              style={[
                styles.popup,
                {
                  opacity,
                  transform: [{ scale }],
                },
              ]}
            >
              <View style={styles.iconContainer}>
                <Ionicons
                  name="checkmark-circle"
                  size={48}
                  color={HoppyColors.success}
                />
              </View>
              
              <Text style={styles.title}>{title}</Text>
              <Text style={styles.message}>{message}</Text>

              <View style={styles.progressBar}>
                <Animated.View
                  style={[
                    styles.progressFill,
                    {
                      width: autoCloseDelay > 0 ? `${(autoCloseDelay / autoCloseDelay) * 100}%` : '0%',
                    },
                  ]}
                />
              </View>
            </Animated.View>
          </TouchableWithoutFeedback>
        </View>
      </TouchableWithoutFeedback>
    </Modal>
  );
};

const { width } = Dimensions.get('window');

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 20,
  },
  popup: {
    backgroundColor: 'white',
    borderRadius: 16,
    padding: 24,
    alignItems: 'center',
    maxWidth: width * 0.85,
    minWidth: width * 0.7,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 10,
    },
    shadowOpacity: 0.25,
    shadowRadius: 20,
    elevation: 10,
  },
  iconContainer: {
    marginBottom: 16,
  },
  title: {
    fontSize: 20,
    fontWeight: '600',
    color: HoppyColors.gray800,
    textAlign: 'center',
    marginBottom: 8,
  },
  message: {
    fontSize: 16,
    color: HoppyColors.gray600,
    textAlign: 'center',
    lineHeight: 22,
    marginBottom: 20,
  },
  progressBar: {
    width: '100%',
    height: 3,
    backgroundColor: HoppyColors.gray200,
    borderRadius: 2,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    backgroundColor: HoppyColors.success,
    borderRadius: 2,
  },
});

export default SuccessPopup;
