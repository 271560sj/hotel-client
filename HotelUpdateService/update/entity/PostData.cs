using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotelUpdateService.update.utils;

namespace HotelUpdateService.update.entity
{
    /// <summary>
    /// post请求传递的消息体
    /// </summary>
    class PostData
    {
        ///远程下载文件的路径
        public String remote { get; set; }

        /// <summary>
        /// 远程下载文件的名称
        /// </summary>
        public String fileName { get; set; }

        /// <summary>
        /// 本地文件的大小
        /// </summary>
        public long localSize { get; set; }

        /// <summary>
        /// 将请求消息转换为json字符串
        /// </summary>
        /// <returns></returns>
        public String toJson()
        {
            if (String.IsNullOrEmpty(remote) || String.IsNullOrEmpty(fileName))
            {
                Logger.info(typeof(PostData), "remote path or file name is empty.");
                return null;
            }
            Logger.info(typeof(PostData), String.Format("remote path is {0}, file name is {1}, local file size is {2}", remote, fileName, localSize));
            return "{ \"remote\": \"" + remote + "\", \"fileName\": \"" + fileName + "\",\"localSize\": \"" + localSize + "\"}";
        }
    }
}
