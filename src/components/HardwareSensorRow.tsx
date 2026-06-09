import React from 'react';
import { formatSensorValue } from '../utils/hardwareFormat';
import { getThresholdColor, getProgressValue } from '../utils/hardwareThresholds';

interface SensorRowProps {
  keyName: string;
  value?: number;
  label?: string;
  icon?: React.ReactNode;
  meta?: { usedGB?: number; totalGB?: number };
  showProgress?: boolean;
}

const SensorRow: React.FC<SensorRowProps> = ({ keyName, value, label, icon, meta, showProgress = true }) => {
  const hasValue = value !== undefined && value !== null;
  const color = hasValue ? getThresholdColor(keyName, value!) : 'var(--fg)';
  const progress = hasValue && showProgress ? getProgressValue(keyName, value!) : 0;
  const displayValue = hasValue ? formatSensorValue(keyName, value!, meta) : '--';

  return (
    <div className="sensor-row">
      {icon && <span className="sensor-icon">{icon}</span>}
      <span className="sensor-label">{label || keyName}</span>
      <span className="sensor-value" style={{ color, opacity: hasValue ? 1 : 0.35 }}>{displayValue}</span>
      {showProgress && (
        <div className="sensor-bar-bg">
          <div
            className="sensor-bar-fill"
            style={{
              width: hasValue ? `${progress * 100}%` : '0%',
              backgroundColor: color,
            }}
          />
        </div>
      )}
    </div>
  );
};

export default SensorRow;
