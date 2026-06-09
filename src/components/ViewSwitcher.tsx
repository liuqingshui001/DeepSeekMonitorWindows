import React from 'react';
import { Columns2, LayoutPanelTop } from 'lucide-react';

interface ViewSwitcherProps {
  viewMode: 'tab' | 'split';
  onToggle: () => void;
}

const ViewSwitcher: React.FC<ViewSwitcherProps> = ({ viewMode, onToggle }) => {
  return (
    <button
      className="view-switcher-btn"
      onClick={onToggle}
      title={viewMode === 'tab' ? '切换到双列模式 (Ctrl+\\)' : '切换到标签页模式 (Ctrl+\\)'}
    >
      {viewMode === 'tab' ? <Columns2 size={14} /> : <LayoutPanelTop size={14} />}
    </button>
  );
};

export default ViewSwitcher;
