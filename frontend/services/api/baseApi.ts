import axios from 'axios';

const baseURL = (import.meta as any).env?.VITE_API_BASE_URL || 'http://localhost:5261';

export const api = axios.create({
  baseURL,
  timeout: 30000,
  withCredentials: false,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use(
  (config) => {
    console.log('API Request:', config.method?.toUpperCase(), config.url, config.params);
    return config;
  },
  (error) => {
    console.error('API Request Error:', error);
    return Promise.reject(error);
  }
);

api.interceptors.response.use(
  (response) => {
    console.log('API Response:', response.status, response.config.url, response.data);
    
    if (response.data && typeof response.data === 'object') {
      if ((response.data as any).success === false) {
        return Promise.reject(new Error((response.data as any).message || '请求失败'));
      }
      return {
        ...response,
        data: (response.data as any).data !== undefined ? (response.data as any).data : response.data
      };
    }
    
    return response;
  },
  (error) => {
    console.error('API Response Error:', error);
    
    if (error.response) {
      const { status, data } = error.response;
      let message = `请求失败 (${status})`;
      
      if (data && typeof data === 'object') {
        message = (data as any).message || (data as any).error || message;
      } else if (typeof data === 'string') {
        message = data;
      }
      
      return Promise.reject(new Error(message));
    }
    
    if (error.request) {
      return Promise.reject(new Error('网络连接失败，请检查网络设置'));
    }
    
    return Promise.reject(error);
  }
);