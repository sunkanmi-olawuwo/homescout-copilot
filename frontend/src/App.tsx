import { useEffect, useState } from 'react';
import './App.css';

import type {
  BaseRate,
  CopilotAnswer,
  MainTab,
  MortgageEstimateRequest,
  MortgageEstimateResult,
  RightTab,
  Theme,
} from './types';
import { initialRequest, savedComparisons, startPrompts } from './data';
import { askCopilot as postCopilotAsk, fetchBaseRate, fetchMortgageEstimate, resetSession } from './api/client';
import { useViewport } from './hooks/useViewport';
import { TopBar } from './components/TopBar';
import { LeftRail } from './components/LeftRail';
import { ConversationPanel } from './components/ConversationPanel';
import { EvidenceRail } from './components/EvidenceRail';

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

  const viewport = useViewport();
  const isMobile = viewport < 760;
  const conversationActive = Boolean(copilotQuestion || copilotNotice || copilotAnswer || isAskingCopilot);

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
      const { status, answer } = await postCopilotAsk(trimmed);
      if (status === 503) {
        setCopilotNotice('HomeScout’s copilot isn’t connected yet — the reasoning agent is still being provisioned. Meanwhile, the Estimator is live: open it from the panel.');
        return;
      }
      if (!answer) {
        setCopilotNotice(`Copilot request failed (${status}). The Estimator remains available.`);
        return;
      }
      setCopilotAnswer(answer);
    } catch {
      setCopilotNotice('Could not reach the HomeScout API. The Estimator uses the same API and will show its own status.');
    } finally {
      setIsAskingCopilot(false);
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

  return (
    <div className="homescout-app" data-theme={theme} data-viewport={isMobile ? 'mobile' : 'wide'}>
      <TopBar
        theme={theme}
        isMobile={isMobile}
        navOpen={navOpen}
        onToggleNav={() => setNavOpen((open) => !open)}
        onToggleTheme={() => setTheme((current) => (current === 'dark' ? 'light' : 'dark'))}
      />

      <div className="workspace-grid">
        <LeftRail isMobile={isMobile} navOpen={navOpen} savedComparisons={savedComparisons} />
        {isMobile && navOpen ? (
          <button className="rail-scrim" type="button" aria-label="Close navigation" onClick={() => setNavOpen(false)} />
        ) : null}

        <main className="main-workspace" aria-label="HomeScout workspace">
          <div className="workspace-tabs" role="tablist" aria-label="Workspace views">
            <button type="button" role="tab" aria-selected={mainTab === 'conversation'} onClick={() => setMainTab('conversation')}>
              Conversation
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={mainTab === 'comparison'}
              onClick={() => {
                setMainTab('comparison');
                if (isMobile) setRightTab('estimator');
              }}
            >
              {isMobile ? 'Estimator' : 'Comparison'}
            </button>
          </div>

          {mainTab === 'conversation' || !isMobile ? (
            <ConversationPanel
              conversationActive={conversationActive}
              isResettingConversation={isResettingConversation}
              onResetConversation={() => void resetConversation()}
              startPrompts={startPrompts}
              onAsk={(message) => void askCopilot(message)}
              isAskingCopilot={isAskingCopilot}
              copilotNotice={copilotNotice}
              copilotAnswer={copilotAnswer}
              copilotQuestion={copilotQuestion}
            />
          ) : null}
        </main>

        {(!isMobile || mainTab === 'comparison') ? (
          <EvidenceRail
            rightTab={rightTab}
            onSelectTab={setRightTab}
            evidence={copilotAnswer?.evidence ?? []}
            request={request}
            setRequest={setRequest}
            depositPercent={depositPercent}
            estimate={estimate}
            estimateError={estimateError}
            baseRate={baseRate}
          />
        ) : null}
      </div>
    </div>
  );
}

export default App;
