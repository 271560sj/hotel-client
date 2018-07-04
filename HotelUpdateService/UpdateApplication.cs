using HotelUpdateService.update.controller;
using HotelUpdateService.update.utils;
using System;

namespace HotelUpdateService
{
    static class UpdateApplication
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            UpdateController controller = UpdateController.getInstance();

            //初始化相关文件夹
            controller.initDirectory();

            //启动定时任务计划
            controller.startTask();

            //开始进行更新操作
            controller.startUpdate();
        }
    }
}