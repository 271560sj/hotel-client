using System;
using TaskScheduler;

namespace HotelUpdateService.update.utils
{
    /// <summary>
    /// 定时任务
    /// </summary>
    class TaskSchedulerUtils
    {
        /// <summary>
        /// 删除任务计划
        /// </summary>
        /// <param name="taskName">任务计划的名称</param>
        #region public static void deleteTask(String taskName)
        public static void deleteTask(String taskName)
        {
            //获取任务计划实例
            TaskSchedulerClass task = new TaskSchedulerClass();
            //连接到对应的主机，本地主机，参数可以不填写
            task.Connect(null, null, null, null);
            //获取任务计划根目录
            ITaskFolder folder = task.GetFolder("\\");
            //删除任务计划
            folder.DeleteTask(taskName, 0);
        }
        #endregion

        /// <summary>
        /// 获取所有的定时任务
        /// </summary>
        /// <returns></returns>
        #region public static IRegisteredTaskCollection GetAllTasks()
        public static IRegisteredTaskCollection GetAllTasks()
        {
            //实例化任务计划
            TaskSchedulerClass task = new TaskSchedulerClass();
            //连接
            task.Connect(null, null, null, null);
            //获取根目录
            ITaskFolder folder = task.GetFolder("\\");
            //获取计划集合
            IRegisteredTaskCollection taskList = folder.GetTasks(1);
            return taskList;
        }
        #endregion

        /// <summary>
        /// 检查定时任务是否存在
        /// </summary>
        /// <param name="taskName"></param>
        /// <returns></returns>
        #region public static bool checkTask(String taskName)
        public static bool checkTask(String taskName, out _TASK_STATE state)
        {
            //标识任务计划是否存在
            var isExists = false;
            //获取计划列表
            IRegisteredTaskCollection taskList = GetAllTasks();
            foreach(IRegisteredTask task in taskList)//循环遍历列表
            {
                if (task.Name.Equals(taskName))//计划名称相等，计划存在
                {
                    isExists = true;//标识为true
                    state = task.State;//返回计划任务的状态
                    
                    return isExists;
                }
            }
            //不存在，返回位置状态
            state = _TASK_STATE.TASK_STATE_UNKNOWN;
            return isExists;

        }
        #endregion

        /// <summary>
        /// 创建任务计划
        /// </summary>
        /// <param name="creator">标识创建任务的用户</param>
        /// <param name="description">任务的描述信息</param>
        /// <param name="name">任务的名称</param>
        /// <param name="path">任务需要执行的exe文件</param>
        /// <param name="frequency">任务启动频率</param>
        /// <param name="date">任务开始时间</param>
        /// <param name="day">任务在那一天执行</param>
        /// <param name="week">任务在星期几执行</param>
        /// <returns></returns>
        #region public static bool createTask(String creator, String description, String name, String path, String frequency, String date, int day, String week)
        public static bool createTask(String creator, String description, String name, String path, String frequency, String date, int day, String week)
        {
            try
            {
                //检查任务计划存在，则删除任务计划
                if (checkTask(name, out _TASK_STATE state))
                {
                    deleteTask(name);
                }
                //创建实例
                TaskSchedulerClass task = new TaskSchedulerClass();
                task.Connect(null, null, null, null);//连接到主机
                ITaskFolder folder = task.GetFolder("\\");//获取任务计划目录

                //设置任务定义信息
                ITaskDefinition definition = task.NewTask(0);
                definition.RegistrationInfo.Author = creator;//任务创建者
                definition.RegistrationInfo.Description = description;//任务描述信息
                definition.RegistrationInfo.Date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");//任务创建时间
                ITrigger trigger = getTriigger(frequency, definition, date, day, week);//创建定时器
                if(trigger == null)//判断定时器是否创建成功
                {
                    Logger.info(typeof(TaskSchedulerUtils), "create trigger type error.");
                    return false;
                }
                //设置任务的动作
                IExecAction action = definition.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC) as IExecAction;
                action.Path = path;//设置任务执行相应动作的目录
                //definition.Settings.ExecutionTimeLimit = "RestartOnFailure";
                definition.Settings.DisallowStartIfOnBatteries = false;//交流电源不允许启动任务
                definition.Settings.RunOnlyIfIdle = false;//空闲状态下运行任务
                //注册任务
                IRegisteredTask iregTask = folder.RegisterTaskDefinition(name, definition, (int)_TASK_CREATION.TASK_CREATE, "", "", _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN, "");
                //IRunningTask running = iregTask.Run(null);
                return true;
            }
            catch(Exception ex)
            {
                Logger.error(typeof(TaskSchedulerUtils), ex);
            }
            return false;
        }
        #endregion

