import axios from 'axios'

const BASE_URL = 'https://localhost:51215/api'

export function getDevices() {
  return axios.get(`${BASE_URL}/Devices`)
}

export function addDevice(device) {
  return axios.post(`${BASE_URL}/Devices/add`, device)
}

export function getLatestData(deviceId) {
  return axios.get(`${BASE_URL}/DeviceData/latest`, { params: { deviceId } })
}

export function getHistoryData(deviceId, start, end) {
  return axios.get(`${BASE_URL}/DeviceData/history`, { params: { deviceId, start, end } })
}

export function getDeviceHistory(deviceId, start, end) {
  return axios.get(`${BASE_URL}/DeviceData/history`, { params: { deviceId, start, end } })
}
export function getThresholdsByDevice(deviceId) {
  return axios.get(`${BASE_URL}/Thresholds/${deviceId}`)
}
export function getThresholds(deviceId) {
  return axios.get(`${BASE_URL}/Thresholds/${deviceId}`)
}

export function setThreshold(threshold) {
  return axios.post(`${BASE_URL}/Thresholds/set`, threshold)
}
