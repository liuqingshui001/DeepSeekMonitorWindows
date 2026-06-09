// 传感器数值格式化函数

export function fmtPercent(val: number): string {
  return val.toFixed(1) + '%';
}

export function fmtTemp(val: number): string {
  return val.toFixed(1) + '°C';
}

export function fmtFreq(val: number): string {
  if (val >= 1000) return (val / 1000).toFixed(2) + 'GHz';
  return val.toFixed(1) + 'MHz';
}

export function fmtSpeed(val: number): string {
  if (val >= 1024) return (val / 1024).toFixed(1) + ' GB/s';
  if (val >= 1) return val.toFixed(1) + ' MB/s';
  return (val * 1024).toFixed(0) + ' KB/s';
}

export function fmtNetSpeed(val: number): string {
  // LHM 网络 Throughput 单位是 KB/s
  if (val >= 1024) return (val / 1024).toFixed(1) + ' MB/s';
  return val.toFixed(1) + ' KB/s';
}

export function fmtRPM(val: number): string {
  return val.toFixed(0) + ' RPM';
}

export function fmtMemory(used: number, total: number): string {
  return used.toFixed(1) + '/' + total.toFixed(1) + ' GB';
}

export function fmtFPS(val: number): string {
  return val.toFixed(0);
}

export function fmtPower(val: number): string {
  return val.toFixed(0) + ' W';
}

export function fmtVoltage(val: number): string {
  return val.toFixed(3) + ' V';
}

export function formatSensorValue(key: string, value: number, meta?: { usedGB?: number; totalGB?: number }): string {
  if (key.endsWith('.Load') || key === 'MEM.Load' || key === 'BAT.Percent')
    return fmtPercent(value);
  if (key.includes('Temp')) return fmtTemp(value);
  if (key.includes('Clock')) return fmtFreq(value);
  if (key === 'GPU.MemUsed' || key === 'MEM.UsedGB') {
    const total = meta?.totalGB;
    if (total) return fmtMemory(value, total);
    return value.toFixed(1) + ' GB';
  }
  if (key === 'DISK.Read' || key === 'DISK.Write' || key === 'NET.Up' || key === 'NET.Down')
    return fmtNetSpeed(value);
  if (key.includes('Fan')) return fmtRPM(value);
  if (key.includes('Power')) return fmtPower(value);
  if (key.includes('Voltage')) return fmtVoltage(value);
  if (key === 'FPS') return fmtFPS(value);
  return value.toFixed(1);
}

export function sensorShortLabel(key: string): string {
  const labels: Record<string, string> = {
    'CPU.Load': '负载', 'CPU.Temp': '温度', 'CPU.Clock': '频率', 'CPU.Power': '功耗',
    'GPU.Load': '负载', 'GPU.Temp': '温度', 'GPU.Clock': '频率', 'GPU.Power': '功耗',
    'GPU.MemUsed': '显存', 'GPU.Fan': '风扇',
    'MEM.Load': '使用率', 'MEM.UsedGB': '内存',
    'DISK.Read': '读取', 'DISK.Write': '写入', 'DISK.Temp': '温度',
    'NET.Up': '上传', 'NET.Down': '下载',
    'MOBO.Temp': '温度',
    'BAT.Percent': '电量', 'BAT.Power': '功耗',
    'FPS': 'FPS',
  };
  return labels[key] || key;
}
