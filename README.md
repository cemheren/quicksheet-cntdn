# quicksheet-cntdn

**Countdown Timer** extension for [QuickSheet](https://github.com/cemheren/QuickSheet) — see days, hours, and minutes remaining until important dates, right on your desktop wallpaper.

![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![Zero Dependencies](https://img.shields.io/badge/dependencies-0-brightgreen)
![License: MIT](https://img.shields.io/badge/license-MIT-blue)

## What it does

Shows countdown timers to important dates with color-coded urgency and progress bars:

| Icon | Meaning |
|------|---------|
| 🔴 | Less than 1 hour |
| 🟡 | Less than 7 days |
| 🟢 | Less than 30 days |
| 🔵 | More than 30 days |
| ✅ | Past (completed) |

## Install

In any QuickSheet cell, type:

```
ext: github:cemheren/quicksheet-cntdn
```

## Usage

**Custom events** — comma-separated dates with labels:

```
cntdn: 2026-12-25 Christmas, 2026-07-04 Independence Day, 2026-10-31 Halloween
```

**Single event:**

```
cntdn: 2026-06-15 Project Launch
```

**No arguments** — shows upcoming US holidays:

```
cntdn:
```

## Output

```
⏳ Countdown        Date         Remaining    Progress
🟢 Christmas        2026-12-25   7mo 10d      ████████░░░░░░░░ 52%
🔵 Independence Day 2026-07-04   1mo 19d      ████████████░░░░ 78%
🟡 Project Launch   2026-06-01   17d 4h       ██████████████░░ 95%
✅ Memorial Day     2026-05-25   3d ago        ████████████████ 100%
```

## Requirements

- [QuickSheet](https://github.com/cemheren/QuickSheet) (any mode)
- .NET 9 SDK

## License

MIT
