using System;
using System.IO;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
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