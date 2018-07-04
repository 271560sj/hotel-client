using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace HotelUpdateService.update.utils
{
    /// <summary>
    /// 通用工具类
    /// </summary>
    class CommonUtils
    {
        /// <summary>
        /// 用于获取服务的安装路径
        /// </summary>
        /// <returns></returns>
        #region public static String getServiceRunningPath()
        public static String getServiceRunningPath()
        {
            //返回升级程序安装路径
            return AppDomain.CurrentDomain.BaseDirectory;
        }
        #endregion

        /// <summary>
        /// 获取当前版本的version
        /// </summary>
        /// <param name="path"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        #region public static bool getVersionFromConfigFile(String path, out String version, out String name)
        public static bool getVersionFromConfigFile(String path, out String version, out String name)
        {
            //标识成功
            bool isSuccess = false;
            //判断传入的参数是否合法
            if (String.IsNullOrEmpty(path))
            {
                Logger.info(typeof(CommonUtils), "version config xml file path is empty.");
                version = String.Empty;
                name = String.Empty;
                return isSuccess;
            }
            //读取配置文件
            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(path);
                XmlElement xml = document.DocumentElement;
                version = xml.SelectSingleNode("version").InnerText.ToString();
                name = xml.SelectSingleNode("name").InnerText.ToString();
                isSuccess = true;
                return isSuccess;
            }
            catch (Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            version = String.Empty;
            name = String.Empty;
            return isSuccess;
        }
        #endregion

        /// <summary>
        /// 修改xml文档的数据
        /// </summary>
        /// <param name="path"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        #region public static bool changeLocalVersionAfterUpdate(String path, String key, String value)
        public static bool changeLocalVersionAfterUpdate(String path, String key, String value)
        {
            //记录操作是否成功
            bool result = false;
            if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(key))
            {
                Logger.warn(typeof(CommonUtils), "xml path and key is empty.");
                return result;
            }
            try
            {
                //判断文件是否存在
                if (!File.Exists(path))
                {
                    Logger.warn(typeof(CommonUtils), "xml file not exist");
                    return result;
                }
                //获取实例
                XmlDocument document = new XmlDocument();
                document.Load(path);//加载文件
                XmlElement xml = document.DocumentElement;//获取根目录
                xml.SelectSingleNode(key).InnerText = value;//修改节点值
                document.Save(path);//保存xml文件
                result = true;
            }catch(Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 检查文件是否存在，并返回本地文件的长度
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        #region public static long checkExistFileInLocal(String name)
        public static long checkExistFileInLocal(String name)
        {
            //标识本地文件大小
            long size = 0L;
            //构建文件全路径
            String filePath = String.Format(@"{0}{1}\{2}", CommonUtils.getServiceRunningPath(), "update", name);
            if (File.Exists(filePath))//判断文件是否存在
            {
                FileStream stream = new FileStream(filePath, FileMode.Open);
                size = stream.Length;
            }
            //返回文件大小
            return size;
        }
        #endregion

        /// <summary>
        /// 将获取的文件流保存到本地文件中
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        #region public bool saveFile(Stream stream, String name)
        public static bool saveFile(Stream stream, String name)
        {
            //标记返回结果
            bool isSuccess = false;
            //构建文件保存全路径
            String fullName = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), name);
            FileStream fs = null;

            //创建文件，并保存文件
            try
            {
                //获取文件流
                if (File.Exists(fullName))//文件存在
                {
                    fs = new FileStream(fullName, FileMode.Append, FileAccess.Write, FileShare.Write);
                }
                else//文件不存在
                {
                    fs = new FileStream(fullName, FileMode.Create, FileAccess.Write);
                }
                //字节数组，用于读取文件流
                byte[] buffer = new byte[5 * 1024 * 1024];
                int len = -1;
                //开始写文件
                while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, len);
                }
                isSuccess = true;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            finally
            {
                //最后关闭相关流
                stream.Flush();
                stream.Close();
                fs.Close();
            }
            //返回结果
            return isSuccess;
        }
        #endregion

        /// <summary>
        /// 查询指定名称的进程，获取进程实例并返回进程的安装目录
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        #region public static Process getProcessInstalled(String name, out String path)
        public static Process getProcessInstalled(String name, out String path)
        {
            Process process = null;
            //判断传入参数是否合法
            if (String.IsNullOrEmpty(name))
            {
                Logger.info(typeof(CommonUtils), "process name is empty.");
                path = null;
                return process;
            }
            //获取指定名称的进程的信息
            try
            {
                //获取进程列表
                Process[] processes = Process.GetProcesses();
                foreach (Process ps in processes)//遍历进程列表
                {
                    //获取指定进程信息
                    if (ps.ProcessName.ToLower().Contains(name.ToLower()))
                    {
                        path = Path.GetDirectoryName(ps.MainModule.FileName);
                        process = ps;
                        return process;
                    }
                }

            }
            catch (InvalidOperationException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            path = String.Empty;
            return process;

        }
        #endregion

        /// <summary>
        /// 备份需要重新安装的文件的数据
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        #region public static bool backAppInfo(String path, String version, String name)
        public static bool backAppInfo(String path, String version, String name)
        {
            //标识是否备份成功
            bool result = false;
            //判断传入参数是否合法
            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(version) || String.IsNullOrEmpty(name))
            {
                Logger.info(typeof(CommonUtils), "app install path or name or version is empty.");
                return result;
            }
            //获取文件备份目标地址全路径
            String directory = String.Format(@"{0}back\{1}", CommonUtils.getServiceRunningPath(), String.Format(@"{0}-{1}.zip", name, version));
            try
            {
                if (File.Exists(directory))//判断文件是否存在
                {
                    File.Delete(directory);//文件存在，删除文件
                }
                //开始文件备份
                using (ZipOutputStream stream = new ZipOutputStream(File.Create(directory)))
                {
                    stream.SetLevel(6);//设置文件压缩级别
                    //开始压缩
                    result = ZipHelper.zipDirectory(path, stream, path);
                    return result;
                }
            }catch(IOException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 解压缩文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        #region public static bool unzipFile(String filePath, String directory)
        public static bool unzipFile(String filePath, String directory)
        {
            //判断传入参数是否合法
            if(String.IsNullOrEmpty(filePath) || String.IsNullOrEmpty(directory))
            {
                Logger.info(typeof(CommonUtils), "file path is empty.");
                return false;
            }
            //获取文件类型
            String type = filePath.Substring(filePath.LastIndexOf("."));
            if (type.Equals(".tar"))
            {
                //解压缩tar包
                return ZipHelper.unTarFile(filePath, directory);
            }
            else
            {
                //解压缩zip包
                return ZipHelper.unZipFile(filePath);
            }
        }
        #endregion

        /// <summary>
        /// 安装软件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="directory"></param>
        #region public static void installApp(String path, String directory)
        public static void installApp(String path, String directory)
        {
            //安装软件的参数信息
            String cmd = String.Format(@"/s /S /silent /D={0} /dir={0}", directory);
            try
            {
                //配置进行信息
                ProcessStartInfo info = new ProcessStartInfo(path, cmd)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,//不显示窗口
                    CreateNoWindow = false,//不创建窗口
                };

                //开始安装
                Process process = Process.Start(info);
                process.WaitForExit();
                process.Close();
            }catch(Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
        }
        #endregion

        /// <summary>
        /// 读取跟新程序的配置文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        #region public static String readConfig(String path, String key)
        public static String readConfig(String path, String key)
        {
            //记录读取的值
            String value = String.Empty;
            //判断传入参数是否合法
            if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(key))
            {
                Logger.info(typeof(CommonUtils), "parameters is not vaildated");
                return value;
            }
            //读取配置文件
            try
            {
                //加载配置文件
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = path;
                Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                //读取配置文件中指定的值
                value = configuration.AppSettings.Settings[key].Value.ToString();
            }catch(ConfigurationErrorsException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            return value;
        }
        #endregion

        /// <summary>
        /// 获取定时任务的名称
        /// </summary>
        /// <returns></returns>
        #region public static String getUpdateName()
        public static String getUpdateName()
        {
            //记录定时任务的名称，设置默认值
            String name = "HotelUpdateTask";
            //读取定时任务名称
            name = CommonUtils.readConfig(String.Format(@"{0}config\update.info.config", CommonUtils.getServiceRunningPath()), "taskName");
            if (String.IsNullOrEmpty(name))
            {
                name = "HotelUpdateTask";
            }
            return name;
        }
        #endregion

        /// <summary>
        /// 描述信息
        /// </summary>
        /// <returns></returns>
        #region public static String getUpdateDescribe()
        public static String getUpdateDescribe()
        {
            //读取定时任务的描述信息
            String describe = CommonUtils.readConfig(String.Format(@"{0}config\update.info.config", CommonUtils.getServiceRunningPath()), "describe");
            if (String.IsNullOrEmpty(describe))
            {
                describe = "update hotel help client";
            }
            return describe;
        }
        #endregion

        /// <summary>
        /// 获取更新频率
        /// </summary>
        /// <returns></returns>
        #region public static String getFrequency()
        public static String getFrequency()
        {
            //读取定时任务的更新频率
            String frequency = CommonUtils.readConfig(String.Format(@"{0}config\timer.config", CommonUtils.getServiceRunningPath()), "frequency");
            if (String.IsNullOrEmpty(frequency))
            {
                frequency = "daily";
            }
            return frequency;
        }
        #endregion

        /// <summary>
        /// 获取任务开始时间
        /// </summary>
        /// <returns></returns>
        #region public static String getDate()
        public static String getDate()
        {
            //设置定时任务的开始时间
            String date = CommonUtils.readConfig(String.Format(@"{0}config\timer.config", CommonUtils.getServiceRunningPath()), "start");
            if (String.IsNullOrEmpty(date))
            {
                date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            return date;
        }
        #endregion

        /// <summary>
        /// 获取天数
        /// </summary>
        /// <returns></returns>
        #region public static int getDay()
        public static int getDay()
        {
            //获取定时任务的天数
            String day = CommonUtils.readConfig(String.Format(@"{0}config\timer.config", CommonUtils.getServiceRunningPath()), "day");
            if (String.IsNullOrEmpty(day))
            {
                day = "1";
            }
            int days = 1;
            int.TryParse(day, out days);
            return days;
        }
        #endregion

        /// <summary>
        /// 获取星期
        /// </summary>
        /// <returns></returns>
        #region public static String getWeek()
        public static String getWeek()
        {
            //获取定时任务的星期书
            String week = CommonUtils.readConfig(String.Format(@"{0}config\timer.config", CommonUtils.getServiceRunningPath()), "week");
            if (String.IsNullOrEmpty(week))
            {
                week = "mon";
            }
            return week;
        }
        #endregion

        /// <summary>
        /// 计算文件的sha256的hash值
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        #region public static String getFileSHA256(String fileName)
        public static String getFileSHA256(String fileName)
        {
            //记录hash值
            String hash = String.Empty;
            try
            {
                //获取本地文件的全路径
                String fullName = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), fileName);
                //开始计算hash值
                using (FileStream fs = new FileStream(fullName, FileMode.Open, FileAccess.Read))
                {
                    //获取计算hash值算法
                    HashAlgorithm algorithm = SHA256.Create();
                    //计算hash值
                    byte[] values = algorithm.ComputeHash(fs);
                    //将字节数组的hash值转换为16进制字符串
                    hash = byteToHeyString(values);
                    return hash;
                }
            }catch(IOException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }catch(Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            return hash;
        }
        #endregion

        /// <summary>
        /// 字节数组转换为16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        #region public static String byteToHeyString(byte[] bytes)
        public static String byteToHeyString(byte[] bytes)
        {
            //记录hash值
            String value = String.Empty;
            if(bytes == null)//判断传入参数是否合法
            {
                return value;
            }
            foreach(byte bs in bytes)//开始转换字节数组
            {
                value += bs.ToString("x2");
            }
            return value;
        }
        #endregion

        /// <summary>
        /// 删除本地文件
        /// </summary>
        /// <param name="fileName"></param>
        #region public static void deleteFile(String fileName)
        public static void deleteFile(String fileName)
        {
            //获取文件的全路径
            String full = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), fileName);
            try//开始删除文件
            {
                if (File.Exists(full))//判断文件存在，删除
                {
                    File.Delete(full);
                }
            }catch(IOException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }catch(Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
        }
        #endregion

        /// <summary>
        /// 检查网络是否可以联通
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        #region public static bool checkNetwork(String ip)
        public static bool checkNetwork(String ip)
        {
            bool result = false;
            try
            {
                Ping ping = new Ping();//实例化ping
                PingOptions options = new PingOptions();//实例化PingOptions
                options.DontFragment = true;
                String data = "";
                byte[] bytes = Encoding.UTF8.GetBytes(data);//设置编码格式
                int timeout = 120;//设置超时时间
                PingReply reply = ping.Send(ip, timeout, bytes, options);//测试是否联通
                String info = reply.Status.ToString();//获取返回的字符串
                result = "success".Equals(info.ToLower()) ? true : false;//解析结果
            }catch(Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            return result;
        }
        #endregion
    }
}
