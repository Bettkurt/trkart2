import React, { useState } from 'react';
import { register, login } from '../api/auth';

const RegisterForm = ({ onRegisterSuccess }) => {
  const [fullname, setFullname] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const handleRegister = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");
    try {
      const result = await register({ fullname, email, password });
      setSuccess(result.data?.message || result.data || "Kayıt başarılı!");
      if (onRegisterSuccess) {
        setTimeout(() => {
          onRegisterSuccess();
        }, 1000);
      }
    } catch (err) {
      setError("Hata: " + (err.response?.data?.message || err.response?.data || "Network Error"));
    }
  };

  return (
    <form onSubmit={handleRegister}>
      <input
        type="text"
        placeholder="Ad Soyad"
        value={fullname}
        onChange={e => setFullname(e.target.value)}
        required
      />
      <input
        type="email"
        placeholder="Email"
        value={email}
        onChange={e => setEmail(e.target.value)}
        required
      />
      <input
        type="password"
        placeholder="Şifre"
        value={password}
        onChange={e => setPassword(e.target.value)}
        required
      />
      <button type="submit">Kayıt Ol</button>
      {success && <div className="success">{success}</div>}
      {error && <div className="error">{error}</div>}
    </form>
  );
}

export default RegisterForm;
