using HotelUpdateService.update.service;
using HotelUpdateService.update.utils;
using System;
using System.Diagnostics;
using System.IO;
using TaskScheduler;

namespace HotelUpdateService.update.controller
{
    /// <summary>
    /// 用于更新服务
    /// </summary>
    class UpdateController
    {
        /// <summary>
        /// 记录查询到的文件在服务器存储路径
        /// </summary>
        private String serverPath { get; set; }

        /// <summary>
        /// 记录查询到的文件的名称
        /// </summary>
        private String serverName { get; set; }

        /// <summary>
        /// 记录需要更新的文件的安装路径
        /// </summary>
        private String installPath { get; set; }

        /// <summary>
        /// 获取更新服务类
        /// </summary>
        private static UpdateVersion update = UpdateVersion.getInstance();
        
        /// <summary>
        /// 私有构造方法，用于单实例模式的设计
        /// </summary>
        #region private UpdateController() 
        private UpdateController() { }
        #endregion

        /// <summary>
        /// 单实例模式，用于初始化实例
        /// </summary>
        /// <returns></returns>
        #region public static UpdateController getInstance
        public static UpdateController getInstance()
        {
            return new UpdateController();
        }
        #endregion

        /// <summary>
        /// 启动定时任务计划
        /// </summary>
        #region public void startTask()
        public void startTask()
        {
            //获取定时任务的配置参数

            //任务名称
            String name = CommonUtils.getUpdateName();
            //任务描述信息
            String describe = CommonUtils.getUpdateDescribe();
            //定时任务更新频率
            String frequency = CommonUtils.getFrequency();
            //定时任务开始执行的日期
            String date = CommonUtils.getDate();
            //定时任务执行的天数，在frequency设置为monthly有效
            int day = CommonUtils.getDay();
            //设置定时任务在星期几执行，在frequency设置为weekly有效
            String week = CommonUtils.getWeek();
            //定时任务执行的命令的文件所在路径
            String path = String.Format(@"{0}HotelUpdateService.exe", CommonUtils.getServiceRunningPath());
            //记录定时任务的状态
            _TASK_STATE state;
            //判断定时任务是否存在不存在则创建定时任务
            if (!TaskSchedulerUtils.checkTask(name, out state))
            {
                //无限循环，直到定时任务创建成功
                for (; ; )
                {
                    //创建定时任务
                    bool flag = TaskSchedulerUtils.createTask(Environment.UserName, describe, name, path, frequency, date, day, week);

                    if (flag)
                    {
                        Logger.info(typeof(UpdateController), String.Format("create task {0} success.", name));
                        //定时任务创建成功以后，杀死当前进程
                        Process.GetCurrentProcess().Kill();
                        return;
                    }
                    Logger.warn(typeof(UpdateController), String.Format("create task {0} failed, try again.", name));
                }
            }

            //根据定时任务状态，判断是否需要启动定时任务
            if (state != _TASK_STATE.TASK_STATE_RUNNING && state != _TASK_STATE.TASK_STATE_READY)
            {
                Logger.warn(typeof(UpdateController), String.Format("task {0} 's state is {1}, waiting for start.", name, state.ToString()));
                TaskSchedulerUtils.startTask(name);
                Process.GetCurrentProcess().Kill();
            }else if(state == _TASK_STATE.TASK_STATE_READY)
            {
                Logger.info(typeof(UpdateController), String.Format("task {0} 's state is {1}", name, state));
                Process.GetCurrentProcess().Kill();
            }
        }
        #endregion

        /// <summary>
        /// 开始更新任务
        /// </summary>
        #region public void startUpdate()
        public void startUpdate()
        {
            //检查版本是否更新
            for(int i = 0; i < 10; i++)//如果不成功，重复查询十次
            {
                //重复十次查询，没有结果，结束本次更新
                if (i >= 10)
                {
                    Logger.warn(typeof(UpdateController), "check app version for 10 times, can not get anything.");
                    return;
                }
                //记录返回的查询结果
                String path, name;
                bool isUpdate = update.checkVersion(out path, out name);
                //查询结果判断
                if (!isUpdate)
                {
                    Logger.warn(typeof(UpdateController), "the app has not been update in server.");
                    return;
                }
                if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(name))
                {
                    Logger.warn(typeof(UpdateController), "update client has find updated version in server, but some error occured unknown.");
                    continue;
                }
                serverName = name;
                serverPath = path;
                break;
            }

             //进行十次下载文件操作，十次以后文件下载如果不成功，则结束本次更新
            for(var i =0; i < 10; i ++)
            {
                //查询次数大于十次，结束更新
                if (i > 9) { Logger.warn(typeof(UpdateController),"download file over 10 times, but still failed,please check if network is avaliable."); return; }
                //获取本地文件大小
                long size = update.checkFileExist(serverName);
                //开始下载
                bool isDownload = update.downloadFile(serverPath, serverName, size);
                //判断是否下载成功
                if (!isDownload)
                {
                    Logger.warn(typeof(UpdateController), "down load file error. try again.");
                    continue;//下载不成功继续下载
                }
                //下载成功校验sha256值是否正确
                String localHash = CommonUtils.getFileSHA256(serverName);
                String serverHash = update.getHashFromServer(serverName);
                if(String.IsNullOrEmpty(localHash) || String.IsNullOrEmpty(serverHash) || !localHash.Equals(serverHash))
                {
                    Logger.warn(typeof(UpdateController), "download file has been destroyed.delete it and try download again");
                    CommonUtils.deleteFile(serverName);
                    continue;
                }
                Logger.info(typeof(UpdateController), "download file success.");
                break;
            }

            //备份本地数据，十次备份不成功，退出更新
             for(var i = 0; i < 10;  i++)
            {
                if(i > 9)//备份次数大于10,结束备份
                {
                    Logger.warn(typeof(UpdateController), "back up local app info error.");
                    return;
                }
                String appPath;//记录主程序的运行路径
                //开始备份数据
                bool isBack = update.backLocalAppInfo(out appPath);
                if (!isBack)//备份失败
                {
                    Logger.warn(typeof(UpdateController), "back up local app info error.try again");
                    continue;
                }
                //备份成功
                Logger.info(typeof(UpdateController), "back up local app info success.");
                installPath = appPath;
                break;
            }

            //安装更新包
            update.installApp(installPath, serverName);
        }
        #endregion

        /// <summary>
        /// 初始化文件夹
        /// </summary>
        #region public void initDirectory()
        public void initDirectory()
        {
            //获取程序运行路径
            String installPath = CommonUtils.getServiceRunningPath();
            //需要初始化的文件夹列表
            String[] dir = new string[] { "update", "log", "back" };
            try
            {
                //初始化文件夹
                foreach (String path in dir)
                {
                    //获取文件夹的全路径
                    String full = String.Format(@"{0}{1}", installPath, path);
                    //创建文件夹
                    if (!Directory.Exists(full))
                    {
                        Directory.CreateDirectory(full);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.error(typeof(UpdateController), ex);
            }
        }
        #endregion
    }
}
