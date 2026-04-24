using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal static class PrometheusDebugPalette {

    public static readonly Color Text = new(0.84f, 0.92f, 0.83f, 0.92f);
    public static readonly Color TextMuted = new(0.60f, 0.74f, 0.64f, 0.88f);
    public static readonly Color TextDark = new(0.10f, 0.08f, 0.06f, 1f);
    public static readonly Color Gold = new(0.72f, 0.58f, 0.30f, 1f);
    public static readonly Color GoldLight = new(1.00f, 0.86f, 0.52f, 1f);
    public static readonly Color Frame = new(0.18f, 0.35f, 0.28f, 1f);
    public static readonly Color Border = Frame;
    public static readonly Color Divider = new(0.20f, 0.39f, 0.31f, 0.85f);
    public static readonly Color GoldDivider = new(0.41f, 0.31f, 0.18f, 0.75f);
    public static readonly Color Shell = new(0.07f, 0.16f, 0.14f, 0.97f);
    public static readonly Color Inset = new(0.05f, 0.12f, 0.10f, 0.96f);
    public static readonly Color InsetDark = new(0.03f, 0.08f, 0.07f, 1f);
    public static readonly Color Transparent = new(0f, 0f, 0f, 0f);

    public static readonly Color Button = new(0.25f, 0.43f, 0.32f, 1f);
    public static readonly Color ButtonSelected = new(0.42f, 0.62f, 0.45f, 1f);
    public static readonly Color ButtonNeutral = Color.white;
    public static readonly Color ButtonUnread = new(0.93f, 0.72f, 0.38f, 1f);
    public static readonly Color ButtonClose = new(0.72f, 0.15f, 0.12f, 1f);
    public static readonly Color ButtonShadow = new(0.26f, 0.16f, 0.08f, 1f);
    public static readonly Color ButtonBorderHighlight = GoldLight;
    public static readonly Color CopySuccess = new(0.64f, 0.86f, 0.64f, 1f);

    public static readonly Color StatusEvent = new(0.76f, 0.93f, 0.76f, 1f);
    public static readonly Color StatusWarning = new(0.98f, 0.78f, 0.40f, 1f);
    public static readonly Color StatusError = new(0.96f, 0.47f, 0.47f, 1f);
    public static readonly Color FeedbackSuccess = new(0.72f, 0.93f, 0.72f, 1f);
    public static readonly Color FeedbackWarning = new(0.96f, 0.74f, 0.40f, 1f);

  }
}
