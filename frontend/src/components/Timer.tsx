import React from 'react';

export type TimerProps = {
  expiresAt?: string;
  onExpired?: () => void;
};

function formatRemaining(ms: number) {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const minutes = Math.floor(totalSeconds / 60)
    .toString()
    .padStart(2, '0');
  const seconds = Math.floor(totalSeconds % 60)
    .toString()
    .padStart(2, '0');
  return `${minutes}:${seconds}`;
}

export const Timer: React.FC<TimerProps> = ({ expiresAt, onExpired }) => {
  const [remaining, setRemaining] = React.useState<string>('--:--');
  const [expired, setExpired] = React.useState(false);

  React.useEffect(() => {
    setExpired(false);
    setRemaining('--:--');
    if (!expiresAt) return;
    const exp = new Date(expiresAt).getTime();
    if (Number.isNaN(exp)) return;
    let calledExpired = false;
    const tick = () => {
      const now = Date.now();
      const diff = exp - now;
      if (diff <= 0) {
        setRemaining('00:00');
        if (!calledExpired) {
          calledExpired = true;
          setExpired(true);
          onExpired?.();
        }
        return false;
      }
      setRemaining(formatRemaining(diff));
      return true;
    };

    const shouldContinue = tick();
    if (!shouldContinue) return;
    const id = setInterval(() => {
      const cont = tick();
      if (!cont) {
        clearInterval(id);
      }
    }, 1000);
    return () => clearInterval(id);
  }, [expiresAt, onExpired]);

  const remainingMs = expiresAt ? new Date(expiresAt).getTime() - Date.now() : undefined;
  const warn = remainingMs !== undefined && remainingMs <= 5 * 60 * 1000 && remainingMs > 0;

  return (
    <div className={`flex items-center gap-2 rounded px-3 py-2 text-sm ${warn ? 'bg-amber-100 text-amber-700' : 'bg-slate-100 text-slate-700'}`}>
      <span className="font-semibold">Conversation Timer:</span>
      <span className="tabular-nums">{remaining}</span>
      {expired && <span className="text-xs font-semibold text-rose-600">Expired</span>}
    </div>
  );
};
