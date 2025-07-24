import React, { useState } from 'react';
import './App.css';
import RegisterForm from './components/RegisterForm';
import LoginForm from './components/LoginForm';

function App() {
  const [showLogin, setShowLogin] = useState(false);

  const handleRegisterSuccess = () => {
    setShowLogin(true);
  };

  return (
    <div className="auth-container">
      {!showLogin ? (
        <div className="form-box">
          <h2>Kayıt Ol</h2>
          <RegisterForm onRegisterSuccess={handleRegisterSuccess} />
        </div>
      ) : (
        <div className="form-box">
          <h2>Giriş Yap</h2>
          <LoginForm />
        </div>
      )}
    </div>
  );
}

export default App;
