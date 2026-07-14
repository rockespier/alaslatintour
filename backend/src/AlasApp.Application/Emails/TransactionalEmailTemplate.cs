using System.Net;
using System.Text;

namespace AlasApp.Application.Emails;

public sealed record EmailDetail(string Label, string Value);

public static class TransactionalEmailTemplate
{
    public static string Render(
        string eyebrow,
        string title,
        string intro,
        string highlightLabel,
        string highlightValue,
        IReadOnlyCollection<EmailDetail>? details,
        string note,
        string footer)
    {
        var safeEyebrow = Encode(eyebrow).ToUpperInvariant();
        var safeTitle = Encode(title);
        var safeIntro = Encode(intro);
        var safeHighlightLabel = Encode(highlightLabel).ToUpperInvariant();
        var safeHighlightValue = Encode(highlightValue);
        var safeNote = Encode(note);
        var safeFooter = Encode(footer);
        var detailsHtml = RenderDetails(details);

        return $"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="x-apple-disable-message-reformatting">
              <title>{safeTitle}</title>
            </head>
            <body style="margin:0;padding:0;background:#f3f6f8;color:#11202a;font-family:Arial,Helvetica,sans-serif;">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">{safeIntro}</div>
              <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;background:#f3f6f8;">
                <tr>
                  <td align="center" style="padding:28px 14px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;width:100%;max-width:620px;">
                      <tr>
                        <td style="padding:0 0 16px 0;">
                          <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;">
                            <tr>
                              <td style="font-size:20px;line-height:24px;font-weight:800;color:#0b2532;letter-spacing:0;">ALAS Latin Tour</td>
                              <td align="right" style="font-size:11px;line-height:16px;font-weight:700;color:#5b7080;letter-spacing:.08em;text-transform:uppercase;">{safeEyebrow}</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="background:#ffffff;border:1px solid #dce5ea;border-radius:8px;overflow:hidden;">
                          <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;">
                            <tr>
                              <td style="background:#0b2532;padding:22px 24px;border-bottom:4px solid #16a3b8;">
                                <div style="font-size:12px;line-height:18px;font-weight:700;color:#7ee4f1;letter-spacing:.08em;text-transform:uppercase;">Notificacion oficial</div>
                                <h1 style="margin:6px 0 0 0;font-size:26px;line-height:32px;font-weight:800;color:#ffffff;letter-spacing:0;">{safeTitle}</h1>
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:24px;">
                                <p style="margin:0 0 18px 0;font-size:16px;line-height:24px;color:#233844;">{safeIntro}</p>
                                <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;margin:0 0 20px 0;">
                                  <tr>
                                    <td style="background:#f8fbfc;border:1px solid #d5e2e8;border-radius:8px;padding:18px 18px;text-align:center;">
                                      <div style="font-size:11px;line-height:16px;font-weight:700;color:#5b7080;letter-spacing:.08em;text-transform:uppercase;">{safeHighlightLabel}</div>
                                      <div style="margin-top:6px;font-size:30px;line-height:36px;font-weight:800;color:#0b2532;letter-spacing:.08em;">{safeHighlightValue}</div>
                                    </td>
                                  </tr>
                                </table>
                                {detailsHtml}
                                <p style="margin:20px 0 0 0;padding:14px 16px;background:#fff7e8;border-left:4px solid #f2a51a;font-size:14px;line-height:21px;color:#5a4420;">{safeNote}</p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:16px 4px 0 4px;font-size:12px;line-height:18px;color:#6c7d88;text-align:center;">{safeFooter}</td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string RenderDetails(IReadOnlyCollection<EmailDetail>? details)
    {
        if (details is null || details.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("""
            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;border-top:1px solid #e5edf1;border-bottom:1px solid #e5edf1;">
            """);

        foreach (var detail in details)
        {
            builder.Append($"""
                  <tr>
                    <td style="padding:10px 0;font-size:13px;line-height:19px;color:#6b7b86;width:42%;border-top:1px solid #eef3f5;">{Encode(detail.Label)}</td>
                    <td align="right" style="padding:10px 0;font-size:14px;line-height:19px;font-weight:700;color:#1b303c;border-top:1px solid #eef3f5;">{Encode(detail.Value)}</td>
                  </tr>
                """);
        }

        builder.Append("</table>");
        return builder.ToString();
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
