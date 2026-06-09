import React from 'react';
import SensorRow from './HardwareSensorRow';

interface SensorItem {
  key: string;
  value?: number;
  label?: string;
  meta?: { usedGB?: number; totalGB?: number };
  showProgress?: boolean;
}

interface SensorGroupProps {
  title: string;
  icon?: React.ReactNode;
  sensors: SensorItem[];
}

const SensorGroup: React.FC<SensorGroupProps> = ({ title, icon, sensors }) => {
  if (sensors.length === 0) return null;

  return (
    <div className="sensor-group">
      <div className="sensor-group-header">
        {icon && <span className="sensor-group-icon">{icon}</span>}
        <span className="sensor-group-title">{title}</span>
      </div>
      <div className="sensor-group-body">
        {sensors.map(s => (
          <SensorRow
            key={s.key}
            keyName={s.key}
            value={s.value}
            label={s.label}
            meta={s.meta}
            showProgress={s.showProgress}
          />
        ))}
      </div>
    </div>
  );
};

export default SensorGroup;
