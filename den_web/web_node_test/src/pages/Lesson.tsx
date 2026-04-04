import React, { useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import { allLessonIds, courseModules, getLessonById } from '../data/courseData';
import './Lesson.css';

const Lesson: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { markLessonCompleted } = useAppContext();
  const lessonId = id ? Number(id) : 1;
  const lessonEntry = getLessonById(lessonId) || getLessonById(1);

  const [code, setCode] = useState('// Опишите решение или вставьте свой фрагмент логики здесь');
  const [taskStatus, setTaskStatus] = useState('');
  const [taskSolved, setTaskSolved] = useState(false);
  const [activeTab, setActiveTab] = useState<'theory' | 'task'>('theory');

  const fallbackModule = courseModules[0];
  const fallbackLesson = fallbackModule.lessons[0];
  const module = lessonEntry?.module || fallbackModule;
  const lesson = lessonEntry?.lesson || fallbackLesson;
  const currentIndex = allLessonIds.indexOf(lesson.id);
  const nextLessonId = currentIndex >= 0 && currentIndex < allLessonIds.length - 1 ? allLessonIds[currentIndex + 1] : null;
  const isTheoryOnlyLesson = lesson.format === 'theory' && !lesson.task;

  const relatedPractice = useMemo(() => module.practice, [module]);

  const checkTask = () => {
    const trimmed = code.trim();

    if (trimmed.length < 20) {
      setTaskStatus('Ответ слишком короткий. Добавьте более конкретное описание логики или код.');
      setTaskSolved(false);
      return;
    }

    if (lesson.successHint && !trimmed.toLowerCase().includes(trimmed.split(' ')[0].toLowerCase())) {
      setTaskStatus(`Ответ сохранён. Ориентир для самопроверки: ${lesson.successHint}`);
      setTaskSolved(true);
      return;
    }

    setTaskStatus(`Задание принято. Ориентир для самопроверки: ${lesson.successHint || 'сверьтесь с теорией урока и практикой модуля.'}`);
    setTaskSolved(true);
  };

  const goToNextLesson = () => {
    markLessonCompleted(lesson.id);

    if (nextLessonId) {
      navigate(`/lesson/${nextLessonId}`);
      return;
    }

    navigate('/course');
  };

  return (
    <div className="lesson-page">
      <div className={`lesson-container ${isTheoryOnlyLesson ? 'lesson-container--theory-only' : ''}`}>
        <div className={`lesson-content ${isTheoryOnlyLesson ? 'lesson-content--centered' : ''}`}>
          <div className="lesson-header">
            <div>
              <Link to={`/topic/${module.id}`} className="back-link">← Назад к модулю {module.order}</Link>
              <h1>{lesson.title}</h1>
              <p className="lesson-subtitle">{module.title}</p>
            </div>
            <div className="lesson-header__meta">
              <span className="theme-tag">{lesson.duration}</span>
              <span className={`format-tag format-tag--${lesson.format}`}>
                {lesson.format === 'practice' ? 'Видео + задание' : 'Видео + теория'}
              </span>
            </div>
          </div>

          <div className="lesson-video">
            <div className="video-player">
              <div className="video-placeholder">
                <div className="play-button">▶</div>
                <p>Здесь будет встроенное видео урока из Rutube</p>
              </div>
            </div>
          </div>

          <div className="lesson-text">
            <h2>О чем урок</h2>
            <p>{lesson.summary}</p>

            <h2>Теория</h2>
            <div className="theory-list">
              {lesson.theory.map((paragraph) => (
                <p key={paragraph}>{paragraph}</p>
              ))}
            </div>

            <div className="lesson-note">
              <strong>Цель модуля:</strong> {module.goal}
            </div>

            <div className="lesson-note">
              <strong>Практика модуля:</strong>
              <ul className="module-practice-inline">
                {relatedPractice.length > 0 ? (
                  relatedPractice.map((item, index) => (
                    <li key={`${item.type}-${index}`}>{item.type}: {item.text}</li>
                  ))
                ) : (
                  <li>Этот модуль завершает курс и задает вектор дальнейшего развития.</li>
                )}
              </ul>
            </div>
          </div>

          {lesson.format === 'theory' && (
            <button className="next-lesson-btn" onClick={goToNextLesson}>
              {nextLessonId ? 'Следующий урок' : 'Вернуться к курсу'}
            </button>
          )}
        </div>

        {lesson.format === 'practice' && (
          <aside className="lesson-task-area">
            <div className="task-switcher">
              <button onClick={() => setActiveTab('theory')} className={activeTab === 'theory' ? 'active' : ''}>Теория</button>
              <button onClick={() => setActiveTab('task')} className={activeTab === 'task' ? 'active' : ''}>Задание</button>
            </div>

            {activeTab === 'theory' ? (
              <div className="task-box">
                <h2>Краткая опора</h2>
                <p>{lesson.summary}</p>
                <ul className="compact-points">
                  {lesson.theory.map((paragraph) => (
                    <li key={paragraph}>{paragraph}</li>
                  ))}
                </ul>
              </div>
            ) : (
              <div className="task-box">
                <h2>Задание</h2>
                <p>{lesson.task || 'Зафиксируйте, как вы будете применять идеи урока на практике.'}</p>

                <div className="code-editor">
                  <textarea value={code} onChange={(e) => setCode(e.target.value)} className="code-input" spellCheck={false} />
                </div>

                <div className="code-controls">
                  <button onClick={checkTask} className="check-button">Проверить</button>
                </div>

                {taskStatus && <p className="task-status">{taskStatus}</p>}

                {taskSolved && (
                  <button className="next-lesson-btn" onClick={goToNextLesson}>
                    {nextLessonId ? 'Следующий урок' : 'Вернуться к курсу'}
                  </button>
                )}
              </div>
            )}
          </aside>
        )}
      </div>
    </div>
  );
};

export default Lesson;
