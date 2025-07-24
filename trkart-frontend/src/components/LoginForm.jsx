import React, { useState } from 'react';
import { login } from '../api/auth';

function LoginForm() {
  const [formData, setFormData] = useState({
    email: '',
    password: ''
  });

  const [message, setMessage] = useState('');

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const response = await login(formData);
      if (response.data.token) {
        localStorage.setItem('token', response.data.token);
      }
      setMessage('Giriş başarılı!');
    } catch (error) {
      let errMsg = 'Giriş başarısız: ';
      if (error.response?.data?.message) {
        errMsg += error.response.data.message;
      } else if (typeof error.response?.data === 'string') {
        errMsg += error.response.data;
      } else {
        errMsg += error.message;
      }
      setMessage(errMsg);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="email"
        name="email"
        placeholder="E-posta"
        value={formData.email}
        onChange={handleChange}
        required
      />
      <input
        type="password"
        name="password"
        placeholder="Şifre"
        value={formData.password}
        onChange={handleChange}
        required
      />
      <button type="submit">Giriş Yap</button>
      {message && (
        <div className={message.startsWith('Giriş başarılı') ? 'success' : 'error'}>{message}</div>
      )}
    </form>
  );
}

export default LoginForm;
