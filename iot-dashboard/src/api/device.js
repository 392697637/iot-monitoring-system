import apiService from '@/services/api'
 

//获取设备
export function getDevices() {
  return apiService.get(`/Devices`);
}
export function addDevice(params) {
  return apiService.post(`/Devices/add`, params);
}
export function deleteDevice(deviceId) {
  return apiService.get(`/Devices/deleteDevice`, {   params: { deviceId },   });
}
//获取表字段
export function getDeviceTable(tableName) {
  return apiService.get(`/DeviceTable/fieldByTableName`, {
    params: { tableName },
  });
}
//获取表数据
export function getDataByTableName(tableName, orderby,topNumber) {
  return apiService.get(`/DeviceTable/dataByTableName`, {
    params: { tableName, orderby, topNumber },
  });
}
//获取历史数据
export function getHistoryData(params) {
  return apiService.get(`/DeviceTable/dataByHistory`, { params: params });
}
export function updateFactor(params) {
  return apiService.get(`/DeviceTable/updateFactor`, { params: params });
}



export function getThresholdsByDevice(deviceId) {
  return apiService.get(`/Thresholds/${deviceId}`);
}
export function getThresholds(deviceId) {
  return apiService.get(`/Thresholds/${deviceId}`);
}

export function setThreshold(threshold) {
  return apiService.post(`/Thresholds/set`, threshold);
}