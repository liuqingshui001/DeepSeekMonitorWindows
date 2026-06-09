import React from 'react';
import { Sun, Moon, RotateCw, Settings } from 'lucide-react';
import ViewSwitcher from './ViewSwitcher';

interface TitleBarProps {
  viewMode: 'tab' | 'split';
  onToggleView: () => void;
  onRefresh: () => void;
  onToggleTheme: () => void;
  onOpenSettings: () => void;
  theme: string;
}

const TitleBar: React.FC<TitleBarProps> = ({
  viewMode, onToggleView, onRefresh, onToggleTheme, onOpenSettings, theme
}) => {
  return (
    <div className="title-bar" data-tauri-drag-region>
      <span className="title-bar-brand">Monitor</span>
      <div className="title-bar-actions">
        <ViewSwitcher viewMode={viewMode} onToggle={onToggleView} />
        <button className="title-bar-btn" onClick={onRefresh} title="刷新">
          <RotateCw size={14} />
        </button>
        <button className="title-bar-btn" onClick={onToggleTheme} title="切换主题">
          {theme === 'dark' ? <Sun size={14} /> : <Moon size={14} />}
        </button>
        <button className="title-bar-btn" onClick={onOpenSettings} title="设置">
          <Settings size={14} />
        </button>
      </div>
    </div>
  );
};

export default TitleBar;
