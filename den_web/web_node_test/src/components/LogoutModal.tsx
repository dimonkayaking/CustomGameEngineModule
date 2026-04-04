import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import './LogoutModal.css';

const LogoutModal: React.FC = () => {
  const navigate = useNavigate();
  const { isLogoutModalOpen, closeLogoutModal, logout } = useAppContext();
  if (!isLogoutModalOpen) return null;

  return (
    <div className="logout-backdrop" onClick={closeLogoutModal}>
      <div className="logout-modal" onClick={(e) => e.stopPropagation()}>
        <h3>Вы уверены, что хотите выйти?</h3>
        <div className="logout-actions">
          <button className="btn-danger" onClick={() => { logout(); closeLogoutModal(); navigate('/'); }}>
            Выйти
          </button>
          <button onClick={closeLogoutModal}>Отмена</button>
        </div>
      </div>
    </div>
  );
};

export default LogoutModal;
