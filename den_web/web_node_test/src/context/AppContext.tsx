import React, { createContext, useContext, useEffect, useState } from 'react';

type User = {
  name: string;
  lastName: string;
  email: string;
};

type Theme = 'light' | 'dark';

interface AppContextType {
  user: User | null;
  login: (user: User) => void;
  logout: () => void;
  theme: Theme;
  toggleTheme: () => void;
  isLogoutModalOpen: boolean;
  openLogoutModal: () => void;
  closeLogoutModal: () => void;
  completedLessons: number[];
  markLessonCompleted: (lessonId: number) => void;
}

const initialContext: AppContextType = {
  user: null,
  login: () => {},
  logout: () => {},
  theme: 'light',
  toggleTheme: () => {},
  isLogoutModalOpen: false,
  openLogoutModal: () => {},
  closeLogoutModal: () => {},
  completedLessons: [],
  markLessonCompleted: () => {},
};

const AppContext = createContext<AppContextType>(initialContext);

export const AppProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [theme, setTheme] = useState<Theme>('light');
  const [isLogoutModalOpen, setIsLogoutModalOpen] = useState(false);
  const [completedLessons, setCompletedLessons] = useState<number[]>([]);

  const login = (newUser: User) => {
    setUser(newUser);
    setCompletedLessons([]);
  };
  const logout = () => {
    setUser(null);
    setIsLogoutModalOpen(false);
    setCompletedLessons([]);
  };

  const toggleTheme = () => setTheme((prev) => (prev === 'light' ? 'dark' : 'light'));
  const openLogoutModal = () => setIsLogoutModalOpen(true);
  const closeLogoutModal = () => setIsLogoutModalOpen(false);
  const markLessonCompleted = (lessonId: number) => {
    setCompletedLessons((prev) => (prev.includes(lessonId) ? prev : [...prev, lessonId]));
  };

  useEffect(() => {
    document.body.classList.remove('light', 'dark');
    document.body.classList.add(theme);
  }, [theme]);

  return (
    <AppContext.Provider
      value={{
        user,
        login,
        logout,
        theme,
        toggleTheme,
        isLogoutModalOpen,
        openLogoutModal,
        closeLogoutModal,
        completedLessons,
        markLessonCompleted,
      }}
    >
      {children}
    </AppContext.Provider>
  );
};

export const useAppContext = () => useContext(AppContext);
