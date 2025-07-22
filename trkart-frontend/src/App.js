import React from 'react';
import './App.css';
import RegisterForm from './components/RegisterForm';
import LoginForm from './components/LoginForm';


function App() {
   return (
    <div className="App">
      <RegisterForm />
      <hr />
      <LoginForm />
    </div>
  );
}

export default App;
