import React from 'react';
import type { ChatMessage } from '../types';
import { Conversation as ConversationLayout, Transcript, PromptInput } from './ai-elements';

export type ChatPaneProps = {
  messages: ChatMessage[];
  onSend: (text: string) => Promise<void>;
  streamingLog?: string;
};

export const ChatPane: React.FC<ChatPaneProps> = ({ messages, onSend, streamingLog }) => {
  const [pending, setPending] = React.useState(false);

  const handleSend = async (value: string) => {
    setPending(true);
    try {
      await onSend(value);
    } finally {
      setPending(false);
    }
  };

  const rendered = streamingLog
    ? [...messages, { id: 'log', role: 'tool', content: streamingLog, createdAt: new Date().toISOString() }]
    : messages;

  return (
    <div className="flex h-full flex-col gap-3 rounded-lg border border-slate-200 bg-white p-3 shadow">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-slate-800">Agent Chat</h2>
      </div>
      <ConversationLayout>
        <Transcript messages={rendered} />
        <PromptInput onSend={handleSend} disabled={pending} />
      </ConversationLayout>
    </div>
  );
};
