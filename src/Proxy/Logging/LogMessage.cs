using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Serilog;
using Serilog.Events;

namespace NathanAlden.Proxy.Logging
{
    public class LogMessage
    {
        private readonly LogEventLevel _level;
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private Exception _exception;

        public LogMessage(LogEventLevel level)
        {
            _level = level;
        }

        public LogMessage Exception(Exception exception)
        {
            _exception = exception;

            return this;
        }

        public LogMessage Text(string text, bool padded = false)
        {
            return Append(text, padded);
        }

        public LogMessage Space()
        {
            return Append(" ");
        }

        public LogMessage DownstreamIdentifier(int id, bool padded = false)
        {
            return Append($"[DS {id}]", padded);
        }

        public LogMessage UpstreamIdentifier(int id, bool padded = false)
        {
            return Append($"[US {id}]", padded);
        }

        public LogMessage ForwardProxyIdentifier(int id, bool padded = false)
        {
            return Append($"[FP {id}]", padded);
        }

        public LogMessage BracketedIpAddress(IPAddress ipAddress, bool padded = false)
        {
            return Append(ipAddress.ToBracketedString(), padded);
        }

        public LogMessage LeftArrow(bool padded = false)
        {
            return Append("<-", padded);
        }

        public LogMessage RightArrow(bool padded = false)
        {
            return Append("->", padded);
        }

        public void Write()
        {
            Log.Write(_level, _exception, _stringBuilder.ToString());
        }

        public static LogMessage Downstream(LogEventLevel level, int id, IPAddress ipAddress, string text = null, Exception exception = null)
        {
            LogMessage message = new LogMessage(level)
                .Exception(exception)
                .DownstreamIdentifier(id)
                .Space()
                .BracketedIpAddress(ipAddress);

            if (text != null)
            {
                message.Space().Text(text);
            }

            return message;
        }

        public static LogMessage Downstream(Exception exception, int id, IPAddress ipAddress)
        {
            if (exception is IOException ioException && exception.InnerException is SocketException)
            {
                exception = (SocketException)ioException.InnerException;
            }
            if (exception is SocketException socketException)
            {
                return Downstream(LogEventLevel.Error, id, ipAddress, GetSocketErrorMessage(socketException));
            }

            return Downstream(LogEventLevel.Error, id, ipAddress, "Unexpected error", exception);
        }

        public static LogMessage Upstream(LogEventLevel level, int id, IPAddress ipAddress, string text = null, Exception exception = null)
        {
            LogMessage message = new LogMessage(level)
                .Exception(exception)
                .UpstreamIdentifier(id)
                .Space()
                .BracketedIpAddress(ipAddress);

            if (text != null)
            {
                message.Space().Text(text);
            }

            return message;
        }

        public static LogMessage Upstream(Exception exception, int id, IPAddress ipAddress)
        {
            if (exception is IOException ioException && exception.InnerException is SocketException)
            {
                exception = (SocketException)ioException.InnerException;
            }
            if (exception is SocketException socketException)
            {
                return Upstream(LogEventLevel.Error, id, ipAddress, GetSocketErrorMessage(socketException));
            }

            return Upstream(LogEventLevel.Error, id, ipAddress, "Unexpected error", exception);
        }

        public static LogMessage ForwardProxy(LogEventLevel level, int id, IPAddress ipAddress, string text = null, Exception exception = null)
        {
            LogMessage message = new LogMessage(level)
                .Exception(exception)
                .ForwardProxyIdentifier(id)
                .Space()
                .BracketedIpAddress(ipAddress);

            if (text != null)
            {
                message.Space().Text(text);
            }

            return message;
        }

        public static LogMessage ForwardProxy(Exception exception, int id, IPAddress ipAddress)
        {
            if (exception is IOException ioException && exception.InnerException is SocketException)
            {
                exception = (SocketException)ioException.InnerException;
            }
            if (exception is SocketException socketException)
            {
                return ForwardProxy(LogEventLevel.Error, id, ipAddress, GetSocketErrorMessage(socketException));
            }

            return ForwardProxy(LogEventLevel.Error, id, ipAddress, "Unexpected error", exception);
        }

        private LogMessage Append(string text, bool padded = false)
        {
            if (padded)
            {
                _stringBuilder.Append(' ');
            }
            _stringBuilder.Append(text);
            if (padded)
            {
                _stringBuilder.Append(' ');
            }

            return this;
        }

        private static string GetSocketErrorMessage(SocketException exception)
        {
            return $"Socket error ({(int)exception.SocketErrorCode} {exception.SocketErrorCode})";
        }
    }
}