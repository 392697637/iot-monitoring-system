import axios from "axios";

const BASE_URL = "https://localhost:51215/api";

//获取设备
export function getDevices() {
  return axios.get(`${BASE_URL}/Devices`);
}

export function addDevice(device) {
  return axios.post(`${BASE_URL}/Devices/add`, device);
}

export function getLatestData(deviceId) {
  return axios.get(`${BASE_URL}/DeviceData/latest`, { params: { deviceId } });
}

export function getDeviceHistory(deviceId, start, end) {
  return axios.get(`${BASE_URL}/DeviceData/history`, {
    params: { deviceId, start, end },
  });
}
export function getThresholdsByDevice(deviceId) {
  return axios.get(`${BASE_URL}/Thresholds/${deviceId}`);
}
export function getThresholds(deviceId) {
  return axios.get(`${BASE_URL}/Thresholds/${deviceId}`);
}

export function setThreshold(threshold) {
  return axios.post(`${BASE_URL}/Thresholds/set`, threshold);
}
//获取表字段
export function getDeviceTable(tableName) {
  return axios.get(`${BASE_URL}/DeviceTable/fieldByTableName`, {
    params: { tableName },
  });
}
//获取表数据
export function getDataByTableName(tableName, orderby,topNumber) {
  return axios.get(`${BASE_URL}/DeviceTable/dataByTableName`, {
    params: { tableName, orderby, topNumber },
  });
}

export function getHistoryData(params) {
  return axios.get(`${BASE_URL}/DeviceTable/dataByHistory`, { params: params });
}
