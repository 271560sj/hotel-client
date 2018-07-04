using System;
using System.IO;
using System.Net;
using System.Text;
using HotelUpdateService.update.entity;

namespace HotelUpdateService.update.utils
{
    /// <summary>
    /// 用于进行http操作，进行服务更新
    /// </summary>
    class HttpUtils
    {
        ///私有构造方法，用于单实例模式的设计
        #region HttpUtils()
        private HttpUtils() { }
        #endregion
        
        ///获取单实例
        #region public static HttpUtils getInstance()
        public static HttpUtils getInstance()
        {
            return new HttpUtils();
        }
        #endregion

        /// <summary>
        /// 用于get请求
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        #region public String get(String url)
        public String get(String url)
        {
            //记录请求结果
            String result = null;

            //判断请求的url是否可用
            if (String.IsNullOrEmpty(url))
            {
                return result;
            }

            //构造请求
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            //get请求方式
            request.Method = WebRequestMethods.Http.Get;
            //设置超时时间
            request.Timeout = 2 * 60 * 1000;
            //执行get请求，获取并处理请求结果
            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    //判断返回结果失败
                    if(response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.error(typeof(HttpUtils), "get http request error.");
                        return result;
                    }
                    Logger.info(typeof(HttpUtils), "get http request success.");
                    //返回正确的结果
                    StreamReader str = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    result = str.ReadToEnd();
                    return result;
                }
            }
            catch (WebException ex)
            {
                Logger.error(typeof(HttpUtils), ex);
            }catch(ArgumentNullException ex)
            {
                Logger.error(typeof(HttpUtils), ex);
            }catch(Exception ex)
            {
                Logger.error(typeof(HttpUtils), ex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 用于下载文件
        /// </summary>
        /// <param name="url"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        #region public Stream download(String url, PostData post)
        public bool download(String url, PostData post)
        {
            //记录返回结果
            bool result = false;
            //判断请求的url为空，则返回空结果
            if (String.IsNullOrEmpty(url))
            {
                Logger.info(typeof(HttpUtils), "request url is empty.");
                return result;
            }
            //判断请求参数为空，则返回空
            if(post == null || String.IsNullOrEmpty(post.toJson()))
            {
                Logger.info(typeof(HttpUtils), "request parameters is empty");
                return result;
            }
            //进行post请求操作
            try
            {
                //构建request
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //设置request的属性信息
                request.ContentType = "application/json";
                request.Method = WebRequestMethods.Http.Post;

                //设置编码方式，并将请求参数转换为byte[]
                byte[] parame = Encoding.GetEncoding("UTF-8").GetBytes(post.toJson());
                Logger.info(typeof(HttpUtils), String.Format("request parame is {0}", post.toJson()));
                //设置请求长度
                request.ContentLength = parame.Length;

                //将请求参数写入到byte数组中去
                using(Stream requestStream = request.GetRequestStream())
                {
                    Logger.info(typeof(HttpUtils), "get request stream success.");
                    requestStream.Write(parame, 0, parame.Length);
                    Logger.info(typeof(HttpUtils), "write request stream to byte array success.");
                }

                Logger.info(typeof(HttpUtils), "start request for server......");
                //获取返回结果
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    //返回结果错误
                    if(response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.info(typeof(HttpUtils), String.Format("request failed,response code is {0}", response.StatusCode));
                        return result;
                    }
                    //返回结果正确
                    Logger.info(typeof(HttpUtils), String.Format("request success,response code is {0}", response.StatusCode));
                    //获取返回文件流
                    Stream stream = response.GetResponseStream();
                    //保存文件流到文件
                    result = CommonUtils.saveFile(stream, post.fileName);
                    return result;
                }
            }
            catch(WebException e)
            {
                Logger.error(typeof(HttpUtils), e);
            }catch(Exception e)
            {
                Logger.error(typeof(HttpUtils), e);
            }
            return result;
        }
        #endregion
    }
}
