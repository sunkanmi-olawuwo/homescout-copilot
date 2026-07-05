import type { Dispatch, SetStateAction } from 'react';
import type {
  BaseRate,
  ConversationSummary,
  CopilotAnswer,
  MainTab,
  MortgageEstimateRequest,
  MortgageEstimateResult,
  RightTab,
} from '../types';
import type { SavedComparison, StartPrompt } from '../data';
import { LeftRail } from './LeftRail';
import { ConversationPanel } from './ConversationPanel';
import { EvidenceRail } from './EvidenceRail';

// The three-pane workspace grid (left rail · conversation · evidence/estimator rail) and its
// responsive show/hide rules. Split out of App so App stays a thin state + handlers container;
// this component owns only layout composition and holds no state of its own.
export function WorkspaceBody(props: {
  isMobile: boolean;
  navOpen: boolean;
  onCloseNav: () => void;
  savedComparisons: SavedComparison[];
  showHistory: boolean;
  history: ConversationSummary[];
  onResume: (sessionId: string) => void;
  mainTab: MainTab;
  onSelectMainTab: (tab: MainTab) => void;
  rightTab: RightTab;
  onSelectRightTab: (tab: RightTab) => void;
  conversationActive: boolean;
  isResettingConversation: boolean;
  onResetConversation: () => void;
  startPrompts: StartPrompt[];
  onAsk: (message: string) => void;
  isAskingCopilot: boolean;
  copilotNotice: string | null;
  copilotAnswer: CopilotAnswer | null;
  copilotQuestion: string | null;
  request: MortgageEstimateRequest;
  setRequest: Dispatch<SetStateAction<MortgageEstimateRequest>>;
  depositPercent: number;
  estimate: MortgageEstimateResult | null;
  estimateError: string | null;
  baseRate: BaseRate | null;
}) {
  const { isMobile, navOpen, mainTab, rightTab } = props;
  const showConversation = mainTab === 'conversation' || !isMobile;
  const showRightRail = !isMobile || mainTab === 'comparison';

  return (
    <div className="workspace-grid">
      <LeftRail
        isMobile={isMobile}
        navOpen={navOpen}
        savedComparisons={props.savedComparisons}
        showHistory={props.showHistory}
        history={props.history}
        onResume={props.onResume}
      />
      {isMobile && navOpen ? (
        <button className="rail-scrim" type="button" aria-label="Close navigation" onClick={props.onCloseNav} />
      ) : null}

      <main className="main-workspace" aria-label="HomeScout workspace">
        <div className="workspace-tabs" role="tablist" aria-label="Workspace views">
          <button type="button" role="tab" aria-selected={mainTab === 'conversation'} onClick={() => props.onSelectMainTab('conversation')}>
            Conversation
          </button>
          <button type="button" role="tab" aria-selected={mainTab === 'comparison'} onClick={() => props.onSelectMainTab('comparison')}>
            {isMobile ? 'Estimator' : 'Comparison'}
          </button>
        </div>

        {showConversation ? (
          <ConversationPanel
            conversationActive={props.conversationActive}
            isResettingConversation={props.isResettingConversation}
            onResetConversation={props.onResetConversation}
            startPrompts={props.startPrompts}
            onAsk={props.onAsk}
            isAskingCopilot={props.isAskingCopilot}
            copilotNotice={props.copilotNotice}
            copilotAnswer={props.copilotAnswer}
            copilotQuestion={props.copilotQuestion}
          />
        ) : null}
      </main>

      {showRightRail ? (
        <EvidenceRail
          rightTab={rightTab}
          onSelectTab={props.onSelectRightTab}
          evidence={props.copilotAnswer?.evidence ?? []}
          request={props.request}
          setRequest={props.setRequest}
          depositPercent={props.depositPercent}
          estimate={props.estimate}
          estimateError={props.estimateError}
          baseRate={props.baseRate}
        />
      ) : null}
    </div>
  );
}
