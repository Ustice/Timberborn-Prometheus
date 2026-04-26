using System;
using System.Collections.Generic;
using System.IO;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireInGameLogEntry {

    public string Timestamp { get; }
    public LogType LogType { get; }
    public string Message { get; }

    public FireInGameLogEntry(string timestamp, LogType logType, string message) {
      Timestamp = timestamp;
      LogType = logType;
      Message = message;
    }

  }

  internal static class FireTelemetry {

    private const string FireLogPrefix = "[Prometheus/Fire] ";
    private const int MaxInGameBufferedLines = 500;
    private static readonly object InGameBufferSync = new();
    private static readonly Queue<FireInGameLogEntry> InGameLogEntries = new();

    public static void Log(string message) {
      var formatted = FormatMessage(message);
      BufferInGameLine(formatted, LogType.Log);
      Debug.Log(formatted);
    }

    public static void LogWarning(string message) {
      var formatted = FormatMessage(message);
      BufferInGameLine(formatted, LogType.Warning);
      Debug.LogWarning(formatted);
    }

    public static string[] GetRecentInGameLogLines(int maxLines = 250) {
      var entries = GetRecentInGameLogEntries(maxLines);
      if (entries.Length == 0) {
        return Array.Empty<string>();
      }

      var lines = new string[entries.Length];
      for (var i = 0; i < entries.Length; i++) {
        lines[i] = FormatInGameEntry(entries[i]);
      }

      return lines;
    }

    public static FireInGameLogEntry[] GetRecentInGameLogEntries(int maxLines = 250) {
      lock (InGameBufferSync) {
        if (InGameLogEntries.Count == 0 || maxLines <= 0) {
          return Array.Empty<FireInGameLogEntry>();
        }

        var skip = Math.Max(0, InGameLogEntries.Count - maxLines);
        var result = new List<FireInGameLogEntry>(Math.Min(maxLines, InGameLogEntries.Count));
        var index = 0;
        foreach (var entry in InGameLogEntries) {
          if (index++ < skip) {
            continue;
          }

          result.Add(entry);
        }

        return result.ToArray();
      }
    }

    public static void ClearInGameLog() {
      lock (InGameBufferSync) {
        InGameLogEntries.Clear();
      }
    }

    private static string FormatMessage(string message) {
      return string.IsNullOrEmpty(message)
        ? FireLogPrefix.TrimEnd()
        : message.StartsWith(FireLogPrefix, StringComparison.Ordinal)
          ? message
          : $"{FireLogPrefix}{message}";
    }

    private static void BufferInGameLine(string formattedMessage, LogType logType) {
      var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
      var entry = new FireInGameLogEntry(timestamp, logType, formattedMessage);

      lock (InGameBufferSync) {
        InGameLogEntries.Enqueue(entry);
        while (InGameLogEntries.Count > MaxInGameBufferedLines) {
          InGameLogEntries.Dequeue();
        }
      }
    }

    private static string FormatInGameEntry(FireInGameLogEntry entry) {
      return $"[{entry.Timestamp}] [{entry.LogType}] {entry.Message}";
    }

  }

  public class PrometheusLogger : IModStarter {

    private static readonly object LogFileSync = new();
    private static bool _subscribed;

    private static string FireLogPath {
      get {
        var customPath = Environment.GetEnvironmentVariable("FIRE_LOG_PATH");
        if (!string.IsNullOrWhiteSpace(customPath)) {
          return customPath;
        }

        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        return Path.Combine(homePath, "Library", "Logs", "Mechanistry", "Timberborn", "Fire.log");
      }
    }

    public void StartMod(IModEnvironment modEnvironment) {
      EnsureFireLogMirroringEnabled();
      TimberbornCompatibility.LogStartupSummary();
      Debug.Log("Prometheus loaded: fire system foundation initialized.");
    }

    private static void EnsureFireLogMirroringEnabled() {
      if (_subscribed) {
        return;
      }

      Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
      _subscribed = true;
    }

    private static void OnUnityLogMessageReceived(string condition, string stackTrace, LogType logType) {
      if (string.IsNullOrEmpty(condition) || !condition.Contains("[Prometheus/Fire]", StringComparison.Ordinal)) {
        return;
      }

      try {
        var logPath = FireLogPath;
        var logDirectory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(logDirectory)) {
          Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] [{logType}] {condition}{Environment.NewLine}";

        lock (LogFileSync) {
          File.AppendAllText(logPath, line);
        }
      } catch {
        // Intentionally silent: never let log mirroring break gameplay.
      }
    }

  }
}
