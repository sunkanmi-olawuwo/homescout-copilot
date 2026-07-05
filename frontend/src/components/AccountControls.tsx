// Header account area. Anonymous (no Keycloak) keeps the placeholder avatar; with auth on it shows a
// Sign in button, or the signed-in name + Sign out. Sign in is disabled until auth is ready (OIDC
// metadata warmed), so the first click always triggers the redirect.
export function AccountControls(props: {
  authEnabled: boolean;
  isReady: boolean;
  isAuthenticated: boolean;
  userName: string | null;
  onSignIn: () => void;
  onSignOut: () => void;
}) {
  const { authEnabled, isReady, isAuthenticated, userName, onSignIn, onSignOut } = props;

  if (!authEnabled) {
    return <button className="avatar-button" type="button" aria-label="Account">AO</button>;
  }

  if (!isAuthenticated) {
    return (
      <button className="toolbar-button sign-in" type="button" onClick={onSignIn} disabled={!isReady}>
        {isReady ? 'Sign in' : 'Loading…'}
      </button>
    );
  }

  return (
    <div className="account-signed-in">
      <span className="account-name" title={userName ?? undefined}>{userName ?? 'Signed in'}</span>
      <button className="toolbar-button sign-out" type="button" onClick={onSignOut}>
        Sign out
      </button>
    </div>
  );
}
