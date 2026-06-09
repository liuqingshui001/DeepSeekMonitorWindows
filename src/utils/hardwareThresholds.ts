export type ThresholdLevel = 'safe' | 'warn' | 'crit';

export interface ThresholdRule {
  warn: number;
  crit: number;
}

export const defaultThresholds: Record<string, ThresholdRule> = {
  load: { warn: 60, crit: 85 },
  temp: { warn: 50, crit: 70 },
  diskIO: { warn: 2, crit: 8 },
  netUp: { warn: 1, crit: 2 },
  netDown: { warn: 2, crit: 8 },
};

export function getThresholdLevel(key: string, value: number): ThresholdLevel {
  if (key.endsWith('Load') || key === 'MEM.Load') {
    if (value >= defaultThresholds.load.crit) return 'crit';
    if (value >= defaultThresholds.load.warn) return 'warn';
    return 'safe';
  }
  if (key.includes('Temp')) {
    if (value >= defaultThresholds.temp.crit) return 'crit';
    if (value >= defaultThresholds.temp.warn) return 'warn';
    return 'safe';
  }
  if (key === 'DISK.Read' || key === 'DISK.Write') {
    if (value >= defaultThresholds.diskIO.crit) return 'crit';
    if (value >= defaultThresholds.diskIO.warn) return 'warn';
    return 'safe';
  }
  if (key === 'NET.Up') {
    if (value >= defaultThresholds.netUp.crit) return 'crit';
    if (value >= defaultThresholds.netUp.warn) return 'warn';
    return 'safe';
  }
  if (key === 'NET.Down') {
    if (value >= defaultThresholds.netDown.crit) return 'crit';
    if (value >= defaultThresholds.netDown.warn) return 'warn';
    return 'safe';
  }
  return 'safe';
}

export function getThresholdColor(key: string, value: number): string {
  const level = getThresholdLevel(key, value);
  switch (level) {
    case 'crit':  return 'var(--threshold-crit, #ef4444)';
    case 'warn':  return 'var(--threshold-warn, #f59e0b)';
    case 'safe':  return 'var(--threshold-safe, #22c55e)';
  }
}

export function getProgressValue(key: string, value: number): number {
  const maxMap: Record<string, number> = {
    'CPU.Load': 100, 'CPU.Temp': 100,
    'GPU.Load': 100, 'GPU.Temp': 100,
    'MEM.Load': 100, 'BAT.Percent': 100,
  };
  const normalized = Object.entries(maxMap);
  for (const [k, max] of normalized) {
    if (key === k) return Math.max(0.05, Math.min(1.0, value / max));
  }
  return Math.max(0.05, Math.min(1.0, value / 100));
}
