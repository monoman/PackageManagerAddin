﻿namespace NuPack {
    public interface ILogger {
        void Log(MessageLevel level, string message, params object[] args);
    }
}