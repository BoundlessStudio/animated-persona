import React from 'react';
import clsx from 'clsx';

export type ConversationProps = {
  children: React.ReactNode;
};

export function Conversation({ children }: ConversationProps) {
  return <div className="flex flex-col gap-3">{children}</div>;
}

export type TranscriptProps = {
  messages: { id: string; role: string; content: string; createdAt?: string }[];
};

export function Transcript({ messages }: TranscriptProps) {
  return (
    <div className="flex-1 overflow-y-auto rounded border border-slate-200 bg-white p-3 text-sm shadow-inner">
      {messages.length === 0 ? (
        <p className="text-slate-500">No messages yet.</p>
      ) : (
        <ul className="space-y-2">
          {messages.map((m) => (
            <li key={m.id} className="rounded bg-slate-50 p-2">
              <div className="text-xs uppercase tracking-wide text-slate-500">{m.role}</div>
              <div className="whitespace-pre-wrap text-slate-800">{m.content}</div>
              {m.createdAt && <div className="text-[10px] text-slate-400">{m.createdAt}</div>}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export type PromptInputProps = {
  onSend: (value: string) => void;
  disabled?: boolean;
};

export function PromptInput({ onSend, disabled }: PromptInputProps) {
  const [value, setValue] = React.useState('');

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!value.trim()) return;
    onSend(value);
    setValue('');
  };

  return (
    <form onSubmit={submit} className="flex items-center gap-2">
      <input
        className="flex-1 rounded border border-slate-200 px-3 py-2 text-sm shadow-inner focus:border-indigo-400 focus:outline-none"
        placeholder="Ask the agent..."
        value={value}
        onChange={(e) => setValue(e.target.value)}
        disabled={disabled}
      />
      <button
        type="submit"
        disabled={disabled}
        className={clsx(
          'rounded bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-indigo-500',
          disabled && 'opacity-60',
        )}
      >
        Send
      </button>
    </form>
  );
}

export type PreviewProps = {
  title?: string;
  footer?: React.ReactNode;
  children: React.ReactNode;
};

export function Preview({ children, title, footer }: PreviewProps) {
  return (
    <div className="flex flex-col overflow-hidden rounded border border-slate-200 bg-white shadow">
      {title && <div className="border-b border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700">{title}</div>}
      <div className="flex-1 overflow-auto">{children}</div>
      {footer && <div className="border-t border-slate-200 px-4 py-2">{footer}</div>}
    </div>
  );
}
