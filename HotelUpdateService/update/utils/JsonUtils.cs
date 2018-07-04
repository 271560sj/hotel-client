using HotelUpdateService.update.entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace HotelUpdateService.update.utils
{
    /// <summary>
    /// 操作json的工具类
    /// </summary>
    class JsonUtils
    {
        /// <summary>
        /// 从json配置文件中读取信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        #region public static String readJson(String path)
        public static String readJson(String path)
        {
            String json = null;

            //文件路径为空，返回空值
            if (String.IsNullOrEmpty(path)){
                Logger.info(typeof(JsonUtils), "json file path is empty.");
                return json;
            }
            //读取文件
            try
            {
                using (StreamReader reader = File.OpenText(path))//获取文件流
                {
                    //解析文件流成json字符串
                    JsonTextReader text = new JsonTextReader(reader);
                    JObject jObject = JToken.ReadFrom(text) as JObject;
                    json = jObject.ToString();
                }
            }
            catch(NotSupportedException ex)
            {
                Logger.error(typeof(JsonUtils), ex);
            }
            catch (Exception e)
            {
                Logger.error(typeof(JsonUtils), e);
            }
            return json;
        }
        #endregion

        /// <summary>
        /// 解析string字符串到ResultEntity实体类
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        #region public static ResultEntity getResultEntity(String str)
        public static ResultEntity getResultEntity(String str)
        {
            //记录结果
            ResultEntity entity = null;
            if (String.IsNullOrEmpty(str))//判断传入参数是否合法
            {
                Logger.info(typeof(JsonUtils), "content of string is empty.");
                return entity;
            }
            try
            {
                JObject jo = JObject.Parse(str);//将json字符串转换为json串
                entity = new ResultEntity();
                if (jo.SelectToken("message") != null)//获取message
                {
                    entity.message = jo.SelectToken("message").ToString();
                }
                
                int result = 0;
                //获取code
                int.TryParse(jo.SelectToken("code").ToString(), out result);
                entity.code = result;

                if (jo.SelectToken("object") != null)//获取object
                {
                    JObject path = JObject.Parse(jo.SelectToken("object").ToString());
                    if (path.SelectToken("path") != null)//获取path
                    {
                        entity.path = path.SelectToken("path").ToString();
                    }
                    if(path.SelectToken("hash") != null)//获取hash值
                    {
                        entity.hash = path.SelectToken("hash").ToString();
                    }
                }
                return entity;
            }
            catch (Exception e)
            {
                Logger.error(typeof(JsonUtils), e);
            }

            return entity;
        } 
        #endregion
    }
}
