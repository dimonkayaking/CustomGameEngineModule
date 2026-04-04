import React, { useState } from 'react';
import { useAppContext } from '../context/AppContext';
import { allLessonIds } from '../data/courseData';
import './Profile.css';

const Profile: React.FC = () => {
  const { user, completedLessons } = useAppContext();
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [status, setStatus] = useState('');
  const [statusType, setStatusType] = useState<'success' | 'error' | ''>('');
  const totalLessons = allLessonIds.length;
  const completedCount = completedLessons.length;
  const progressPercent = totalLessons > 0 ? Math.round((completedCount / totalLessons) * 100) : 0;

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();

    if (newPassword !== confirmPassword) {
      setStatus('Пароли не совпадают');
      setStatusType('error');
      return;
    }

    setStatus('Пароль успешно изменён');
    setStatusType('success');
    setOldPassword('');
    setNewPassword('');
    setConfirmPassword('');
  };

  if (!user) {
    return <p className="profile-notice">Пожалуйста, войдите, чтобы посмотреть профиль.</p>;
  }

  return (
    <section className="profile-page">
      <h1>Профиль пользователя</h1>
      <div className="profile-grid">
        <div className="profile-card">
          <h2>Данные</h2>
          <p><strong>Имя:</strong> {user.name}</p>
          <p><strong>Фамилия:</strong> {user.lastName}</p>
          <p><strong>Email:</strong> {user.email}</p>
        </div>

        <div className="profile-card">
          <h2>Смена пароля</h2>
          <form onSubmit={handleSave} className="change-password-form">
            <input
              type="password"
              placeholder="Старый пароль"
              value={oldPassword}
              onChange={(e) => setOldPassword(e.target.value)}
              required
            />
            <input
              type="password"
              placeholder="Новый пароль"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
            />
            <input
              type="password"
              placeholder="Подтвердите новый пароль"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
            />
            <button type="submit">Сохранить</button>
            <p className={`status-msg ${statusType ? `status-msg--${statusType}` : ''}`}>{status}</p>
          </form>
        </div>

        <div className="profile-card full-width">
          <h2>Прогресс</h2>
          <p>Пройдено уроков: {completedCount} из {totalLessons}</p>
          <div className="progress-line">
            <div style={{ width: `${progressPercent}%` }} />
          </div>
        </div>
      </div>
    </section>
  );
};

export default Profile;
