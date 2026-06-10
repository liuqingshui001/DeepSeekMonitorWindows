import React from 'react';
import { Cpu, Monitor, HardDrive, Wifi, Server } from 'lucide-react';
import SensorGroup from './HardwareSensorGroup';
import { sensorShortLabel } from '../utils/hardwareFormat';

interface HardwareDashboardProps {
  sensors: Record<string, number>;
  enabledCategories?: string[];
}

const AlwaysSensorGroup: React.FC<{ title: string; icon: React.ReactNode; items: Array<{ key: string; label: string; showProgress: boolean; meta?: { totalGB?: number } }>; get: (k: string) => number | undefined }> = ({ title, icon, items, get }) => {
  const mapped = items.map(item => ({
    ...item,
    value: get(item.key),
  }));
  return <SensorGroup title={title} icon={icon} sensors={mapped} />;
};

const ALL_CATEGORIES = ['cpu', 'gpu', 'memory', 'disk', 'network'];

const HardwareDashboard: React.FC<HardwareDashboardProps> = ({ sensors, enabledCategories = ALL_CATEGORIES }) => {
  const get = (key: string): number | undefined => sensors[key];
  const isAvailable = Object.keys(sensors).length > 0;

  return (
    <div className="hardware-dashboard">
      {!isAvailable && <div className="hw-loading">等待传感器数据…</div>}
      {enabledCategories.includes('cpu') && (
      <AlwaysSensorGroup title="CPU" icon={<Cpu size={16} />}
        get={get}
        items={[
          { key: 'CPU.Load',  label: sensorShortLabel('CPU.Load'), showProgress: true },
          { key: 'CPU.Temp',  label: sensorShortLabel('CPU.Temp'), showProgress: true },
          { key: 'CPU.Clock', label: sensorShortLabel('CPU.Clock'), showProgress: false },
          { key: 'CPU.Power', label: sensorShortLabel('CPU.Power'), showProgress: true },
        ]}
      />)}
      {enabledCategories.includes('gpu') && (
      <AlwaysSensorGroup title="GPU" icon={<Monitor size={16} />}
        get={get}
        items={[
          { key: 'GPU.Load',    label: sensorShortLabel('GPU.Load'), showProgress: true },
          { key: 'GPU.Temp',    label: sensorShortLabel('GPU.Temp'), showProgress: true },
          { key: 'GPU.Clock',   label: sensorShortLabel('GPU.Clock'), showProgress: false },
          { key: 'GPU.Power',   label: sensorShortLabel('GPU.Power'), showProgress: true },
          { key: 'GPU.MemUsed', label: sensorShortLabel('GPU.MemUsed'), showProgress: true },
        ]}
      />)}
      {enabledCategories.includes('memory') && (
      <AlwaysSensorGroup title="内存" icon={<Server size={16} />}
        get={get}
        items={[
          { key: 'MEM.Load',   label: sensorShortLabel('MEM.Load'), showProgress: true },
          { key: 'MEM.UsedGB', label: sensorShortLabel('MEM.UsedGB'), showProgress: false },
        ]}
      />)}
      {enabledCategories.includes('disk') && (
      <AlwaysSensorGroup title="磁盘" icon={<HardDrive size={16} />}
        get={get}
        items={[
          { key: 'DISK.Read',  label: sensorShortLabel('DISK.Read'), showProgress: false },
          { key: 'DISK.Write', label: sensorShortLabel('DISK.Write'), showProgress: false },
          { key: 'DISK.Temp',  label: sensorShortLabel('DISK.Temp'), showProgress: true },
        ]}
      />)}
      {enabledCategories.includes('network') && (
      <AlwaysSensorGroup title="网络" icon={<Wifi size={16} />}
        get={get}
        items={[
          { key: 'NET.Up',   label: sensorShortLabel('NET.Up'), showProgress: false },
          { key: 'NET.Down', label: sensorShortLabel('NET.Down'), showProgress: false },
        ]}
      />)}
    </div>
  );
};

export default HardwareDashboard;
