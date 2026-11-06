# Hoppy Route Manager - Frontend

Een cross-platform applicatie gebouwd met React Native en Expo voor het beheren van routes, voertuigen en zones. Werkt op iOS, Android en web.

## Functionaliteiten

- 📱 **Cross-platform**: iOS, Android, en Web
- 🗺️ **Route Management**: Routes maken, bewerken en optimaliseren
- 🚗 **Voertuig Tracking**: Voertuigen beheren en tracken
- 🎯 **Zone Management**: Geografische zones definiëren
- 📊 **Dashboard**: Overzicht van alle activiteiten
- 🌐 **Real-time updates**: Live data van .NET Core API

## Tech Stack

- **React Native** met Expo
- **TypeScript** voor type safety
- **Redux Toolkit** voor state management
- **React Navigation** voor navigatie
- **Axios** voor API communicatie
- **React Native Maps** voor kaart functionaliteiten

## Vereisten

- Node.js (versie 18 of hoger)
- npm of yarn
- Expo CLI
- .NET Core API server (Backend)

## Installatie

1. **Clone de repository en navigeer naar de frontend folder:**
   ```bash
   cd Frontend
   ```

2. **Installeer dependencies:**
   ```bash
   npm install
   ```

3. **Start de development server:**
   ```bash
   npm start
   ```

## Platform-specifieke commands

### iOS Simulator
```bash
npm run ios
```

### Android Emulator
```bash
npm run android
```

### Web Browser
```bash
npm run web
```

## Project Structuur

```
src/
├── components/          # Herbruikbare UI components
├── navigation/          # Navigation setup
├── screens/            # App screens
│   ├── DashboardScreen.tsx
│   ├── RoutesScreen.tsx
│   ├── VehiclesScreen.tsx
│   ├── MapScreen.tsx
│   └── SettingsScreen.tsx
├── services/           # API services
│   └── api.ts
├── store/              # Redux store
│   ├── index.ts
│   ├── hooks.ts
│   └── slices/
│       ├── routesSlice.ts
│       ├── vehiclesSlice.ts
│       └── zonesSlice.ts
└── types/              # TypeScript types
    └── index.ts
```

## API Configuratie

De app verwacht dat de .NET Core API draait op:
- **Development**: `http://localhost:5033/api`
- **Production**: Configureer in `src/services/api.ts`

## Ontwikkeling

### Nieuwe screens toevoegen
1. Maak een nieuwe file in `src/screens/`
2. Voeg de screen toe aan `src/navigation/RootNavigator.tsx`
3. Update de types in `src/types/index.ts`

### API endpoints gebruiken
```typescript
import { apiService } from '../services/api';

// Gebruik in components of async thunks
const routes = await apiService.getRoutes();
```

### Redux state gebruiken
```typescript
import { useAppSelector, useAppDispatch } from '../store/hooks';

const routes = useAppSelector(state => state.routes.routes);
const dispatch = useAppDispatch();
```

## Building voor productie

### Expo Development Build
```bash
expo build:ios
expo build:android
```

### Expo Application Services (EAS)
```bash
eas build --platform ios
eas build --platform android
```

### Web deployment
```bash
expo build:web
```

## Troubleshooting

### Metro bundler problemen
```bash
npx expo start --clear
```

### Package problemen
```bash
rm -rf node_modules
npm install
```

### TypeScript errors
Zorg ervoor dat alle dependencies up-to-date zijn:
```bash
npm update
```

## Volgende stappen

1. **Route optimalisatie algoritmes** implementeren
2. **Real-time tracking** toevoegen met WebSocket
3. **Push notifications** voor route updates
4. **Offline support** met AsyncStorage
5. **Advanced mapping** met custom markers en routes
6. **User authentication** en authorization
7. **Advanced analytics** en reporting

## Backend Integratie

Deze frontend werkt samen met de .NET Core API. Zorg ervoor dat de backend draait voordat je de app test:

1. Start de .NET Core API
2. Verifieer dat de API bereikbaar is op `http://localhost:5033`
3. Test API endpoints met Postman of browser
4. Start de React Native app

## Ondersteuning

Voor vragen of problemen, check:
- Expo documentatie: https://docs.expo.dev
- React Native documentatie: https://reactnative.dev
- Redux Toolkit: https://redux-toolkit.js.org


Inloggegevens:
Er zijn geen standaard-wachtwoorden. Log in als `admin` met het wachtwoord dat je in de backend instelt via `Seed__AdminPassword` (zie de hoofd-README, sectie "Secrets and configuration").