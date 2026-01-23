import apiService from '@/services/api'
 //获取设备
export function getDevices() {
  return apiService.get(`/Devices`);
}
//添加设备
export function addDevice(params) {
  return apiService.post(`/Devices/addDevice`, params);
}
//修改设备
export function updateDevice(params) {
  return apiService.post(`/Devices/updateDevice`, params);
}
//删除设备
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
//获取数据历史
export function getHistoryData(params) {
  return apiService.get(`/DeviceTable/dataByHistory`, { params: params });
}
//添加设备因子
export function addFactor(params) {
  return apiService.post(`/DeviceTable/addFactor`, params);
}
////修改设备因子
export function updateFactor(params) {
  return apiService.post(`/DeviceTable/updateFactor`,params);
}
//删除设备因子
export function deleteFactor(params) {
  return apiService.get(`/DeviceTable/deleteFactor`, { params: params });
}


// 获取报警历史列表（带分页和查询条件）
export function getAlarmHistory(params) {
  return apiService.get(`/Alarm/history`, { params: params });
}

// 获取报警统计信息
export function getAlarmStats() {
  return apiService.get(`/Alarm/stats`);
}

// 删除单条报警记录
export function deleteAlarm(id) {
  return apiService.delete(`/Alarm/${id}`);
}

// 批量删除报警记录
export function batchDeleteAlarm(ids) {
  return apiService.delete(`/Alarm/batch`, { data: ids });
}