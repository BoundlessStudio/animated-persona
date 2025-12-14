import React from 'react';
import type { Conversation } from '../types';
import { Timer } from './Timer';

export type TavusPaneProps = {
  conversation?: Conversation;
  onStartNew: () => void;
  onHangup: () => void;
  expired: boolean;
  setExpired: (value: boolean) => void;
};

const moduleName = '@tavus/cvi-ui';

export const TavusPane: React.FC<TavusPaneProps> = ({ conversation, onHangup, onStartNew, expired, setExpired }) => {
  const [TavusIframe, setTavusIframe] = React.useState<React.ComponentType<{ url: string }> | null>(null);

  React.useEffect(() => {
    const load = async () => {
      try {
        // @vite-ignore ensures Vite does not try to bundle the optional dependency.
        const mod = await import(/* @vite-ignore */ moduleName);
        if (mod && mod.CVI) {
          setTavusIframe(() => (props: { url: string }) => <mod.CVI src={props.url} className="h-full w-full" />);
        }
      } catch (err) {
        console.warn('Falling back to iframe for Tavus CVI', err);
      }
    };
    load();
  }, []);

  const showIframe = !!conversation?.url;

  return (
    <div className="flex h-full flex-col gap-3 rounded-lg border border-slate-200 bg-white p-3 shadow">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-lg font-semibold text-slate-800">Tavus Conversation</h2>
        <div className="flex items-center gap-2">
          <Timer
            expiresAt={conversation?.expiresAt}
            onExpired={() => {
              setExpired(true);
            }}
          />
          <button
            className="rounded bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow hover:bg-indigo-500 disabled:opacity-50"
            onClick={onStartNew}
          >
            Start New Conversation
          </button>
          <button
            className="rounded bg-rose-600 px-3 py-2 text-sm font-semibold text-white shadow hover:bg-rose-500 disabled:opacity-50"
            onClick={onHangup}
            disabled={!conversation || expired}
          >
            Hang Up
          </button>
        </div>
      </div>
      <div className="flex-1 overflow-hidden rounded border border-slate-200 bg-black/90 shadow-inner">
        {showIframe ? (
          TavusIframe ? (
            <TavusIframe url={conversation!.url} />
          ) : (
            <iframe title="Tavus CVI" src={conversation!.url} className="h-full w-full border-0" allow="camera; microphone; autoplay" />
          )
        ) : (
          <div className="flex h-full items-center justify-center text-slate-400">Conversation will start after login</div>
        )}
      </div>
    </div>
  );
};
