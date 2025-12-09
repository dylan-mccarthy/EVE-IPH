export interface AuthData {
  characterId: number;
  characterName: string;
  accessToken?: string;
  refreshToken?: string;
  expiresAt?: string;
}

export const getAuthData = (): AuthData | null => {
  const authStr = localStorage.getItem('eveAuth');
  if (!authStr) {
    return null;
  }

  try {
    return JSON.parse(authStr) as AuthData;
  } catch {
    return null;
  }
};

export const setAuthData = (data: AuthData): void => {
  localStorage.setItem('eveAuth', JSON.stringify(data));
};

export const clearAuthData = (): void => {
  localStorage.removeItem('eveAuth');
};

export const isTokenExpired = (expiresAt?: string): boolean => {
  if (!expiresAt) {
    return true;
  }

  const expiry = new Date(expiresAt);
  const now = new Date();
  
  // Consider expired if less than 5 minutes remaining
  const fiveMinutesFromNow = new Date(now.getTime() + 5 * 60 * 1000);
  
  return expiry < fiveMinutesFromNow;
};

export const shouldReauthenticate = (): boolean => {
  const auth = getAuthData();
  
  if (!auth) {
    return true;
  }

  // If we have a refresh token, we can keep the session alive
  if (auth.refreshToken) {
    return false;
  }

  // Otherwise check if token is expired
  return isTokenExpired(auth.expiresAt);
};
