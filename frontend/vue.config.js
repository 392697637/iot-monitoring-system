const { defineConfig } = require("@vue/cli-service");
const webpack = require("webpack");
const path = require("path");
const CompressionPlugin = require("compression-webpack-plugin");
const BundleAnalyzerPlugin =
  require("webpack-bundle-analyzer").BundleAnalyzerPlugin;

module.exports = defineConfig({
  transpileDependencies: true,

  // 配置开发服务器
  devServer: {
    port: 8080,
    host: "localhost",
    open: true,
    hot: true,
    compress: true,
    client: {
      overlay: {
        warnings: false,
        errors: true,
      },
    },
    proxy: {
      "/api": {
        target: process.env.VUE_APP_API_BASE_URL || "http://localhost:5000",
        changeOrigin: true,
        ws: true,
        pathRewrite: {
          "^/api": "/api",
        },
      },
      "/hubs": {
        target: process.env.VUE_APP_API_BASE_URL || "http://localhost:5000",
        changeOrigin: true,
        ws: true,
      },
    },
  },

  // 配置生产构建
  productionSourceMap: false,

  // 配置Webpack
  configureWebpack: (config) => {
    // 开发和生产环境通用配置
    config.resolve = {
      ...config.resolve,
      alias: {
        ...config.resolve.alias,
        "@": path.resolve(__dirname, "src"),
        "@components": path.resolve(__dirname, "src/components"),
        "@views": path.resolve(__dirname, "src/views"),
        "@services": path.resolve(__dirname, "src/services"),
        "@utils": path.resolve(__dirname, "src/utils"),
        "@assets": path.resolve(__dirname, "src/assets"),
      },
      extensions: [".js", ".vue", ".json"],
    };

    // 性能提示
    config.performance = {
      hints: process.env.NODE_ENV === "production" ? "warning" : false,
      maxEntrypointSize: 512000,
      maxAssetSize: 512000,
    };

    // 生产环境特定配置
    if (process.env.NODE_ENV === "production") {
      // 启用Gzip压缩
      config.plugins.push(
        new CompressionPlugin({
          algorithm: "gzip",
          test: /\.(js|css|html|svg)$/,
          threshold: 10240,
          minRatio: 0.8,
        })
      );

      // 代码分割
      config.optimization = {
        ...config.optimization,
        splitChunks: {
          chunks: "all",
          minSize: 20000,
          maxSize: 244000,
          minChunks: 1,
          maxAsyncRequests: 30,
          maxInitialRequests: 30,
          automaticNameDelimiter: "~",
          cacheGroups: {
            vendors: {
              test: /[\\/]node_modules[\\/]/,
              priority: -10,
              name: "chunk-vendors",
            },
            echarts: {
              test: /[\\/]node_modules[\\/]echarts[\\/]/,
              name: "chunk-echarts",
              priority: 20,
            },
            elementUI: {
              test: /[\\/]node_modules[\\/]element-ui[\\/]/,
              name: "chunk-elementUI",
              priority: 20,
            },
            common: {
              minChunks: 2,
              priority: -20,
              reuseExistingChunk: true,
            },
          },
        },
      };

      // 启用Bundle分析
      if (process.env.ANALYZE === "true") {
        config.plugins.push(
          new BundleAnalyzerPlugin({
            analyzerMode: "static",
            reportFilename: "bundle-report.html",
          })
        );
      }
    }
  },

  // 配置CSS
  css: {
    extract: process.env.NODE_ENV === "production",
    sourceMap: false,
    loaderOptions: {
      sass: {
        additionalData: `@import "@/assets/styles/variables.scss";`,
      },
    },
  },

  // PWA配置
  pwa: {
    name: "IoT监测系统",
    themeColor: "#409EFF",
    msTileColor: "#2d3748",
    appleMobileWebAppCapable: "yes",
    appleMobileWebAppStatusBarStyle: "black",

    workboxPluginMode: "InjectManifest",
    workboxOptions: {
      swSrc: "src/service-worker.js",
      exclude: [/\.map$/, /_redirects/],
    },
  },

  // 链式配置
  chainWebpack: (config) => {
    // 图片压缩
    config.module
      .rule("images")
      .test(/\.(png|jpe?g|gif|webp|svg)(\?.*)?$/)
      .use("url-loader")
      .loader("url-loader")
      .options({
        limit: 8192,
        name: "img/[name].[hash:8].[ext]",
      });

    // 字体处理
    config.module
      .rule("fonts")
      .test(/\.(woff2?|eot|ttf|otf)(\?.*)?$/i)
      .use("url-loader")
      .loader("url-loader")
      .options({
        limit: 8192,
        name: "fonts/[name].[hash:8].[ext]",
      });
    // 预加载关键资源
    if (config.plugins.has("preload")) {
      config.plugin("preload").tap(() => [
        {
          rel: "preload",
          as(entry) {
            if (/\.css$/.test(entry)) return "style";
            if (/\.woff2$/.test(entry)) return "font";
            if (/\.png$/.test(entry)) return "image";
            return "script";
          },
          include: "initial",
        },
      ]);
    }
  },
});
