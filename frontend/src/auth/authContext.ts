import { createContext, useContext } from 'react';

// A thin auth abstraction so components never couple to a specific OIDC library — mirrors the
// backend's "internal id, swappable IdP" philosophy and keeps tests simple (provide a fake value).
export interface AuthState {
  /** True when Keycloak is configured (from GET /api/config) — gates the sign-in UI. */
  authEnabled: boolean;
  /** True once config + the auth library have settled (avoid flashing sign-in mid-init). */
  isReady: boolean;
  isAuthenticated: boolean;
  userName: string | null;
  accessToken: string | null;
  signIn: () => void;
  signOut: () => void;
}

// Default: anonymous, auth off. Components rendered without an AuthGate (e.g. in unit tests) simply
// behave as the anonymous app.
export const anonymousAuth: AuthState = {
  authEnabled: false,
  isReady: true,
  isAuthenticated: false,
  userName: null,
  accessToken: null,
  signIn: () => {},
  signOut: () => {},
};

export const AuthContext = createContext<AuthState>(anonymousAuth);

export function useAuth(): AuthState {
  return useContext(AuthContext);
}
