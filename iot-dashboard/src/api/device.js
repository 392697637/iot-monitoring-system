import axios from 'axios';

const API_BASE = 'https://localhost:5001/api'; // .NET 8 后端地址

export const getDevices = () => axios.get(`${API_BASE}/device`);

export const getDeviceFactors = (deviceId) => axios.get(`${API_BASE}/device/${deviceId}/factors`);

export const getRealtimeData = (deviceId) => axios.get(`${API_BASE}/device/${deviceId}/realtime`);

export const getHistoryData = (deviceId, factor, start, end) => {
  return axios.get(`${API_BASE}/device/${deviceId}/history`, {
    params: { factor, start, end }
  });
};

export const getDeviceAlarms = (deviceId) => axios.get(`${API_BASE}/device/${deviceId}/alarm`);
