import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { AuthProvider, useAuth as useOidc } from 'react-oidc-context';
import { UserManager, type UserManagerSettings } from 'oidc-client-ts';
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

  return <ConfiguredAuth config={config}>{children}</ConfiguredAuth>;
}

// Owns the UserManager so we can preload the OIDC discovery metadata before the user clicks — a cold
// metadata fetch on the first signinRedirect is what previously made the first Sign in click a no-op.
function ConfiguredAuth({ config, children }: { config: AuthConfigResponse; children: ReactNode }) {
  const origin = window.location.origin;

  const userManager = useMemo(() => {
    const settings: UserManagerSettings = {
      authority: config.authority!,
      client_id: config.clientId,
      redirect_uri: `${origin}/`,
      post_logout_redirect_uri: `${origin}/`,
      scope: 'openid profile email',
    };
    return new UserManager(settings);
  }, [config.authority, config.clientId, origin]);

  const [metadataReady, setMetadataReady] = useState(false);
  useEffect(() => {
    let active = true;
    // Warm the discovery document + JWKS so the first Sign in redirect is instant. On failure we
    // still mark ready so the button isn't stuck — the click will then surface the real error.
    void userManager.metadataService
      .getMetadata()
      .catch(() => { /* surfaced on click */ })
      .finally(() => { if (active) setMetadataReady(true); });
    return () => { active = false; };
  }, [userManager]);

  return (
    <AuthProvider
      userManager={userManager}
      onSigninCallback={() => window.history.replaceState({}, document.title, window.location.pathname)}
    >
      <AuthBridge metadataReady={metadataReady}>{children}</AuthBridge>
    </AuthProvider>
  );
}

// Maps react-oidc-context's state onto our AuthContext, so the rest of the app depends only on our
// small AuthState interface. isReady gates the sign-in control until both init and metadata settle.
function AuthBridge({ metadataReady, children }: { metadataReady: boolean; children: ReactNode }) {
  const oidc = useOidc();

  const value: AuthState = {
    authEnabled: true,
    isReady: metadataReady && !oidc.isLoading,
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
