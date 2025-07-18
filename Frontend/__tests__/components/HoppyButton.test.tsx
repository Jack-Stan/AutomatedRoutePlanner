import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import { HoppyButton } from '../../src/components/HoppyButton';

describe('HoppyButton', () => {
  test('renders correctly with title', () => {
    const { getByText } = render(
      <HoppyButton title="Test Button" onPress={() => {}} />
    );
    
    expect(getByText('Test Button')).toBeTruthy();
  });

  test('calls onPress when pressed', () => {
    const mockOnPress = jest.fn();
    const { getByText } = render(
      <HoppyButton title="Test Button" onPress={mockOnPress} />
    );
    
    fireEvent.press(getByText('Test Button'));
    expect(mockOnPress).toHaveBeenCalledTimes(1);
  });

  test('renders with primary variant by default', () => {
    const { getByTestId } = render(
      <HoppyButton 
        title="Test Button" 
        onPress={() => {}} 
        testID="test-button"
      />
    );
    
    const button = getByTestId('test-button');
    expect(button).toBeTruthy();
  });

  test('renders with secondary variant', () => {
    const { getByTestId } = render(
      <HoppyButton 
        title="Test Button" 
        onPress={() => {}} 
        variant="secondary"
        testID="test-button"
      />
    );
    
    const button = getByTestId('test-button');
    expect(button).toBeTruthy();
  });

  test('renders with outline variant', () => {
    const { getByTestId } = render(
      <HoppyButton 
        title="Test Button" 
        onPress={() => {}} 
        variant="outline"
        testID="test-button"
      />
    );
    
    const button = getByTestId('test-button');
    expect(button).toBeTruthy();
  });

  test('renders with large size', () => {
    const { getByTestId } = render(
      <HoppyButton 
        title="Test Button" 
        onPress={() => {}} 
        size="large"
        testID="test-button"
      />
    );
    
    const button = getByTestId('test-button');
    expect(button).toBeTruthy();
  });

  test('renders with small size', () => {
    const { getByTestId } = render(
      <HoppyButton 
        title="Test Button" 
        onPress={() => {}} 
        size="small"
        testID="test-button"
      />
    );
    
    const button = getByTestId('test-button');
    expect(button).toBeTruthy();
  });

  test('renders as disabled when disabled prop is true', () => {
    const mockOnPress = jest.fn();
    const { getByTestId } = render(
      <HoppyButton 
        title="Test Button" 
        onPress={mockOnPress}
        disabled={true}
        testID="test-button"
      />
    );
    
    const button = getByTestId('test-button');
    fireEvent.press(button);
    
    // onPress should not be called when disabled
    expect(mockOnPress).not.toHaveBeenCalled();
  });

  test('renders with loading state', () => {
    const { getByTestId } = render(
      <HoppyButton 
        title="Test Button" 
        onPress={() => {}}
        loading={true}
        testID="test-button"
      />
    );
    
    const button = getByTestId('test-button');
    expect(button).toBeTruthy();
  });

  test('does not call onPress when loading', () => {
    const mockOnPress = jest.fn();
    const { getByTestId } = render(
      <HoppyButton 
        title="Test Button" 
        onPress={mockOnPress}
        loading={true}
        testID="test-button"
      />
    );
    
    const button = getByTestId('test-button');
    fireEvent.press(button);
    
    // onPress should not be called when loading
    expect(mockOnPress).not.toHaveBeenCalled();
  });
});
