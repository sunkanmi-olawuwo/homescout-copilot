import { useEffect, useState } from 'react';
import './App.css';

import type {
  BaseRate,
  ConversationSummary,
  CopilotAnswer,
  MainTab,
  MeResponse,
  MortgageEstimateRequest,
  MortgageEstimateResult,
  RightTab,
  Theme,
} from './types';
import { initialRequest, savedComparisons, startPrompts } from './data';
import {
  askCopilot as postCopilotAsk,
  fetchBaseRate,
  fetchHistory,
  fetchMe,
  fetchMortgageEstimate,
  resetSession,
  resumeSession,
} from './api/client';
import { useAuth } from './auth/authContext';
import { useViewport } from './hooks/useViewport';
import { TopBar } from './components/TopBar';
import { WorkspaceBody } from './components/WorkspaceBody';

// The workspace container: owns all state and data fetching, and composes the presentational
// components (header, left rail, conversation, evidence/estimator rail). The layout switches between
// the wide three-column view and a mobile single-column + drawer.
function App() {
  const [theme, setTheme] = useState<Theme>('light');
  const [mainTab, setMainTab] = useState<MainTab>('conversation');
  const [rightTab, setRightTab] = useState<RightTab>('evidence');
  const [navOpen, setNavOpen] = useState(false);

  const [request, setRequest] = useState<MortgageEstimateRequest>(initialRequest);
  const [estimate, setEstimate] = useState<MortgageEstimateResult | null>(null);
  const [estimateError, setEstimateError] = useState<string | null>(null);
  const [baseRate, setBaseRate] = useState<BaseRate | null>(null);
  const [copilotNotice, setCopilotNotice] = useState<string | null>(null);
  const [copilotAnswer, setCopilotAnswer] = useState<CopilotAnswer | null>(null);
  const [copilotQuestion, setCopilotQuestion] = useState<string | null>(null);
  const [isAskingCopilot, setIsAskingCopilot] = useState(false);
  const [isResettingConversation, setIsResettingConversation] = useState(false);
  const [me, setMe] = useState<MeResponse | null>(null);
  const [history, setHistory] = useState<ConversationSummary[]>([]);

  const auth = useAuth();
  const viewport = useViewport();
  const isMobile = viewport < 760;
  const conversationActive = Boolean(copilotQuestion || copilotNotice || copilotAnswer || isAskingCopilot);
  const accessToken = auth.accessToken;

  // Load the signed-in identity + conversation history; clear both on sign-out.
  useEffect(() => {
    if (!auth.isAuthenticated || !accessToken) {
      setMe(null);
      setHistory([]);
      return;
    }
    const controller = new AbortController();
    void fetchMe(accessToken, controller.signal).then(setMe).catch(() => { /* stays anonymous-looking */ });
    void fetchHistory(accessToken, controller.signal)
      .then((data) => setHistory(data.conversations))
      .catch(() => { /* leave history as-is */ });
    return () => controller.abort();
  }, [auth.isAuthenticated, accessToken]);

  const refreshHistory = () => {
    if (!accessToken) return;
    void fetchHistory(accessToken).then((data) => setHistory(data.conversations)).catch(() => {});
  };

  useEffect(() => {
    const controller = new AbortController();
    void fetchBaseRate(controller.signal)
      .then((data) => setBaseRate(data))
      .catch(() => {
        if (!controller.signal.aborted) setBaseRate(null);
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void fetchMortgageEstimate(request, controller.signal)
      .then((data) => {
        setEstimate(data);
        setEstimateError(null);
      })
      .catch((error: Error) => {
        if (controller.signal.aborted) return;
        setEstimate(null);
        setEstimateError(error.message);
      });
    return () => controller.abort();
  }, [request]);

  const depositPercent = request.propertyPrice > 0 ? Math.round((request.deposit / request.propertyPrice) * 100) : 0;

  const askCopilot = async (message: string) => {
    const trimmed = message.trim();
    if (!trimmed || isAskingCopilot) return;
    setCopilotQuestion(trimmed);
    setCopilotNotice(null);
    setCopilotAnswer(null);
    setRightTab('evidence');
    setIsAskingCopilot(true);
    try {
      const { status, answer } = await postCopilotAsk(trimmed, accessToken);
      if (status === 503) {
        setCopilotNotice('HomeScout’s copilot isn’t connected yet — the reasoning agent is still being provisioned. Meanwhile, the Estimator is live: open it from the panel.');
        return;
      }
      if (!answer) {
        setCopilotNotice(`Copilot request failed (${status}). The Estimator remains available.`);
        return;
      }
      setCopilotAnswer(answer);
      // A signed-in turn is owner-stamped — refresh the history list so it appears.
      if (auth.isAuthenticated) refreshHistory();
    } catch {
      setCopilotNotice('Could not reach the HomeScout API. The Estimator uses the same API and will show its own status.');
    } finally {
      setIsAskingCopilot(false);
    }
  };

  const resumeConversation = async (sessionId: string) => {
    if (!accessToken) return;
    try {
      await resumeSession(sessionId, accessToken);
      // The server session cookie now points at the reopened thread; clear the visible thread and
      // invite a follow-up (past messages aren't returned yet — a future backend enhancement).
      setCopilotQuestion(null);
      setCopilotAnswer(null);
      setRightTab('evidence');
      setCopilotNotice('Reopened your saved conversation — ask a follow-up to continue where you left off.');
      if (isMobile) setNavOpen(false);
    } catch {
      setCopilotNotice('Couldn’t reopen that conversation — please try again.');
    }
  };

  const clearConversation = () => {
    setCopilotQuestion(null);
    setCopilotAnswer(null);
    setCopilotNotice(null);
    setRightTab('evidence');
  };

  const resetConversation = async () => {
    if (isResettingConversation) return;
    setIsResettingConversation(true);
    try {
      await resetSession();
      clearConversation();
    } catch {
      setCopilotNotice('Couldn’t start a new conversation — your current one is unchanged. Try again in a moment.');
    } finally {
      setIsResettingConversation(false);
    }
  };

  // On mobile the "Comparison" tab surfaces the Estimator, so opening it also selects that rail tab.
  const selectMainTab = (tab: MainTab) => {
    setMainTab(tab);
    if (tab === 'comparison' && isMobile) setRightTab('estimator');
  };

  return (
    <div className="homescout-app" data-theme={theme} data-viewport={isMobile ? 'mobile' : 'wide'}>
      <TopBar
        theme={theme}
        isMobile={isMobile}
        navOpen={navOpen}
        onToggleNav={() => setNavOpen((open) => !open)}
        onToggleTheme={() => setTheme((current) => (current === 'dark' ? 'light' : 'dark'))}
        authEnabled={auth.authEnabled}
        isReady={auth.isReady}
        isAuthenticated={auth.isAuthenticated}
        userName={me?.name ?? auth.userName}
        onSignIn={auth.signIn}
        onSignOut={auth.signOut}
      />

      <WorkspaceBody
        isMobile={isMobile}
        navOpen={navOpen}
        onCloseNav={() => setNavOpen(false)}
        savedComparisons={savedComparisons}
        showHistory={auth.isAuthenticated}
        history={history}
        onResume={(sessionId) => void resumeConversation(sessionId)}
        mainTab={mainTab}
        onSelectMainTab={selectMainTab}
        rightTab={rightTab}
        onSelectRightTab={setRightTab}
        conversationActive={conversationActive}
        isResettingConversation={isResettingConversation}
        onResetConversation={() => void resetConversation()}
        startPrompts={startPrompts}
        onAsk={(message) => void askCopilot(message)}
        isAskingCopilot={isAskingCopilot}
        copilotNotice={copilotNotice}
        copilotAnswer={copilotAnswer}
        copilotQuestion={copilotQuestion}
        request={request}
        setRequest={setRequest}
        depositPercent={depositPercent}
        estimate={estimate}
        estimateError={estimateError}
        baseRate={baseRate}
      />
    </div>
  );
}

export default App;
