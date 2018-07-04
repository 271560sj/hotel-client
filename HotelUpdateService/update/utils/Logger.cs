using System;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace HotelUpdateService.update.utils
{
    /// <summary>
    /// 用于记录操作日志
    /// </summary>
    class Logger
    {
        /// <summary>
        /// 记录info
        /// </summary>
        /// <param name="t"></param>
        /// <param name="message"></param>
        #region public static void info(Type t, String message)
        public static void info(Type t, String message)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.InfoFormat("Info: {0}", message);
        }
        #endregion

        /// <summary>
        /// 记录info
        /// </summary>
        /// <param name="t"></param>
        /// <param name="ex"></param>
        #region public static void info(Type t, Exception ex)
        public static void info(Type t, Exception ex)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.InfoFormat("Info: {0}", ex);
        }
        #endregion

        /// <summary>
        /// 记录error
        /// </summary>
        /// <param name="t"></param>
        /// <param name="message"></param>
        #region public static void error(Type t, String message)
        public static void error(Type t, String message)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.ErrorFormat("Error: {0}", message);
        }
        #endregion

        /// <summary>
        /// 记录error
        /// </summary>
        /// <param name="t"></param>
        /// <param name="e"></param>
        #region public static void error(Type t, Exception e)
        public static void error(Type t, Exception e)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.ErrorFormat("Error: {0}", e);
        }
        #endregion

        /// <summary>
        /// 记录warn
        /// </summary>
        /// <param name="t"></param>
        /// <param name="message"></param>
        #region public static void warn(Type t, String message)
        public static void warn(Type t, String message)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.WarnFormat("Warn: {0}", message);
        }
        #endregion

        /// <summary>
        /// 记录warn
        /// </summary>
        /// <param name="t"></param>
        /// <param name="e"></param>
        #region public static void warn(Type t, Exception e)
        public static void warn(Type t, Exception e)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.WarnFormat("Warn: {0}", e);
        }
        #endregion
    }
}
