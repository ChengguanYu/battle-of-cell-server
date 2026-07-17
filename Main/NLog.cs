using System.Diagnostics;
using System.IO;
using Fantasy.Platform.Net;
using System.Runtime.CompilerServices;
using NLog;
using NLog.Targets;
using NLog.Targets.Wrappers;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Fantasy
{
    /// <summary>
    /// 使用 NLog 实现的日志记录器。
    /// </summary>
    public sealed class NLog : ILog
    {
        private const string DefaultLogSceneName = "Log";
        private readonly Logger _logger; // NLog 日志记录器实例

        /// <summary>
        /// 初始化 NLog 实例。
        /// </summary>
        /// <param name="name">日志记录器的名称。</param>
        public NLog(string name)
        {
            // 获取指定名称的 NLog 日志记录器
            _logger = LogManager.GetLogger(name);
            // 配置加载后即可注入 ANSI 变量（避免 Initialize 前 INFO 无柔和色）
            EnsureAnsiVariables();
        }

        private static void EnsureAnsiVariables()
        {
            var cfg = LogManager.Configuration;
            if (cfg == null)
            {
                return;
            }

            cfg.Variables["ansiSoftWhite"] = "\u001b[38;2;220;220;220m";
            cfg.Variables["ansiReset"] = "\u001b[0m";
        }

        /// <summary>
        /// 初始化方法
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="processMode"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(string appId, ProcessMode processMode)
        {
            LogManager.Configuration.Variables["appId"] = string.IsNullOrEmpty(appId) || appId=="0" ? "Develop" : appId;
            EnsureAnsiVariables();
            ApplyFileFlushOptions(processMode);
            
            if (processMode == ProcessMode.Release)
            {
                LogManager.Configuration.RemoveRuleByName("ConsoleTrace");
                LogManager.Configuration.RemoveRuleByName("ConsoleDebug");
                LogManager.Configuration.RemoveRuleByName("ConsoleInfo");
                LogManager.Configuration.RemoveRuleByName("ConsoleWarn");
                LogManager.Configuration.RemoveRuleByName("ConsoleError");
            }
            
            LogManager.ReconfigExistingLoggers();
        }
        
        private static void ApplyFileFlushOptions(ProcessMode processMode)
        {
            var isDevelop = processMode == ProcessMode.Develop;
            
            foreach (var target in LogManager.Configuration.AllTargets)
            {
                ApplyFileFlushOptions(target, isDevelop);
            }
        }
        
        private static void ApplyFileFlushOptions(Target target, bool isDevelop)
        {
            switch (target)
            {
                case FileTarget fileTarget:
                {
                    fileTarget.AutoFlush = isDevelop;
                    fileTarget.OpenFileFlushTimeout = isDevelop ? 1 : 60;
                    return;
                }
                case WrapperTargetBase { WrappedTarget: not null } wrapperTarget:
                {
                    ApplyFileFlushOptions(wrapperTarget.WrappedTarget, isDevelop);
                    return;
                }
            }
        }

        /// <summary>
        /// 记录跟踪级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string message)
        {
            Write(LogLevel.Trace, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录警告级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message)
        {
            Write(LogLevel.Warn, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录信息级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message)
        {
            Write(LogLevel.Info, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录调试级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message)
        {
            Write(LogLevel.Debug, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录错误级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message)
        {
            Write(LogLevel.Error, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录严重错误级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fatal(string message)
        {
            Write(LogLevel.Fatal, DefaultLogSceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string sceneName, string message)
        {
            Write(LogLevel.Trace, sceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string sceneName, string message)
        {
            Write(LogLevel.Warn, sceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string sceneName, string message)
        {
            Write(LogLevel.Info, sceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string sceneName, string message)
        {
            Write(LogLevel.Debug, sceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string sceneName, string message)
        {
            Write(LogLevel.Error, sceneName, message);
        }

        /// <summary>
        /// 记录跟踪级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string message, params object[] args)
        {
            Write(LogLevel.Trace, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录警告级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message, params object[] args)
        {
            Write(LogLevel.Warn, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录信息级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message, params object[] args)
        {
            Write(LogLevel.Info, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录调试级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message, params object[] args)
        {
            Write(LogLevel.Debug, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录错误级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message, params object[] args)
        {
            Write(LogLevel.Error, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录严重错误级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fatal(string message, params object[] args)
        {
            Write(LogLevel.Fatal, DefaultLogSceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Trace, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Warn, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Info, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Debug, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Error, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(LogLevel level, string sceneName, string message, params object[] args)
        {
            // async target 下 ${callsite} 不可靠；手动跳过 Fantasy.Log / 本封装层后写入 caller。
            var logEvent = new LogEventInfo(level, _logger.Name, null, message, args);
            logEvent.Properties["sceneName"] = string.IsNullOrWhiteSpace(sceneName) ? DefaultLogSceneName : sceneName;
            logEvent.Properties["caller"] = CaptureCaller();
            _logger.Log(typeof(NLog), logEvent);
        }

        private static string CaptureCaller()
        {
            // skipFrames=1 跳过 CaptureCaller 自身
            var stack = new StackTrace(1, true);
            for (var i = 0; i < stack.FrameCount; i++)
            {
                var frame = stack.GetFrame(i);
                var method = frame?.GetMethod();
                var type = method?.DeclaringType;
                if (type == null)
                {
                    continue;
                }

                if (type == typeof(NLog) || type.FullName == "Fantasy.Log")
                {
                    continue;
                }

                var ns = type.Namespace;
                if (ns != null && (ns == "NLog" || ns.StartsWith("NLog.", StringComparison.Ordinal)))
                {
                    continue;
                }

                var typeName = type.Name;
                if (typeName.Contains("<>", StringComparison.Ordinal) || typeName.StartsWith("<", StringComparison.Ordinal))
                {
                    var declaring = type.DeclaringType;
                    if (declaring != null)
                    {
                        typeName = declaring.Name;
                    }
                }

                var methodName = method!.Name;
                if (methodName is "MoveNext" or "Invoke" or "Start")
                {
                    methodName = methodName == "MoveNext" ? "async" : methodName;
                }

                var file = frame!.GetFileName();
                var line = frame.GetFileLineNumber();
                if (!string.IsNullOrEmpty(file) && line > 0)
                {
                    return $"{typeName}.{methodName}({Path.GetFileName(file)}:{line})";
                }

                return $"{typeName}.{methodName}";
            }

            return "?";
        }
    }
}