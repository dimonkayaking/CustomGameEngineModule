import React from 'react';
import { Link, useParams } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import { courseModules, getModuleById } from '../data/courseData';
import './Topic.css';

const Topic: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { completedLessons } = useAppContext();
  const module = getModuleById(id || '1') || courseModules[0];

  return (
    <section className="topic-page">
      <Link to="/course" className="back-topic">← Назад ко всем модулям</Link>

      <div className="topic-hero">
        <div>
          <span className="topic-eyebrow">Модуль {module.order}</span>
          <h1>{module.title}</h1>
          <p><strong>Цель модуля:</strong> {module.goal}</p>
        </div>

        <div className="topic-side-card">
          <strong>{module.lessons.length} урока</strong>
          <span>{module.practice.length} практических блока</span>
        </div>
      </div>

      <div className="topic-sections">
        <div className="topic-block">
          <h2>Уроки модуля</h2>
          <div className="lesson-list">
            {module.lessons.map((lesson, index) => {
              const previousLesson = index > 0 ? module.lessons[index - 1] : null;
              const isDone = completedLessons.includes(lesson.id);
              const isOpen =
                index === 0 ||
                isDone ||
                (previousLesson ? completedLessons.includes(previousLesson.id) : false);
              const status: 'locked' | 'done' | 'open' = isDone ? 'done' : isOpen ? 'open' : 'locked';

              return (
                <div key={lesson.id} className={`lesson-row lesson-row--${status}`}>
                  <div className="status-icon">
                    {status === 'locked' && '🔒'}
                    {status === 'done' && '✓'}
                    {status === 'open' && '○'}
                  </div>
                  <div className="lesson-body">
                    <div className="lesson-name">{lesson.title}</div>
                    <p>{lesson.summary}</p>
                    <div className="lesson-tags">
                      <span>{lesson.duration}</span>
                      <span>{lesson.format === 'practice' ? 'С заданием' : 'Теория'}</span>
                    </div>
                  </div>
                  <div className="row-action">
                    <Link to={`/lesson/${lesson.id}`} className={`watch-btn ${status === 'locked' ? 'disabled' : ''}`}>
                      Смотреть
                    </Link>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        <div className="topic-block">
          <h2>Практика по модулю</h2>
          <div className="practice-list">
            {module.practice.length > 0 ? (
              module.practice.map((item, index) => (
                <article key={`${item.type}-${index}`} className="practice-card">
                  <span className={`practice-type practice-type--${item.type === 'Проверка' ? 'check' : 'free'}`}>
                    {item.type}
                  </span>
                  <p>{item.text}</p>
                </article>
              ))
            ) : (
              <article className="practice-card">
                <span className="practice-type practice-type--free">Итог</span>
                <p>В этом модуле акцент сделан на завершение курса, подведение итогов и определение следующего шага в развитии.</p>
              </article>
            )}
          </div>
        </div>
      </div>
    </section>
  );
};

export default Topic;
