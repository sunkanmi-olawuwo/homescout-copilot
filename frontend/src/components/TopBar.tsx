import type { Theme } from '../types';
import { AccountControls } from './AccountControls';

// The app header: brand, decision-support pill, and global actions (search, theme toggle, account /
// sign-in). On mobile it also carries the navigation drawer toggle.
export function TopBar(props: {
  theme: Theme;
  isMobile: boolean;
  navOpen: boolean;
  onToggleNav: () => void;
  onToggleTheme: () => void;
  authEnabled: boolean;
  isAuthenticated: boolean;
  userName: string | null;
  onSignIn: () => void;
  onSignOut: () => void;
}) {
  const { theme, isMobile, navOpen, onToggleNav, onToggleTheme } = props;

  return (
    <header className="top-bar">
      {isMobile ? (
        <button
          className="icon-button nav-toggle"
          type="button"
          aria-label={navOpen ? 'Close navigation' : 'Open navigation'}
          aria-expanded={navOpen}
          onClick={onToggleNav}
        >
          ☰
        </button>
      ) : null}
      <div className="brand-lockup" aria-label="HomeScout Copilot">
        <span className="brand-mark" aria-hidden="true">H</span>
        <div>
          <strong>HomeScout <span>Copilot</span></strong>
          <small>Greenwich vs Croydon · draft</small>
        </div>
      </div>
      <span className="decision-pill">Decision support, not advice</span>
      <div className="top-actions">
        <button className="toolbar-button" type="button">Search<kbd>⌘K</kbd></button>
        <button
          className="icon-button"
          type="button"
          aria-label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
          title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
          onClick={onToggleTheme}
        >
          {theme === 'dark' ? '☀' : '◐'}
        </button>
        <AccountControls
          authEnabled={props.authEnabled}
          isAuthenticated={props.isAuthenticated}
          userName={props.userName}
          onSignIn={props.onSignIn}
          onSignOut={props.onSignOut}
        />
      </div>
    </header>
  );
}
