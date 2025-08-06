# TRKart Frontend

A modern React + TypeScript frontend application for the TRKart digital banking system.

## Features

- 🔐 **Authentication**: Login/Register with JWT token support
- 💳 **User Cards Management**: View and manage payment cards
- 💰 **Transaction History**: View transaction history by customer or card
- 🍪 **Session Management**: Remember me functionality with secure cookies
- 📱 **Responsive Design**: Mobile-first design with Tailwind CSS
- 🔄 **Auto-login**: Persistent sessions with cookie-based authentication

## Tech Stack

- **React 18** with TypeScript
- **Vite** for fast development and building
- **React Router** for navigation
- **Axios** for API communication
- **Tailwind CSS** for styling
- **Context API** for state management

## Project Structure

```
src/
├── components/          # Reusable UI components
├── pages/              # Page components
├── services/           # API services
├── contexts/           # React contexts for state management
├── types/              # TypeScript type definitions
├── utils/              # Utility functions
└── layouts/            # Layout components
```

## Getting Started

### Prerequisites

- Node.js 16+ 
- npm or yarn
- Backend API running on `http://localhost:5000`

### Installation

1. Clone the repository
2. Navigate to the frontend directory:
   ```bash
   cd TRKartFrontend
   ```

3. Install dependencies:
   ```bash
   npm install
   ```

4. Create environment file:
   ```bash
   cp env.example .env
   ```

5. Update the `.env` file with your API configuration:
   ```
   VITE_API_BASE_URL=http://localhost:5000/api
   VITE_APP_NAME=TRKart
   ```

### Development

Start the development server:
```bash
npm run dev
```

The application will be available at `http://localhost:3000`

### Building for Production

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

## API Integration

The frontend is configured to work with the TRKart backend API. Key endpoints:

- **Authentication**: `/api/Auth/login`, `/api/Auth/register`, `/api/Auth/logout`
- **User Cards**: `/api/UserCard/*`
- **Transactions**: `/api/Transaction/*`

## Authentication Flow

1. **Login**: User enters credentials with optional "Remember Me"
2. **Token Storage**: JWT token stored in localStorage
3. **Auto-login**: Session cookie provides persistent authentication
4. **API Calls**: Axios interceptors automatically include JWT token
5. **Logout**: Clears both localStorage and session cookie

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `VITE_API_BASE_URL` | Backend API base URL | `http://localhost:5000/api` |
| `VITE_APP_NAME` | Application name | `TRKart` |

## Contributing

1. Follow the existing code structure
2. Use TypeScript for all new code
3. Follow the established naming conventions
4. Test your changes thoroughly

## License

This project is part of the TRKart digital banking system. 