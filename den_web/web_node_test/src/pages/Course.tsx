import React from 'react';
import { Link } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import { allLessonIds, courseModules } from '../data/courseData';
import './Course.css';

const Course: React.FC = () => {
  const { completedLessons } = useAppContext();
  const totalLessons = allLessonIds.length;
  const overallProgress = Math.round((completedLessons.length / totalLessons) * 100);

  return (
    <section className="course-page">
      <div className="course-hero">
        <div>
          <span className="course-eyebrow">Полная программа курса</span>
          <h1>От визуальной логики к коду: Основы Unity</h1>
          <p>
            Курс полностью повторяет план обучения: 8 модулей, 24 урока, практики на проверку
            понимания и свободные задания для закрепления навыков в Unity и C#.
          </p>
        </div>

        <div className="course-overview-card">
          <strong>{overallProgress}% пройдено</strong>
          <span>{completedLessons.length} из {totalLessons} уроков завершено</span>
          <div className="progress-line">
            <div style={{ width: `${overallProgress}%` }} />
          </div>
        </div>
      </div>

      <div className="course-summary-grid">
        <article className="course-summary-card">
          <h2>Что внутри</h2>
          <p>Unity Hub, интерфейс движка, визуальные ноды, Space Invaders, генерация C#, практика со скриптами и реверс-инжиниринг кода.</p>
        </article>
        <article className="course-summary-card">
          <h2>Как устроено обучение</h2>
          <p>Каждый модуль содержит цель, набор уроков и блок практики: проверка понимания и свободное задание.</p>
        </article>
        <article className="course-summary-card">
          <h2>Финальный результат</h2>
          <p>Ученик понимает Unity, читает связь между нодами и C#, умеет строить механику и расширять плагин кодом.</p>
        </article>
      </div>

      <div className="topic-grid">
        {courseModules.map((module) => {
          const completedCount = module.lessons.filter((lesson) => completedLessons.includes(lesson.id)).length;
          const progress = Math.round((completedCount / module.lessons.length) * 100);

          return (
            <article key={module.id} className="topic-card">
              <div className="topic-card__top">
                <span className="topic-order">Модуль {module.order}</span>
                <span className="topic-meta">{module.lessons.length} урока</span>
              </div>

              <h3>{module.title}</h3>
              <p className="topic-goal">
                <strong>Цель:</strong> {module.goal}
              </p>

              <ul className="topic-preview-list">
                {module.lessons.slice(0, 3).map((lesson) => (
                  <li key={lesson.id}>{lesson.title}</li>
                ))}
              </ul>

              <div className="progress-wrap">
                <div className="progress-line">
                  <div style={{ width: `${progress}%` }} />
                </div>
                <span>{progress}%</span>
              </div>

              <Link to={`/topic/${module.id}`} className={`topic-btn ${progress > 0 ? 'continue' : 'start'}`}>
                {progress > 0 ? 'Продолжить' : 'Начать'}
              </Link>
            </article>
          );
        })}
      </div>
    </section>
  );
};

export default Course;
