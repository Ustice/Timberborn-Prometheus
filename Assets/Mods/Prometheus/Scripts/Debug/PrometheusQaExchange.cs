using System;
using System.IO;
using System.Text;

namespace Mods.Prometheus.Scripts {
  internal enum PrometheusQaResult {
    Passed,
    Failed,
    Blocked,
  }

  internal readonly struct PrometheusQaInstruction {

    public readonly string Text;
    public readonly DateTime LastUpdatedUtc;
    public readonly bool Exists;

    public PrometheusQaInstruction(string text, DateTime lastUpdatedUtc, bool exists) {
      Text = text;
      LastUpdatedUtc = lastUpdatedUtc;
      Exists = exists;
    }

    public string Signature => $"{Exists}|{LastUpdatedUtc.Ticks}|{Text}";

  }

  internal class PrometheusQaExchange {

    private const string DirectoryName = "PrometheusQA";
    private const string InstructionsFileName = "instructions.md";
    private const string ResultsFileName = "results.md";
    private const string EmptyInstructionText = "Waiting for Codex QA instructions.";

    public string DirectoryPath { get; }
    public string InstructionsPath { get; }
    public string ResultsPath { get; }

    public PrometheusQaExchange() {
      DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        "Library",
        "Application Support",
        "Timberborn",
        DirectoryName);
      InstructionsPath = Path.Combine(DirectoryPath, InstructionsFileName);
      ResultsPath = Path.Combine(DirectoryPath, ResultsFileName);
    }

    public PrometheusQaInstruction ReadInstruction() {
      try {
        EnsureDirectory();
        if (!File.Exists(InstructionsPath)) {
          return new PrometheusQaInstruction(
            $"{EmptyInstructionText}\n\nCodex can write the next task to:\n{InstructionsPath}",
            DateTime.MinValue,
            exists: false);
        }

        var text = File.ReadAllText(InstructionsPath);
        return new PrometheusQaInstruction(
          string.IsNullOrWhiteSpace(text) ? EmptyInstructionText : text.TrimEnd(),
          File.GetLastWriteTimeUtc(InstructionsPath),
          exists: true);
      } catch (Exception exception) {
        return new PrometheusQaInstruction(
          $"Unable to read QA instructions: {exception.Message}",
          DateTime.UtcNow,
          exists: false);
      }
    }

    public bool RecordResult(PrometheusQaResult result, string note, PrometheusQaInstruction instruction, out string message) {
      try {
        EnsureDirectory();
        File.AppendAllText(ResultsPath, BuildResultEntry(result, note, instruction));
        message = $"{result} recorded.";
        FireTelemetry.Log($"event=qa_result_recorded result={result.ToString().ToLowerInvariant()} path=\"{ResultsPath}\"");
        return true;
      } catch (Exception exception) {
        message = $"Unable to record QA result: {exception.Message}";
        FireTelemetry.LogWarning($"event=qa_result_record_failed result={result.ToString().ToLowerInvariant()} error=\"{exception.Message}\"");
        return false;
      }
    }

    private void EnsureDirectory() {
      Directory.CreateDirectory(DirectoryPath);
    }

    private static string BuildResultEntry(PrometheusQaResult result, string note, PrometheusQaInstruction instruction) {
      var builder = new StringBuilder();
      builder.AppendLine();
      builder.AppendLine($"## {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {result}");
      builder.AppendLine();
      builder.AppendLine($"Instruction updated UTC: {(instruction.Exists ? instruction.LastUpdatedUtc.ToString("yyyy-MM-dd HH:mm:ss") : "not found")}");
      builder.AppendLine();
      builder.AppendLine("### Note");
      builder.AppendLine();
      builder.AppendLine(string.IsNullOrWhiteSpace(note) ? "_No note entered._" : note.Trim());
      builder.AppendLine();
      builder.AppendLine("### Instruction");
      builder.AppendLine();
      builder.AppendLine("```");
      builder.AppendLine(instruction.Text.Trim());
      builder.AppendLine("```");
      return builder.ToString();
    }

  }
}
