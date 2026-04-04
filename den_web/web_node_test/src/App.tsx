import { BrowserRouter as Router, Navigate, Route, Routes } from 'react-router-dom';
import Landing from './pages/Landing';
import Dashboard from './pages/Dashboard';
import Lesson from './pages/Lesson';
import Login from './pages/Login';
import Register from './pages/Register';
import Plugin from './pages/Plugin';
import Course from './pages/Course';
import Topic from './pages/Topic';
import Profile from './pages/Profile';
import Privacy from './pages/Privacy';
import Terms from './pages/Terms';
import Header from './components/Header';
import Footer from './components/Footer';
import LogoutModal from './components/LogoutModal';
import { AppProvider, useAppContext } from './context/AppContext';
import './App.css';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { user } = useAppContext();

  if (!user) {
    return <Navigate to="/register" replace />;
  }

  return children;
}

function App() {
  return (
    <AppProvider>
      <Router>
        <div className="App">
          <Header />

          <main className="main-layout">
            <Routes>
              <Route path="/" element={<Landing />} />
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />
              <Route
                path="/dashboard"
                element={
                  <ProtectedRoute>
                    <Dashboard />
                  </ProtectedRoute>
                }
              />
              <Route path="/plugin" element={<Plugin />} />
              <Route
                path="/course"
                element={
                  <ProtectedRoute>
                    <Course />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/topic/:id"
                element={
                  <ProtectedRoute>
                    <Topic />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/lesson/:id"
                element={
                  <ProtectedRoute>
                    <Lesson />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/profile"
                element={
                  <ProtectedRoute>
                    <Profile />
                  </ProtectedRoute>
                }
              />
              <Route path="/privacy" element={<Privacy />} />
              <Route path="/terms" element={<Terms />} />
            </Routes>
          </main>

          <Footer />
          <LogoutModal />
        </div>
      </Router>
    </AppProvider>
  );
}

export default App;
