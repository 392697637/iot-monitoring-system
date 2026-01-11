<template>
  <div id="app">
    <!-- 顶部导航栏 -->
    <header class="app-header">
      <h1>IoT 仪表盘系统</h1>
    </header>

    <div class="app-container">
      <!-- 侧边菜单 -->
      <aside class="sidebar">
        <nav>
          <ul>
            <li>
              <router-link to="/">首页</router-link>
            </li>
            <li>
              <router-link to="/device/1">设备详情 A</router-link>
            </li>
            <li>
              <router-link to="/device/2">设备详情 B</router-link>
            </li>
          </ul>
        </nav>
      </aside>

      <!-- 主内容 -->
      <main class="main-content">
        <router-view></router-view>
      </main>
    </div>

    <!-- 底部 -->
    <footer class="app-footer">
      <p>&copy; 2026 IoT 仪表盘系统</p>
    </footer>
  </div>
</template>

<script setup>
</script>

<style>
/* 全局布局样式 */
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  color: #2c3e50;
  height: 100vh;
  display: flex;
  flex-direction: column;
  margin: 0;
}

/* 顶部导航栏 */
.app-header {
  background-color: #409eff;
  color: white;
  padding: 10px 20px;
}

/* 主体布局：侧边 + 内容 */
.app-container {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* 侧边栏 */
.sidebar {
  width: 200px;
  background-color: #f5f5f5;
  padding: 20px;
  overflow-y: auto;
}

.sidebar ul {
  list-style: none;
  padding: 0;
}

.sidebar li {
  margin-bottom: 10px;
}

.sidebar a {
  text-decoration: none;
  color: #333;
}

.sidebar a.router-link-active {
  font-weight: bold;
  color: #409eff;
}

/* 主内容 */
.main-content {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
  background-color: #ffffff;
}

/* 底部 */
.app-footer {
  background-color: #f5f5f5;
  padding: 10px 20px;
  text-align: center;
  font-size: 14px;
  color: #999;
}
</style>
