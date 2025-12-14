import React from 'react';
import type { MachineInfo } from '../types';
import { Preview } from './ai-elements';

export type PreviewPaneProps = {
  machine?: MachineInfo;
  onStart: () => Promise<void>;
  onStop: () => Promise<void>;
  onStreamLogs?: () => void;
  logSessionId?: string;
};

export const PreviewPane: React.FC<PreviewPaneProps> = ({ machine, onStart, onStop, onStreamLogs, logSessionId }) => {
  const [busy, setBusy] = React.useState(false);

  const handle = async (fn: () => Promise<void>) => {
    setBusy(true);
    try {
      await fn();
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="flex h-full flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-slate-800">Fly.io Preview</h2>
        <div className="flex items-center gap-2">
          <button
            className="rounded bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow hover:bg-indigo-500 disabled:opacity-60"
            onClick={() => handle(onStart)}
            disabled={busy}
          >
            Start Machine
          </button>
          <button
            className="rounded bg-slate-700 px-3 py-2 text-sm font-semibold text-white shadow hover:bg-slate-600 disabled:opacity-60"
            onClick={() => handle(onStop)}
            disabled={busy}
          >
            Stop Machine
          </button>
          {onStreamLogs && (
            <button
              className="rounded bg-amber-600 px-3 py-2 text-sm font-semibold text-white shadow hover:bg-amber-500 disabled:opacity-60"
              onClick={onStreamLogs}
              disabled={!logSessionId}
            >
              Stream Logs
            </button>
          )}
        </div>
      </div>
      <div className="flex flex-col gap-2 text-sm text-slate-700">
        <div className="rounded border border-slate-200 bg-slate-50 px-3 py-2">
          <div className="font-semibold">Service URL</div>
          <div className="text-indigo-700">{machine?.serviceUrl ?? 'Not started yet'}</div>
        </div>
      </div>
      <Preview title="Live Preview" footer={<div className="text-xs text-slate-500">Machine traffic proxied through Fly.io</div>}>
        {machine?.serviceUrl ? (
          <iframe
            title="Fly Preview"
            src={machine.serviceUrl}
            className="h-[calc(100vh-240px)] w-full border-0"
            allow="clipboard-write; autoplay"
          />
        ) : (
          <div className="flex h-[calc(100vh-240px)] items-center justify-center text-slate-400">Start a machine to view the preview</div>
        )}
      </Preview>
    </div>
  );
};
