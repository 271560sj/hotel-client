using System;
using System.Diagnostics;
using System.IO;
using HotelUpdateService.update.entity;
using HotelUpdateService.update.utils;
using Newtonsoft.Json.Linq;

namespace HotelUpdateService.update.service
{
    /// <summary>
    /// 检查版本信息，下载新版本软件
    /// </summary>
    class UpdateVersion
    {
        /// <summary>
        /// 操作http请求的工具类
        /// </summary>
        private static HttpUtils http = HttpUtils.getInstance();

        /// <summary>
        /// 私有构造方法
        /// </summary>
        #region private UpdateVersion()
        private UpdateVersion() { }
        #endregion

        /// <summary>
        /// 单实例模式
        /// </summary>
        /// <returns></returns>
        #region public static UpdateVersion getInstance()
        public static UpdateVersion getInstance()
        {
            return new UpdateVersion();
        }
        #endregion

        /// <summary>
        /// 检查版本服务器中是否具有新的版本
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        #region public ResultEntity checkVersionFromServer(String version)
        public ResultEntity checkVersionFromServer(String version)
        {
            //记录返回结果
            ResultEntity result = null;
            //获取服务器地址
            String server = getServerUrl();
            //判断服务器地址是否为空
            if (String.IsNullOrEmpty(server) || String.IsNullOrEmpty(version))
            {
                Logger.info(typeof(UpdateVersion), "local app version or version server url is empty.");
                return result;
            }
            //设置检查版本路径
            String url = String.Format(@"{0}/check/app/{1}", server, version);
            //发送版本检查请求
            String request = http.get(url);
            //解析返回结果
            result = JsonUtils.getResultEntity(request);
            return result;
        }

        #endregion

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        #region public bool downloadFile(String path, String name, long localSize)
        public bool downloadFile(String path, String name, long localSize)
        {
            //标志请求结果
            bool isSuccess = false;

            //判断远程文件路径和文件名称是否为空
            if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(name))
            {
                Logger.info(typeof(UpdateVersion), "request remote file path and name is empty.");
                return isSuccess;
            }

            //构建请求消息体
            PostData post = new PostData()
            {
                remote = path,
                fileName = name,
                localSize = localSize
            };

            //获取服务器的url地址
            String server = getServerUrl();
            if (String.IsNullOrEmpty(server))
            {
                Logger.info(typeof(UpdateVersion), "app version manager server is empty.");
                return isSuccess;
            }
            //构造请求url
            String url = String.Format(@"{0}/download/ftp", server);

            //开始下载文件
            bool flag = http.download(url, post);
            //判断文件下载结果
            if(!flag)
            {
                Logger.info(typeof(UpdateVersion), "download file error.");
                return isSuccess;
            }
            isSuccess = true;
            return isSuccess;
        }
        #endregion

        /// <summary>
        /// 获取版本管理服务器的地址
        /// </summary>
        /// <returns></returns>
        #region private String getServerUrl()
        private String getServerUrl()
        {
            //获取服务信息配置文件
            String path = String.Format(@"{0}config\version-server.json", CommonUtils.getServiceRunningPath());
            Logger.info(typeof(UpdateVersion), String.Format("version manager server config file path is {0}", path));
            //获取服务器地址json字符串
            String server = JsonUtils.readJson(path);
            if (String.IsNullOrEmpty(server))
            {
                Logger.info(typeof(UpdateVersion), "version manager server info is empty.");
                return null;
            }

            JObject obj = JObject.Parse(server);
            //获取地址信息
            String host = obj.SelectToken("server").ToString();
            String port = obj.SelectToken("port").ToString();

            if(String.IsNullOrEmpty(host) || String.IsNullOrEmpty(port))
            {
                Logger.info(typeof(UpdateVersion), "can not get version manager server info.");
                return null;
            }

            return String.Format(@"{0}:{1}", host, port);
        }
        #endregion

        /// <summary>
        /// 获取服务器中文件的hash值
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        #region public String getHashFromServer(String fileName)
        public String getHashFromServer(String fileName)
        {
            //记录获取的hash值
            String hash = String.Empty;
            //构建请求的url
            String url = String.Format(@"{0}/check/hash/{1}", getServerUrl(), fileName);
            //开始查询
            String result = http.get(url);
            if (String.IsNullOrEmpty(result))
            {
                Logger.info(typeof(UpdateVersion), "get file hash value from server is failed.");
                return hash;
            }
            //解析返回结果
            ResultEntity entity = JsonUtils.getResultEntity(result);
            if(entity == null)
            {
                Logger.info(typeof(UpdateVersion), "get response value error.");
                return hash;
            }
            //返回查询到的hash值
            hash = entity.hash;
            return hash;
        }
        #endregion

