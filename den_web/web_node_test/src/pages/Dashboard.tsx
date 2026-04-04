import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import './Dashboard.css';

interface Lesson {
  id: number;
  title: string;
  description: string;
  duration: string;
  completed: boolean;
  videoUrl: string;
}

const Dashboard: React.FC = () => {
  const { user } = useAppContext();
  const [activeTab, setActiveTab] = useState<'lessons' | 'progress' | 'keys'>('lessons');
  
  const [lessons] = useState<Lesson[]>([
    {
      id: 1,
      title: 'Введение в программирование',
      description: 'Основные понятия и принципы программирования',
      duration: '15 мин',
      completed: true,
      videoUrl: '#'
    },
    {
      id: 2,
      title: 'Переменные и типы данных',
      description: 'Изучаем переменные и основные типы данных',
      duration: '20 мин',
      completed: false,
      videoUrl: '#'
    },
    {
      id: 3,
      title: 'Условные операторы',
      description: 'if, else, switch - учимся принимать решения',
      duration: '25 мин',
      completed: false,
      videoUrl: '#'
    },
    {
      id: 4,
      title: 'Циклы',
      description: 'for, while, do-while - повторяем действия',
      duration: '30 мин',
      completed: false,
      videoUrl: '#'
    }
  ]);

  const [trialKeys] = useState([
    { id: 1, key: 'CODE123-456-789', status: 'Активен', expires: '30.12.2024' },
    { id: 2, key: 'CODE456-789-123', status: 'Использован', expires: '15.12.2024' },
  ]);

  const progress = Math.round((lessons.filter(l => l.completed).length / lessons.length) * 100);

  const generateKey = () => {
    const newKey = `CODE${Math.random().toString(36).substr(2, 9).toUpperCase()}`;
    alert(`Новый ключ: ${newKey}`);
  };

  return (
    <div className="dashboard">
      <nav className="dashboard-nav">
        <div className="nav-container">
          <div className="logo">UnityNodeBridge</div>
          <div className="user-info">
            <span>Привет, {user ? `${user.name} ${user.lastName}` : 'гость'}!</span>
            <Link to="/" className="logout-btn">Выйти</Link>
          </div>
        </div>
      </nav>

      <div className="dashboard-content">
        <aside className="sidebar">
          <div className="user-profile">
            <div className="avatar">И</div>
            <h3>Иван Петров</h3>
            <p>Студент</p>
          </div>
          
          <nav className="sidebar-nav">
            <button 
              className={activeTab === 'lessons' ? 'active' : ''}
              onClick={() => setActiveTab('lessons')}
            >
              📚 Уроки
            </button>
            <button 
              className={activeTab === 'progress' ? 'active' : ''}
              onClick={() => setActiveTab('progress')}
            >
              📊 Прогресс
            </button>
            <button 
              className={activeTab === 'keys' ? 'active' : ''}
              onClick={() => setActiveTab('keys')}
            >
              🔑 Мои ключи
            </button>
          </nav>
        </aside>

        <main className="main-content">
          {activeTab === 'lessons' && (
            <div className="lessons-tab">
              <h2>Доступные уроки</h2>
              <div className="progress-bar">
                <div className="progress" style={{ width: `${progress}%` }}></div>
              </div>
              <p className="progress-text">Прогресс: {progress}%</p>
              
              <div className="lessons-grid">
                {lessons.map(lesson => (
                  <Link to={`/lesson/${lesson.id}`} key={lesson.id} className="lesson-card">
                    <div className="lesson-status">
                      {lesson.completed ? '✅' : '📹'}
                    </div>
                    <h3>{lesson.title}</h3>
                    <p>{lesson.description}</p>
                    <div className="lesson-meta">
                      <span>⏱️ {lesson.duration}</span>
                      <span className="start-lesson">Начать →</span>
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          )}

          {activeTab === 'progress' && (
            <div className="progress-tab">
              <h2>Мой прогресс</h2>
              
              <div className="stats-grid">
                <div className="stat-card">
                  <div className="stat-value">{lessons.length}</div>
                  <div className="stat-label">Всего уроков</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{lessons.filter(l => l.completed).length}</div>
                  <div className="stat-label">Пройдено</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{progress}%</div>
                  <div className="stat-label">Завершено</div>
                </div>
              </div>

              <h3>Детальный прогресс по урокам</h3>
              <div className="lesson-progress-list">
                {lessons.map(lesson => (
                  <div key={lesson.id} className="lesson-progress-item">
                    <div className="lesson-info">
                      <h4>{lesson.title}</h4>
                      <span className={lesson.completed ? 'completed' : 'not-completed'}>
                        {lesson.completed ? 'Пройдено' : 'В процессе'}
                      </span>
                    </div>
                    <div className="progress-bar small">
                      <div className="progress" style={{ width: lesson.completed ? '100%' : '0%' }}></div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {activeTab === 'keys' && (
            <div className="keys-tab">
              <h2>Мои триальные ключи</h2>
              
              <button onClick={generateKey} className="generate-key-btn">
                🎁 Сгенерировать новый ключ
              </button>

              <div className="keys-table">
                <table>
                  <thead>
                    <tr>
                      <th>Ключ</th>
                      <th>Статус</th>
                      <th>Истекает</th>
                    </tr>
                  </thead>
                  <tbody>
                    {trialKeys.map(key => (
                      <tr key={key.id}>
                        <td><code>{key.key}</code></td>
                        <td>
                          <span className={`status-badge ${key.status === 'Активен' ? 'active' : 'used'}`}>
                            {key.status}
                          </span>
                        </td>
                        <td>{key.expires}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
};

export default Dashboard;