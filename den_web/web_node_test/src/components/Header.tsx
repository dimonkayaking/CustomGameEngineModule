import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import './Header.css';

const Header: React.FC = () => {
  const { user, theme, toggleTheme, openLogoutModal } = useAppContext();
  const [isMobileMenuOpen, setMobileMenuOpen] = useState(false);

  const hideMobileMenu = () => setMobileMenuOpen(false);

  return (
    <header className={`site-header ${theme}`}>
      <div className="header-container">
        <Link to="/" className="logo" onClick={hideMobileMenu}>
          UnityNodeBridge
        </Link>

        <button className="burger" onClick={() => setMobileMenuOpen((s) => !s)} aria-label="Меню">
          ☰
        </button>

        <nav className={`main-nav ${isMobileMenuOpen ? 'open' : ''}`}> 
          {user ? (
            <>
              <Link to="/plugin" onClick={hideMobileMenu}>Плагин</Link>
              <Link to="/course" onClick={hideMobileMenu}>Курс</Link>
              <button className="icon-btn" onClick={toggleTheme} title="Переключить тему">💡</button>
              <Link to="/profile" className="header-action-btn" onClick={hideMobileMenu}>Профиль</Link>
              <button
                className="header-action-btn"
                onClick={() => {
                  hideMobileMenu();
                  openLogoutModal();
                }}
              >
                Выйти
              </button>
            </>
          ) : (
            <>
              <button className="icon-btn" onClick={toggleTheme} title="Переключить тему">💡</button>
              <Link to="/login" onClick={hideMobileMenu}>Войти</Link>
              <Link to="/register" className="btn-primary" onClick={hideMobileMenu}>Регистрация</Link>
            </>
          )}
        </nav>
      </div>
    </header>
  );
};

export default Header;