        /// <summary>
        /// 备份本地已经安装的软件的信息
        /// </summary>
        /// <returns></returns>
        #region public bool backLocalAppInfo(out String installPath)
        public bool backLocalAppInfo(out String installPath)
        {
            //标识是否备份成功
            bool back = false;
            //记录查询到版本信息与软件名称
            String version;
            String name;
            //从配置文件中获取到当前运行的程序的版本和名称
            back = CommonUtils.getVersionFromConfigFile(String.Format(@"{0}config\version.xml", CommonUtils.getServiceRunningPath()), out version, out name);
            if (!back)//获取信息失败
            {
                Logger.info(typeof(UpdateVersion), "get local app version error.");
                installPath = String.Empty;
                return back;
            }
            String path;
            //获取程序运行的路径
            Process process = CommonUtils.getProcessInstalled(name, out path);
            if(process == null || String.IsNullOrEmpty(path))//如果获取路径失败，则根据升级程序的运行路径作为备份路径
            {
                String runPath = CommonUtils.getServiceRunningPath();
                String defaultPath = runPath.Substring(0, runPath.LastIndexOf(@"\"));

                path = String.Format(@"{0}", defaultPath.Substring(0, defaultPath.LastIndexOf(@"\")));
            }
            else
            {
                //杀死进程
                process.Kill();
            }
            //开始备份
            back = CommonUtils.backAppInfo(path, version, name);
            installPath = path;
            return back;
        }
        #endregion

        /// <summary>
        /// 检查安装完成以后，安装的状态
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        #region public bool checkInstallState(String appName)
        public bool checkInstallState(String appName)
        {
            //记录结果
            bool result = false;
            //判断传入的参数是否合法
            if (String.IsNullOrEmpty(appName))
            {
                Logger.warn(typeof(UpdateVersion), "can not get validated app name info when check installed status.");
                return result;
            }

            //  todo 软件提供的检查软件是否正常的方式

            //todo 检查软件已经正常启动，则把新的版本信息写入配置文件

            //返回结果
            return result;
        }
        #endregion

        /// <summary>
        /// 用于检查版本是否需要更新
        /// </summary>
        /// <param name="path"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        #region public bool checkVersion(out String path, out String name)
        public bool checkVersion(out String path, out String name)
        {
            //获取当前程序版本
            String versionFilePath = CommonUtils.getServiceRunningPath();
            String file = String.Format(@"{0}config\version.xml", versionFilePath);
            String oldVersion;
            bool flag = CommonUtils.getVersionFromConfigFile(file, out oldVersion, out String appName);
            if (!flag)
            {
                Logger.warn(typeof(UpdateVersion), "get old app version error.");
                path = String.Empty;
                name = String.Empty;
                return false;
            }
            //获取服务器中最新版本信息
            ResultEntity result = checkVersionFromServer(oldVersion);
            if (result == null)
            {
                Logger.warn(typeof(UpdateVersion), "check app version from server failed.");
                path = String.Empty;
                name = String.Empty;
                return false;
            }
            //判断服务器是否返回更新路径
            String requestUrl = result.path;
            if (String.IsNullOrEmpty(requestUrl))
            {
                Logger.warn(typeof(UpdateVersion), "check app version from server failed.");
                path = String.Empty;
                name = String.Empty;
                return false;
            }
            //解析更新路径
            path = requestUrl.Substring(0, requestUrl.LastIndexOf("/"));
            name = requestUrl.Substring(requestUrl.LastIndexOf("/") + 1);
            return true;
        }
        #endregion

        /// <summary>
        /// 检查文件是否存在，获取初始文件大小
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        #region public long checkFileExist(String name)
        public long checkFileExist(String name)
        {
            //记录本地文件大小
            long size = 0;
            //获取本地文件的全路径信息
            String fullName = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), name);
            //判断文件是否存在
            try
            {
                if (File.Exists(fullName))//文件存在返回文件大小
                {
                    FileStream stream = new FileStream(fullName, FileMode.Open, FileAccess.Read);
                    size = stream.Length;
                    stream.Flush();
                    stream.Close();
                    return size;
                }
            }
            catch (IOException ex)//异常处理
            {
                Logger.error(typeof(UpdateVersion), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(UpdateVersion), ex);
            }
            return size;
        }
        #endregion

        /// <summary>
        /// 安装应用程序
        /// </summary>
        /// <param name="installPath"></param>
        /// <param name="serverName"></param>
        #region public void installApp(String installPath, String serverName)
        public void installApp(String installPath, String serverName)
        {
            //获取安装文件的类型
            String type = serverName.Substring(serverName.LastIndexOf("."));
            //判断文件类型
            if (type.Equals(".exe"))
            {
                //exe文件安装方式
                String filePath = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), serverName);
                CommonUtils.installApp(filePath, installPath);
            }
            else
            {
                //解压缩文件
                String path = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), serverName);
                bool result = CommonUtils.unzipFile(path, installPath);
                if (!bool)
                {
                    Logger.warn(typeof(UpdateVersion), "uncompress file error, update error.");
                    Logger.info(typeof(UpdateVersion), "update will be done next time.");
                    return;
                }

                //todo tar安装方式
            }
            //todo 数据备份还原 将主程序的配置文件信息进行还原操作，确保安装程序可以正常运行

            //检查版本是否安装成功
            bool installSuccess = checkInstallState(serverName);
            if (!installSuccess)
            {
                //todo 回滚到上一个版本的软件
            }
        }
        #endregion
    }
}