        /// <summary>
        /// 启动任务
        /// </summary>
        /// <param name="name"></param>
        #region public static void startTask(String name)
        public static void startTask(String name)
        {
            //获取任务列表
            IRegisteredTaskCollection tasks = GetAllTasks();
            foreach(IRegisteredTask task in tasks)//循环遍历列表
            {
                if (task.Name.Equals(name))
                {
                    task.Run(null);//开始运行任务
                }
            }
        }
        #endregion

        /// <summary>
        /// 获取定时器
        /// </summary>
        /// <param name="frequency">任务执行频率</param>
        /// <param name="task">任务实例</param>
        /// <param name="date">任务开始时间</param>
        /// <param name="day">任务在那一天执行</param>
        /// <param name="week">任务在星期几执行</param>
        /// <returns></returns>
        #region private static ITrigger getTriigger(String frequency,ITaskDefinition task, String date, String date, int day, String week)
        private static ITrigger getTriigger(String frequency,ITaskDefinition task, String date, int day, String week)
        {
            ITrigger trigger = null;//记录定时器
           
            if(String.IsNullOrEmpty(frequency))//任务启动频率为空值
            {
                Logger.info(typeof(TaskSchedulerUtils), "task scheduler is empty");
                return null;
            }

            try
            {
                if (frequency.Equals("weekly"))//每周启动一次
                {
                    //获取周定时器
                    IWeeklyTrigger weekly = task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_WEEKLY) as IWeeklyTrigger;
                    weekly.StartBoundary = date;//设置任务开始时间
                    //week 取值为mon ,tues,wed,thur,fru,sat,sun
                    if (String.IsNullOrEmpty(week) || week.ToLower().Equals("mon"))
                    {
                        //每周一执行
                        weekly.DaysOfWeek = 2;
                    }else if (week.ToLower().Equals("tues"))
                    {
                        //每周二执行
                        weekly.DaysOfWeek = 4;
                    }else if (week.ToLower().Equals("wed"))
                    {
                        //每周三执行
                        weekly.DaysOfWeek = 8;
                    }else if (week.ToLower().Equals("thur"))
                    {
                        //每周四执行
                        weekly.DaysOfWeek = 16;
                    }else if (week.ToLower().Equals("fri"))
                    {
                        //每周五执行
                        weekly.DaysOfWeek = 32;
                    }else if (week.ToLower().Equals("sat"))
                    {
                        //每周六执行
                        weekly.DaysOfWeek = 64;
                    }else
                    {
                        //其他时间，设置为每周日执行
                        weekly.DaysOfWeek = 1;
                    }
                    trigger = weekly;
                }
                else if (frequency.Equals("monthly"))//任务每月执行一次
                {
                    //实例化月定时器
                    IMonthlyTrigger monthly = task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_MONTHLY) as IMonthlyTrigger;
                    //设置任务开始时间
                    monthly.StartBoundary = date;
                    if (day <= 0 || day > 31)//设置的天数超出月份的正常天数范围，则默认每月1号执行任务
                    {
                        monthly.DaysOfMonth = 1;
                    }else
                    {  
                        //设置执行任务的日期
                        monthly.DaysOfMonth = (int)Math.Pow(2.0, (day - 1) * 1.0);
                    }
                    trigger = monthly;
                }
                else
                {
                    //获取天定时器
                    IDailyTrigger daily = task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY) as IDailyTrigger;
                    //设置开始时间
                    daily.StartBoundary = date;
                    trigger = daily;
                }
            }
            catch(Exception ex)
            {
                Logger.error(typeof(TaskSchedulerUtils), ex);
            }
            return trigger;
        }
        #endregion
    }
}
