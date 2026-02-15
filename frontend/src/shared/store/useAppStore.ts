import { create } from 'zustand';

type AppState = {
  isSidebarOpen: boolean;
  setSidebarOpen: (isOpen: boolean) => void;
};

export const useAppStore = create<AppState>((set) => ({
  isSidebarOpen: true,
  setSidebarOpen: (isOpen) => set({ isSidebarOpen: isOpen }),
}));
