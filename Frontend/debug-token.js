// Debug script om JWT token te inspecteren
const jwt = require('jsonwebtoken');

// Vervang deze met je echte token uit AsyncStorage
const token = "YOUR_JWT_TOKEN_HERE";

try {
    const decoded = jwt.decode(token);
    console.log('JWT Claims:', JSON.stringify(decoded, null, 2));
} catch (error) {
    console.error('Error decoding token:', error);
}
