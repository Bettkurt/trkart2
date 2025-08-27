// Cookie utility functions
export const setCookie = (name: string, value: string, days: number = 7): void => {
  const date = new Date();
  date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
  const expires = `expires=${date.toUTCString()}`;
  document.cookie = `${name}=${value};${expires};path=/;SameSite=Strict`;
};

export const getCookie = (name: string): string | null => {
  const nameEQ = `${name}=`;
  for (const cookie of document.cookie.split(';')) {
    let c = cookie.trimStart();
    if (c.startsWith(nameEQ)) return c.substring(nameEQ.length);
  }
  return null;
};

export const deleteCookie = (name: string): void => {
  document.cookie = `${name}=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;`;
};

export const isTokenExpired = (expirationKey: string): boolean => {
  const expiration = getCookie(expirationKey);
  if (!expiration) return true;
  
  try {
    const expirationDate = new Date(expiration);
    return expirationDate <= new Date();
  } catch (e) {
    console.error('Error parsing token expiration:', e);
    return true;
  }
};
