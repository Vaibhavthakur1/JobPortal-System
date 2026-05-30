namespace NotificationService.Services;

public static class EmailTemplates
{
    public static string ApplicationStatus(string recipientName, string subject, string message) => $@"
<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'/><meta name='viewport' content='width=device-width,initial-scale=1'/></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f1f5f9;padding:40px 0;'>
    <tr><td align='center'>
      <table width='520' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <tr>
          <td style='background:#4F46E5;padding:32px 40px;text-align:center;'>
            <span style='font-size:28px;font-weight:900;color:#ffffff;letter-spacing:-0.5px;'>
              Job<span style='color:#a5b4fc;'>Mart</span>
            </span>
          </td>
        </tr>

        <!-- Body -->
        <tr>
          <td style='padding:40px;'>
            <p style='margin:0 0 8px;font-size:15px;color:#6b7280;'>Hi <strong style='color:#111827;'>{recipientName}</strong>,</p>
            <h2 style='margin:0 0 20px;font-size:22px;font-weight:800;color:#111827;line-height:1.3;'>{subject}</h2>
            <p style='margin:0 0 28px;font-size:15px;color:#374151;line-height:1.6;'>{message}</p>

            <a href='http://localhost:4200/my-applications'
               style='display:inline-block;background:#4F46E5;color:#ffffff;font-size:14px;font-weight:700;
                      padding:14px 28px;border-radius:10px;text-decoration:none;'>
              View My Applications
            </a>
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style='background:#f9fafb;padding:24px 40px;border-top:1px solid #e5e7eb;text-align:center;'>
            <p style='margin:0;font-size:12px;color:#9ca3af;'>
              © 2026 JobMart — Your Career Partner<br/>
              You're receiving this because you have an active application on JobMart.
            </p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";
}
