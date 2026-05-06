# 查看所有 ra2 服务日志
journalctl -u ra2

# 查看最近的日志
journalctl -u ra2 -f

# 查看特定时间段的日志
journalctl -u ra2 --since "1 hour ago"