import { useAuth0 } from '@auth0/auth0-react';
import React from 'react';
import { ChatPane } from './components/ChatPane';
import { PreviewPane } from './components/PreviewPane';
import { TavusPane } from './components/TavusPane';
import { useApiClient } from './hooks/useApiClient';
import type { AgentResponse, ChatMessage, Conversation, MachineInfo } from './types';

const apiBase = import.meta.env.VITE_API_BASE_URL || '/api';

function buildMessage(role: ChatMessage['role'], content: string): ChatMessage {
  return { id: crypto.randomUUID(), role, content, createdAt: new Date().toISOString() };
}

function loadMemory(sub?: string): ChatMessage[] {
  if (!sub) return [];
  const stored = localStorage.getItem(`memory-${sub}`);
  if (!stored) return [];
  try {
    return JSON.parse(stored);
  } catch {
    return [];
  }
}

function persistMemory(sub: string | undefined, messages: ChatMessage[]) {
  if (!sub) return;
  localStorage.setItem(`memory-${sub}`, JSON.stringify(messages));
}

const App: React.FC = () => {
  const { isAuthenticated, isLoading, loginWithRedirect, logout, user } = useAuth0();
  const api = useApiClient();
  const [conversation, setConversation] = React.useState<Conversation | undefined>();
  const [conversationExpired, setConversationExpired] = React.useState(false);
  const [messages, setMessages] = React.useState<ChatMessage[]>(() => loadMemory(user?.sub));
  const [machine, setMachine] = React.useState<MachineInfo | undefined>();
  const [streamingLog, setStreamingLog] = React.useState('');
  const [sessionId, setSessionId] = React.useState<string | undefined>();
  const logStreamRef = React.useRef<EventSource | null>(null);

  React.useEffect(() => {
    setMessages(loadMemory(user?.sub));
  }, [user?.sub]);

  React.useEffect(() => {
    persistMemory(user?.sub, messages);
  }, [messages, user?.sub]);

  React.useEffect(() => {
    return () => {
      logStreamRef.current?.close();
      logStreamRef.current = null;
    };
  }, []);

  const startConversation = React.useCallback(async () => {
    const response = await api.post(`${apiBase}/tavus/conversations/start`);
    const data = response.data as { conversationId: string; conversationUrl: string; startedAt: string; expiresAt: string };
    setConversation({ id: data.conversationId, url: data.conversationUrl, startedAt: data.startedAt, expiresAt: data.expiresAt });
    setConversationExpired(false);
  }, [api]);

  const endConversation = React.useCallback(async () => {
    if (!conversation) return;
    await api.post(`${apiBase}/tavus/conversations/${conversation.id}/end`);
    setConversationExpired(true);
  }, [api, conversation]);

  React.useEffect(() => {
    if (!isAuthenticated) return;
    startConversation().catch((err) => console.error('Failed to start conversation', err));
  }, [isAuthenticated, startConversation]);

  const startMachine = async () => {
    const res = await api.post(`${apiBase}/fly/machines/start`);
    setMachine(res.data as MachineInfo);
  };

  const stopMachine = async () => {
    await api.post(`${apiBase}/fly/machines/stop`);
    setMachine(undefined);
  };

  const streamLogs = () => {
    if (!sessionId) return;
    logStreamRef.current?.close();
    const ev = new EventSource(`${apiBase}/fly/machines/exec/stream?sessionId=${sessionId}`, { withCredentials: false });
    logStreamRef.current = ev;
    ev.onmessage = (msg) => {
      setStreamingLog((prev) => `${prev}\n${msg.data}`.trim());
    };
    ev.onerror = () => {
      ev.close();
      logStreamRef.current = null;
    };
  };

  const sendMessage = async (text: string) => {
    const updated = [...messages, buildMessage('user', text)];
    setMessages(updated);
    const res = await api.post(`${apiBase}/agent/run`, { message: text, memory: updated });
    const agent = res.data as AgentResponse;
    const assistantMsg = buildMessage('assistant', agent.reply);
    const nextMessages = [...updated, assistantMsg];
    setMessages(agent.memory ?? nextMessages);
    if (agent.sessionId) {
      setSessionId(agent.sessionId);
      setStreamingLog('');
    }
    if (agent.toolLog) {
      setStreamingLog((prev) => `${prev}\n${agent.toolLog}`.trim());
    }
  };

  if (isLoading) {
    return <div className="flex h-screen items-center justify-center text-lg text-slate-700">Loading auth...</div>;
  }

  if (!isAuthenticated) {
    return (
      <div className="flex h-screen flex-col items-center justify-center gap-4 bg-gradient-to-br from-indigo-50 via-white to-slate-100">
        <h1 className="text-3xl font-bold text-slate-900">Azure SWA + Tavus + Fly Machines</h1>
        <p className="max-w-xl text-center text-slate-600">
          Log in with Auth0 to start a Tavus conversation, chat with the agent, and preview your Fly.io machine.
        </p>
        <button
          onClick={() => loginWithRedirect()}
          className="rounded bg-indigo-600 px-5 py-3 text-lg font-semibold text-white shadow hover:bg-indigo-500"
        >
          Log in with Auth0
        </button>
      </div>
    );
  }

  return (
    <div className="flex h-screen flex-col bg-slate-50">
      <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3 shadow-sm">
        <div>
          <div className="text-lg font-bold text-slate-900">Animated Persona Playground</div>
          <div className="text-sm text-slate-600">Authenticated as {user?.email || user?.name}</div>
        </div>
        <button
          onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-slate-700"
        >
          Logout
        </button>
      </header>
      <main className="flex flex-1 gap-4 p-4">
        <div className="flex w-[30%] flex-col gap-4">
          <div className="h-1/2 min-h-[320px]">
            <TavusPane
              conversation={conversation}
              onHangup={endConversation}
              onStartNew={startConversation}
              expired={conversationExpired}
              setExpired={setConversationExpired}
            />
          </div>
          <div className="h-1/2 min-h-[320px]">
            <ChatPane messages={messages} onSend={sendMessage} streamingLog={streamingLog} />
          </div>
        </div>
        <div className="w-[70%]">
          <PreviewPane machine={machine} onStart={startMachine} onStop={stopMachine} onStreamLogs={streamLogs} logSessionId={sessionId} />
        </div>
      </main>
    </div>
  );
};

export default App;
