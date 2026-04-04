import React from 'react';
import { Link } from 'react-router-dom';
import './Landing.css';

const advantages = [
  {
    title: 'Двусторонняя конвертация',
    description: 'Переводите визуальные ноды в C# и разбирайте код обратно в понятный граф без потери логики.',
  },
  {
    title: 'Курс внутри платформы',
    description: 'От первого запуска Unity до уверенной работы с C#: обучение встроено прямо в экосистему продукта.',
  },
  {
    title: 'Практика на реальных задачах',
    description: 'Каждый модуль ведет к рабочим игровым механикам, мини-проектам и финальной сборке Space Invaders.',
  },
  {
    title: 'Понятный путь для новичка',
    description: 'Курс связывает интерфейс Unity, визуальное программирование и основы кода в одну непрерывную траекторию.',
  },
];

const modules = [
  {
    number: '01',
    title: 'Первые шаги в Unity',
    focus: 'Установка Unity, обзор интерфейса, импорт плагина и знакомство с визуальным редактором нод.',
  },
  {
    number: '02',
    title: 'Как мыслит Unity',
    focus: 'Методы Start и Update, переменные, передача данных в логику движения и первые связи между узлами.',
  },
  {
    number: '03',
    title: 'Общение объектов',
    focus: 'Ссылки, теги, события, реакция на столкновения и построение интеракций вроде кнопки и двери.',
  },
  {
    number: '04',
    title: 'Собираем Space Invaders',
    focus: 'Игрок, стрельба, враги, очки, победа и практика на полноценном игровом проекте.',
  },
  {
    number: '05',
    title: 'От нод к C#',
    focus: 'Разбираем, как визуальные блоки превращаются в код, и учимся читать сгенерированные скрипты.',
  },
  {
    number: '06',
    title: 'Практика с C# в Unity',
    focus: 'Разбираем, как переносить сложную игровую логику в C# и применять ее в проекте Unity.',
  },
  {
    number: '07',
    title: 'Из кода обратно в ноды',
    focus: 'Реверс-инжиниринг: берем готовый C# скрипт и анализируем, как он распадается на граф.',
  },
  {
    number: '08',
    title: 'Выпускной модуль',
    focus: 'Подводим итоги, показываем профессиональные сценарии применения и намечаем путь дальнейшего роста.',
  },
];

const learningPath = [
  'Интерфейс Unity и рабочее окружение без лишнего стресса',
  'Переменные, методы, события и логика объектов через визуальные связи',
  'Переход от нод к чтению C# без резкого порога входа',
  'Понимание, где C# усиливает визуальный пайплайн и упрощает сложную логику',
];

const Landing: React.FC = () => {
  return (
    <div className="landing-page">
      <section className="landing-hero">
        <div className="landing-hero__content">
          <span className="landing-badge">Плагин + встроенный курс для Unity</span>
          <h1>Мост между визуальным программированием и кодом в Unity</h1>
          <p className="landing-hero__lead">
            Платформа помогает работать с нодами и C# в обе стороны, а встроенный курс проводит
            пользователя от знакомства с интерфейсом Unity до анализа кода и практики со скриптами.
          </p>

          <div className="landing-hero__actions">
            <Link to="/plugin" className="landing-button landing-button--primary">
              Скачать плагин
            </Link>
            <Link to="/course" className="landing-button landing-button--ghost">
              Начать обучение
            </Link>
          </div>

          <ul className="landing-hero__stats">
            <li>
              <strong>8 модулей</strong>
              <span>от основ Unity до практики с C#</span>
            </li>
            <li>
              <strong>Space Invaders</strong>
              <span>как итоговый проект курса</span>
            </li>
            <li>
              <strong>Ноды ↔ C#</strong>
              <span>понятный переход между двумя форматами</span>
            </li>
          </ul>
        </div>

        <div className="landing-hero__visual">
          <div className="hero-panel hero-panel--graph">
            <div className="hero-panel__label">Визуальная логика</div>
            <div className="hero-node hero-node--accent">On Start</div>
            <div className="hero-link" />
            <div className="hero-node">Speed: 5</div>
            <div className="hero-link" />
            <div className="hero-node hero-node--success">Move Forward</div>
          </div>

          <div className="hero-panel hero-panel--code">
            <div className="hero-panel__label">C# представление</div>
            <pre>{`void Update()
{
    transform.Translate(
        Vector3.forward * speed * Time.deltaTime
    );
}`}</pre>
          </div>
        </div>
      </section>

      <section className="landing-section landing-section--advantages">
        <div className="landing-section__header">
          <span className="landing-eyebrow">Что умеет платформа</span>
          <h2>Сразу показывает, о чем сайт и зачем он нужен</h2>
          <p>
            Главная идея проекта в том, чтобы убрать разрыв между визуальной логикой и текстовым кодом,
            а обучение сделать встроенной частью продукта, а не отдельным абстрактным разделом.
          </p>
        </div>

        <div className="advantages-grid">
          {advantages.map((item) => (
            <article key={item.title} className="info-card">
              <h3>{item.title}</h3>
              <p>{item.description}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="landing-section landing-section--course-callout">
        <div className="course-callout">
          <div>
            <span className="landing-eyebrow">Анонс курса</span>
            <h2>Обучение встроено в сам продукт, а не существует отдельно от него</h2>
            <p>
              Курс ведет от установки Unity и первых сцен к игровым механикам, чтению C#,
              работе со скриптами и обратной компиляции кода в граф.
            </p>
          </div>

          <div className="course-callout__path">
            {learningPath.map((item) => (
              <div key={item} className="path-item">
                <span className="path-item__dot" />
                <p>{item}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="landing-section landing-section--modules">
        <div className="landing-section__header">
          <span className="landing-eyebrow">Программа курса</span>
          <h2>Структура курса: от освоения Unity до реверс-инжиниринга</h2>
          <p>
            Каждый модуль решает понятную учебную задачу: сначала снять барьер входа, затем объяснить
            логику Unity, научить собирать механики и показать, как визуальные блоки связаны с C#.
          </p>
        </div>

        <div className="modules-grid">
          {modules.map((module) => (
            <article key={module.number} className="module-card">
              <span className="module-card__number">{module.number}</span>
              <h3>{module.title}</h3>
              <p>{module.focus}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="landing-section landing-section--final">
        <div className="final-cta">
          <div>
            <span className="landing-eyebrow">Для кого это</span>
            <h2>Для тех, кто хочет понять Unity глубже, не теряя наглядность</h2>
            <p>
              Сайт одновременно объясняет продукт и показывает учебную траекторию: здесь удобно скачать
              плагин, зайти в курс и сразу увидеть, как визуальная логика приводит к настоящему коду.
            </p>
          </div>

          <div className="final-cta__actions">
            <Link to="/plugin" className="landing-button landing-button--primary">
              Перейти к плагину
            </Link>
            <Link to="/register" className="landing-button landing-button--ghost">
              Создать аккаунт
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
};

export default Landing;
