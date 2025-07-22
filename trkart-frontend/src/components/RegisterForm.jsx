import React, { useState } from 'react';
import { register, login } from '../api/auth';

const RegisterForm = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    fullName: ''
  });
  const [message, setMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    setMessage('');

    console.log('Sending registration data:', formData); // Debug için

    try {
      const registerRes = await register(formData);
      console.log('Registration successful:', registerRes); // Debug için
      setMessage('Kayıt başarılı! Otomatik giriş yapılıyor...');
      
      // Auto-login after successful registration
      setTimeout(async () => {
        try {
          const loginRes = await login({
            email: formData.email,
            password: formData.password
          });
          setMessage(`Giriş başarılı! Token: ${loginRes.data.token}`);
          
          // Store token in localStorage
          localStorage.setItem('token', loginRes.data.token);
          
        } catch (loginErr) {
          setMessage('Kayıt başarılı ancak otomatik giriş başarısız. Lütfen manuel giriş yapın.');
        }
      }, 1000);
      
    } catch (err) {
      console.error('Registration error:', err); // Debug için
      console.error('Error response:', err.response); // Debug için
      
      const errorMessage = err.response?.data?.message || 
                          err.message || 
                          'Kayıt sırasında hata oluştu';
      setMessage(`Hata: ${errorMessage}`);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div>
      <h2>Kayıt Ol</h2>
      <form onSubmit={handleSubmit}>
        <input 
          type="text" 
          name="fullName" 
          placeholder="Ad Soyad (İsteğe bağlı)" 
          value={formData.fullName}
          onChange={handleChange} 
        />
        <input 
          type="email" 
          name="email" 
          placeholder="Email" 
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
        <button type="submit" disabled={isLoading}>
          {isLoading ? 'İşleniyor...' : 'Kayıt Ol'}
        </button>
      </form>
      <p>{message}</p>
    </div>
  );
};

export default RegisterForm;
