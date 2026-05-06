
namespace ZLog
{
    /// <summary>
    /// 日志系统常量配置
    /// </summary>
    public class LoggerConst
    {
        /// <summary>
        /// unity 日志初始路径
        /// </summary>
        public const string UNITY_LOG_START_PATH = "unityLogStartPath";

        #region 日志显示项

        /// <summary>
        /// log等级<see cref="UnityLogLevel" />
        /// </summary>
        public const string LOG_LEVEL = "level";
        /// <summary>
        /// 日期格式
        /// </summary>
        public const string DATE_TIME_FORMAT = "dateTimeFormat";

        #endregion

        /// <summary>
        /// True string
        /// </summary>
        public const string TRUE = "true";
        /// <summary>
        /// False string
        /// </summary>
        public const string FALSE = "false";

        #region TIME FORMAT

        /// <summary>
        /// 年-月-日
        /// </summary>
        public const string DATE = "yyyy-MM-dd";
        /// <summary>
        /// 年-月-日 时:分:秒.毫秒
        /// </summary>
        public const string DATE_NOW_TIME_OF_DAY = "[yyyy-MM-dd] HH:mm:ss.fffffff";
        /// <summary>
        /// 年-月-日 时:分:秒
        /// </summary>
        public const string DATE_NOW_TIME = "[yyyy-MM-dd] HH:mm:ss";
        /// <summary>
        /// 时:分:秒
        /// </summary>
        public const string NOW_TIME = "HH:mm:ss";
        /// <summary>
        /// 时:分:秒.毫秒
        /// </summary>
        public const string NOW_TIME_OF_DAY = "HH:mm:ss.fffffff";

        #endregion

        #region COLOR
        /// <summary>
        /// 红色
        /// </summary>
        public const string COLOR_RED = "#ff0000";
        /// <summary>
        /// 绿色
        /// </summary>
        public const string COLOR_GREEN = "#00ff00";
        /// <summary>
        /// 蓝色
        /// </summary>
        public const string COLOR_BLUE = "#0000ff";
        /// <summary>
        /// 黄色
        /// </summary>
        public const string COLOR_YELLOW = "#FDF500";
        /// <summary>
        /// 橘色
        /// </summary>
        public const string COLOR_ORINGE = "#FF8700";
        /// <summary>
        /// 黑色
        /// </summary>
        public const string COLOR_BLACK = "#000000";
        /// <summary>
        /// 白色
        /// </summary>
        public const string COLOR_WHITE = "FFFFFF";

        #endregion
    }
}
