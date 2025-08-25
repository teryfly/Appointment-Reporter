import axios from 'axios';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5261';

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
    
    // 检查响应数据结构
    if (response.data && typeof response.data === 'object') {
      // 如果有success字段且为false，抛出错误
      if (response.data.success === false) {
        return Promise.reject(new Error(response.data.message || '请求失败'));
      }
      // 如果有data字段，返回data；否则返回整个响应数据
      return {
        ...response,
        data: response.data.data !== undefined ? response.data.data : response.data
      };
    }
    
    return response;
  },
  (error) => {
    console.error('API Response Error:', error);
    
    // 处理HTTP错误状态码
    if (error.response) {
      const { status, data } = error.response;
      let message = `请求失败 (${status})`;
      
      if (data && typeof data === 'object') {
        message = data.message || data.error || message;
      } else if (typeof data === 'string') {
        message = data;
      }
      
      return Promise.reject(new Error(message));
    }
    
    // 处理网络错误
    if (error.request) {
      return Promise.reject(new Error('网络连接失败，请检查网络设置'));
    }
    
    return Promise.reject(error);
  }
);