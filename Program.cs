using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// QuickSheet Countdown Timer Extension.
/// Prefix: "cntdn". Usage: "cntdn: 2026-12-25 Christmas" or "cntdn: 2026-06-01 Project Launch"
/// Multiple events: "cntdn: 2026-12-25 Christmas, 2026-07-04 Independence Day, 2026-10-31 Halloween"
/// No params = built-in upcoming holidays for current year.
/// </summary>
class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                string? type = doc.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() : null;

                switch (type)
                {
                    case "init":
                        HandleInit();
                        break;
                    case "activate":
                        HandleActivate(doc.RootElement);
                        break;
                    case "deactivate":
                        break;
                }
            }
            catch (Exception ex)
            {
                SendJson(new { type = "error", id = "", message = $"Parse error: {ex.Message}" });
            }
        }
    }

    static void HandleInit()
    {
        SendJson(new
        {
            type = "register",
            prefix = "cntdn",
            name = "Countdown Timer",
            version = "1.0.0"
        });
        SendLog("Countdown Timer registered. Usage: cntdn: YYYY-MM-DD Label, ...");
    }

    static void HandleActivate(JsonElement root)
    {
        string id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";

        string[] extParams = [];
        if (root.TryGetProperty("params", out var paramsProp) && paramsProp.ValueKind == JsonValueKind.Array)
        {
            extParams = paramsProp.EnumerateArray()
                .Select(p => p.GetString()?.Trim() ?? "")
                .Where(p => p.Length > 0)
                .ToArray();
        }

        try
        {
            // Join all params back and split by comma for multiple events
            string rawInput = string.Join(" ", extParams);
            var events = ParseEvents(rawInput);

            if (events.Count == 0)
                events = GetDefaultHolidays();

            // Sort by date
            events.Sort((a, b) => a.Date.CompareTo(b.Date));

            var cells = new List<(int r, int c, string v)>();

            // Header
            cells.Add((0, 0, "⏳ Countdown"));
            cells.Add((0, 1, "Date"));
            cells.Add((0, 2, "Remaining"));
            cells.Add((0, 3, "Progress"));

            var now = DateTime.Now;

            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                int row = i + 1;
                var diff = ev.Date - now;

                string remaining;
                string icon;
                string progress;

                if (diff.TotalSeconds <= 0)
                {
                    // Past event
                    var ago = now - ev.Date;
                    remaining = FormatTimeSpan(ago) + " ago";
                    icon = "✅";
                    progress = "████████████████ 100%";
                }
                else if (diff.TotalHours < 1)
                {
                    remaining = $"{(int)diff.TotalMinutes}m {diff.Seconds}s";
                    icon = "🔴";
                    progress = MakeProgressBar(ev.Date, now, 16);
                }
                else if (diff.TotalDays < 1)
                {
                    remaining = $"{(int)diff.TotalHours}h {diff.Minutes}m";
                    icon = "🟡";
                    progress = MakeProgressBar(ev.Date, now, 16);
                }
                else if (diff.TotalDays < 7)
                {
                    remaining = $"{(int)diff.TotalDays}d {diff.Hours}h";
                    icon = "🟡";
                    progress = MakeProgressBar(ev.Date, now, 16);
                }
                else if (diff.TotalDays < 30)
                {
                    int weeks = (int)(diff.TotalDays / 7);
                    int days = (int)(diff.TotalDays % 7);
                    remaining = $"{weeks}w {days}d";
                    icon = "🟢";
                    progress = MakeProgressBar(ev.Date, now, 16);
                }
                else
                {
                    int months = (int)(diff.TotalDays / 30.44);
                    int days = (int)(diff.TotalDays % 30.44);
                    remaining = $"{months}mo {days}d";
                    icon = "🔵";
                    progress = MakeProgressBar(ev.Date, now, 16);
                }

                cells.Add((row, 0, $"{icon} {ev.Label}"));
                cells.Add((row, 1, ev.Date.ToString("yyyy-MM-dd")));
                cells.Add((row, 2, remaining));
                cells.Add((row, 3, progress));
            }

            // Footer
            cells.Add((events.Count + 1, 0, $"Updated {now:HH:mm} · {events.Count} events"));

            SendCells(id, cells);
        }
        catch (Exception ex)
        {
            SendCells(id, new List<(int r, int c, string v)>
            {
                (0, 0, $"⚠️ Error: {ex.Message}"),
                (1, 0, "Usage: cntdn: 2026-12-25 Christmas, 2026-06-01 Launch")
            });
        }
    }

    static List<CountdownEvent> ParseEvents(string input)
    {
        var events = new List<CountdownEvent>();
        if (string.IsNullOrWhiteSpace(input)) return events;

        // Split by comma for multiple events
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            // Expected: "YYYY-MM-DD Label" or "YYYY-MM-DD HH:mm Label"
            var tokens = part.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length < 1) continue;

            string dateStr = tokens[0];
            string label = tokens.Length > 1 ? tokens[1] : "Event";

            // Try datetime with time component
            if (DateTime.TryParseExact(dateStr + (tokens.Length > 1 && tokens[1].Contains(':') ? " " + tokens[1].Split(' ', 2)[0] : ""),
                new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                // If we consumed a time from label, remove it
                if (tokens.Length > 1 && tokens[1].Contains(':'))
                {
                    var labelParts = tokens[1].Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    label = labelParts.Length > 1 ? labelParts[1] : "Event";
                }
                events.Add(new CountdownEvent { Date = dt, Label = label });
            }
            else if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                events.Add(new CountdownEvent { Date = dt, Label = label });
            }
        }

        return events;
    }

    static List<CountdownEvent> GetDefaultHolidays()
    {
        int year = DateTime.Now.Year;
        var now = DateTime.Now;

        var holidays = new List<CountdownEvent>
        {
            new() { Date = new DateTime(year, 1, 1), Label = "New Year's Day" },
            new() { Date = new DateTime(year, 2, 14), Label = "Valentine's Day" },
            new() { Date = new DateTime(year, 3, 17), Label = "St. Patrick's Day" },
            new() { Date = new DateTime(year, 7, 4), Label = "Independence Day" },
            new() { Date = new DateTime(year, 10, 31), Label = "Halloween" },
            new() { Date = new DateTime(year, 11, 27), Label = "Thanksgiving" },
            new() { Date = new DateTime(year, 12, 25), Label = "Christmas" },
            new() { Date = new DateTime(year, 12, 31, 23, 59, 59), Label = "New Year's Eve" },
            new() { Date = new DateTime(year + 1, 1, 1), Label = $"New Year {year + 1}" },
        };

        // Show upcoming + recently passed (last 7 days)
        return holidays
            .Where(h => h.Date > now.AddDays(-7))
            .Take(8)
            .ToList();
    }

    static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{(int)ts.TotalMinutes}m";
    }

    static string MakeProgressBar(DateTime target, DateTime now, int width)
    {
        // Progress from "1 year before target" to target
        var totalSpan = TimeSpan.FromDays(365);
        var elapsed = totalSpan - (target - now);
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        double ratio = Math.Clamp(elapsed / totalSpan, 0, 1);
        int filled = (int)(ratio * width);
        int empty = width - filled;

        string bar = new string('█', filled) + new string('░', empty);
        return $"{bar} {ratio * 100:F0}%";
    }

    static void SendCells(string id, List<(int r, int c, string v)> cells)
    {
        SendJson(new
        {
            type = "write",
            id,
            cells = cells.Select(c => new { r = c.r, c = c.c, v = c.v }).ToArray()
        });
    }

    static void SendJson(object obj)
    {
        string json = JsonSerializer.Serialize(obj, JsonOpts);
        Console.WriteLine(json);
        Console.Out.Flush();
    }

    static void SendLog(string message)
    {
        SendJson(new { type = "log", message });
    }
}

class CountdownEvent
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = "Event";
}
