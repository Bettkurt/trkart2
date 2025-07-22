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
      setMessage('Giriş başarılı! Token: ' + response.data.token);
    } catch (error) {
      setMessage('Giriş başarısız: ' + (error.response?.data || error.message));
    }
  };

  return (
    <div>
      <h2>Giriş Yap</h2>
      <form onSubmit={handleSubmit}>
        <input
          type="email"
          name="email"
          placeholder="E-posta"
          value={formData.email}
          onChange={handleChange}
          required
        /><br/>
        <input
          type="password"
          name="password"
          placeholder="Şifre"
          value={formData.password}
          onChange={handleChange}
          required
        /><br/>
        <button type="submit">Giriş Yap</button>
      </form>
      {message && <p>{message}</p>}
    </div>
  );
}

export default LoginForm;
