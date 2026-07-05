import { useEffect, useState, type ReactNode } from 'react';
import { AuthProvider, useAuth as useOidc } from 'react-oidc-context';
import type { AuthConfigResponse } from '../types';
import { fetchAuthConfig } from '../api/client';
import { AuthContext, anonymousAuth, type AuthState } from './authContext';

// Bootstraps auth for the app: reads GET /api/config, and — when Keycloak is configured — mounts the
// OIDC provider (Authorization Code + PKCE) and bridges its state into our AuthContext. When auth is
// off (no Keycloak / config still loading / config failed), children run anonymously.
export function AuthGate({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<AuthConfigResponse | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    void fetchAuthConfig(controller.signal)
      .then(setConfig)
      .catch(() => {
        if (!controller.signal.aborted) setFailed(true);
      });
    return () => controller.abort();
  }, []);

  if (failed || !config || !config.authEnabled || !config.authority) {
    return <AuthContext.Provider value={anonymousAuth}>{children}</AuthContext.Provider>;
  }

  const origin = window.location.origin;
  const oidcConfig = {
    authority: config.authority,
    client_id: config.clientId,
    redirect_uri: `${origin}/`,
    post_logout_redirect_uri: `${origin}/`,
    scope: 'openid profile email',
    // Strip the ?code=… from the URL after a successful sign-in redirect.
    onSigninCallback: () => window.history.replaceState({}, document.title, window.location.pathname),
  };

  return (
    <AuthProvider {...oidcConfig}>
      <AuthBridge>{children}</AuthBridge>
    </AuthProvider>
  );
}

// Maps react-oidc-context's state onto our AuthContext, so the rest of the app depends only on our
// small AuthState interface.
function AuthBridge({ children }: { children: ReactNode }) {
  const oidc = useOidc();

  const value: AuthState = {
    authEnabled: true,
    isReady: !oidc.isLoading,
    isAuthenticated: oidc.isAuthenticated,
    userName:
      (oidc.user?.profile.name as string | undefined) ??
      (oidc.user?.profile.preferred_username as string | undefined) ??
      (oidc.user?.profile.email as string | undefined) ??
      null,
    accessToken: oidc.user?.access_token ?? null,
    signIn: () => void oidc.signinRedirect(),
    signOut: () => void oidc.signoutRedirect(),
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
