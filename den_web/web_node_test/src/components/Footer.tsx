import React from 'react';
import { Link } from 'react-router-dom';
import './Footer.css';

const Footer: React.FC = () => {
  return (
    <footer className="site-footer">
      <div className="footer-inner">
        <p>
          Техподдержка: <a href="mailto:support@unity-node-bridge.ru">support@unity-node-bridge.ru</a>
        </p>
        <div className="footer-links">
          <Link to="/plugin">Описание плагина</Link>
          <Link to="/privacy">Политика конфиденциальности</Link>
          <Link to="/terms">Пользовательское соглашение</Link>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
